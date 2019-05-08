﻿using System;
using Dfc.DiscoverSkillsAndCareers.CmsFunctionApp.DataRequesters;
using Dfc.DiscoverSkillsAndCareers.CmsFunctionApp.Services;
using Dfc.DiscoverSkillsAndCareers.Models;
using Dfc.DiscoverSkillsAndCareers.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dfc.DiscoverSkillsAndCareers.CmsFunctionApp.DataProcessors
{
    public class ShortQuestionSetDataProcessor : IShortQuestionSetDataProcessor
    {
        readonly ISiteFinityHttpService _httpService;
        readonly IQuestionRepository _questionRepository;
        readonly IQuestionSetRepository _questionSetRepository;
        readonly IGetShortQuestionSetData _getShortQuestionSetData;
        readonly IGetShortQuestionData _getShortQuestionData;
        readonly AppSettings _appSettings;

        public ShortQuestionSetDataProcessor(
            ISiteFinityHttpService httpService, 
            IQuestionRepository questionRepository,
            IQuestionSetRepository questionSetRepository,
            IGetShortQuestionSetData getShortQuestionSetData,
            IGetShortQuestionData getShortQuestionData,
            IOptions<AppSettings> appSettings)
        {
            _httpService = httpService;
            _questionRepository = questionRepository;
            _questionSetRepository = questionSetRepository;
            _getShortQuestionSetData = getShortQuestionSetData;
            _getShortQuestionData = getShortQuestionData;
            _appSettings = appSettings.Value;
        }

        public async Task RunOnce(ILogger logger)
        {
            logger.LogInformation("Begin poll for ShortQuestionSet");

            string siteFinityApiUrlbase = _appSettings.SiteFinityApiUrlbase;
            string siteFinityService = _appSettings.SiteFinityApiWebService;
            string assessmentType = "short";

            var questionSets = await _getShortQuestionSetData.GetData(siteFinityApiUrlbase, siteFinityService);
            logger.LogInformation($"Have {questionSets?.Count} question sets to review");

            foreach (var data in questionSets)
            {
                logger.LogInformation($"Getting cms data for questionset {data.Id} {data.Title}");

                // Attempt to load the current version for this assessment type and title
                var questionSet = await _questionSetRepository.GetCurrentQuestionSet("short", data.Title);

                // Determine if an update is required i.e. the last updated datetime stamp has changed
                bool updateRequired = questionSet == null || (data.LastUpdated > questionSet.LastUpdated);

                // Nothing to do so log and exit
                if (!updateRequired)
                {
                    logger.LogInformation($"Questionset {data.Id} {data.Title} is upto date - no changes to be done");
                    return;
                }

                // Attempt to get the questions for this questionset
                logger.LogInformation($"Getting cms questions for questionset {data.Id} {data.Title}");
                data.Questions = await _getShortQuestionData.GetData(siteFinityApiUrlbase, siteFinityService, data.Id);
                if (data.Questions.Count == 0)
                {
                    logger.LogInformation($"Questionset {data.Id} doesn't have any questions");
                    return;
                }
                logger.LogInformation($"Received {data.Questions?.Count} questions for questionset {data.Id} {data.Title}");

                if (questionSet != null)
                {
                    // Change the current question set to be not current
                    questionSet.IsCurrent = false;
                    await _questionSetRepository.CreateOrUpdateQuestionSet(questionSet);
                }

                // Create the new current version
                int newVersionNumber = questionSet == null ? 1 : questionSet.Version + 1;
                var newQuestionSet = new QuestionSet()
                {
                    PartitionKey = "ncs",
                    Title = data.Title,
                    TitleLowercase = data.Title.ToLower(),
                    Description = data.Description,
                    Version = newVersionNumber,
                    QuestionSetVersion = $"{assessmentType.ToLower()}-{data.Title.ToLower()}-{newVersionNumber}",
                    AssessmentType = assessmentType,
                    IsCurrent = true,
                    LastUpdated = data.LastUpdated,                    
                };

                string questionPartitionKey = newQuestionSet.QuestionSetVersion;
                int questionNumber = 1;
                foreach (var dataQuestion in data.Questions.OrderBy(x => x.Order))
                {
                    var newQuestion = new Question
                    {
                        IsNegative = dataQuestion.IsNegative,
                        Order = questionNumber,
                        QuestionId = questionPartitionKey + "-" + questionNumber,
                        TraitCode = dataQuestion.Trait.ToUpper(),
                        PartitionKey = questionPartitionKey,
                        LastUpdatedDt = dataQuestion.LastUpdatedDt,
                        Texts = new []
                    {
                        new QuestionText { LanguageCode = "EN", Text = dataQuestion.Title }
                    }
                    };
                    newQuestionSet.MaxQuestions = questionNumber;
                    questionNumber++;
                    await _questionRepository.CreateQuestion(newQuestion);
                    logger.LogInformation($"Created question {newQuestion.QuestionId}");
                }
                await _questionSetRepository.CreateOrUpdateQuestionSet(newQuestionSet);
                logger.LogInformation($"Created questionset {newQuestionSet.Version}");
            }
            logger.LogInformation($"End poll for ShortQuestionSet");
        }
    }
}
