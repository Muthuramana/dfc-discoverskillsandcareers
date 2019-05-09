﻿using Dfc.DiscoverSkillsAndCareers.Models;
using Dfc.DiscoverSkillsAndCareers.Repositories;
using Microsoft.Azure.Documents;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dfc.UnitTests.Fakes
{
    public class FakeQuestionSetRepository : IQuestionSetRepository
    {
        public Task<QuestionSet> GetCurrentQuestionSet(string assessmentType)
        {
            var questionSet = new QuestionSet()
            {
                AssessmentType = assessmentType,
                Title = "QS 1"
            };
            return Task.FromResult(questionSet);
        }

        public Task<Document> CreateOrUpdateQuestionSet(QuestionSet questionSet)
        {
            throw new NotImplementedException();
        }

        public Task<List<QuestionSet>> GetCurrentFilteredQuestionSets()
        {
            var questionSet = new QuestionSet()
            {
                AssessmentType = "filtered",
                Title = "Filtered 1",
                QuestionSetKey = "filtered-1"
            };
            return Task.FromResult(new List<QuestionSet> {questionSet});
        }


        public Task<QuestionSet> GetQuestionSetVersion(string assessmentType, string title, int version)
        {
            throw new NotImplementedException();
        }

        public Task<int> ResetCurrentFilteredQuestionSets()
        {
            throw new NotImplementedException();
        }

        public Task<QuestionSet> GetLatestQuestionSetByTypeAndKey(string assessmentType, string key)
        {
            var questionSet = new QuestionSet()
            {
                AssessmentType = assessmentType,
                Title = key,
                QuestionSetKey = key.Replace(" ", "-").ToLower()
            };
            return Task.FromResult<QuestionSet>(questionSet);
        }
    }
}
