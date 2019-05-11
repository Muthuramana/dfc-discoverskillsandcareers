﻿using Dfc.DiscoverSkillsAndCareers.WebApp.Config;
using Dfc.DiscoverSkillsAndCareers.WebApp.Models;
using Dfc.DiscoverSkillsAndCareers.WebApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using System.Web;

namespace Dfc.DiscoverSkillsAndCareers.WebApp.Controllers
{
    public class QuestionController : BaseController
    {
        readonly ILogger<QuestionController> _log;
        readonly IApiServices _apiServices;
        readonly AppSettings _appSettings;

        public QuestionController(
            ILogger<QuestionController> log,
            IApiServices apiServices,
            IOptions<AppSettings> appSettings)
        {
            _log = log;
            _apiServices = apiServices;
            _appSettings = appSettings.Value;
        }

        [HttpPost]
        [Route("q/{assessment}/{questionNumber}")]
        public async Task<IActionResult> AnswerQuestion(string assessment, string questionNumber)
        {
            var correlationId = Guid.NewGuid();
            var isRealQuestionNumber = int.TryParse(questionNumber, out int questionNumberValue);
            string sessionId = null;
            try
            {
                sessionId = await TryGetSessionId(Request);

                if (sessionId == null || sessionId != HttpUtility.UrlEncode(sessionId))
                {
                    return BadRequest();
                }
                if (!isRealQuestionNumber)
                {
                    return BadRequest();
                }

                var postAnswerRequest = new PostAnswerRequest()
                {
                    QuestionId = GetFormValue("questionId"),
                    SelectedOption = GetFormValue("selected_answer")
                };
                
                var postAnswerResponse = await _apiServices.PostAnswer(sessionId, postAnswerRequest, correlationId);
                
                if (postAnswerRequest.SelectedOption == null || postAnswerResponse == null)
                {
                    return await NextQuestion(sessionId, assessment, questionNumberValue, true);
                }
                if (postAnswerResponse.IsComplete)
                {
                    var finishEndpoint = postAnswerResponse.IsFilterAssessment ? $"/finish/{postAnswerResponse.JobCategorySafeUrl}" : "/finish";
                    AppendCookie(sessionId);
                    return Redirect(finishEndpoint);
                }
                var url = $"/q/{assessment}/{GetQuestionPageNumber(postAnswerResponse.NextQuestionNumber)}";
                return Redirect(url);
            }
            catch (System.Net.Http.HttpRequestException)
            {
                return await NextQuestion(sessionId, assessment, questionNumberValue, true);
            }
            catch (Exception ex)
            {
                _log.LogError(ex,$"Correlation Id: {correlationId} - An error occurred in session {sessionId} answering question: {questionNumber} in assessment {assessment}.");
                return StatusCode(500);
            }
        }

        [HttpGet("assessment/{assessment}")]
        public async Task<IActionResult> NewAssessment(string assessment)
        {
            var correlationId = Guid.NewGuid();
            try
            {                
                var newSessionResponse = await _apiServices.NewSession(correlationId, assessment);
                if (newSessionResponse == null)
                {
                    throw new Exception($"Failed to create session for assessment type {assessment}");
                }
                var sessionId = newSessionResponse.SessionId;
                AppendCookie(sessionId);
                
                var redirectResponse = new RedirectResult($"/q/{assessment}/01");
                
                return redirectResponse;

            }
            catch (Exception ex)
            {
                _log.LogError(ex, $"Correlation Id: {correlationId} - An error occured creating a new assessment of type {assessment}");
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("q/{assessment}/{questionNumber}")]
        public async Task<IActionResult> AtQuestionNumber(string assessment, string questionNumber)
        {
            var correlationId = Guid.NewGuid();
            var isRealQuestionNumber = int.TryParse(questionNumber, out int questionNumberValue);
            try
            {
                if (!isRealQuestionNumber)
                {
                    return BadRequest();
                }
                var sessionId = await TryGetSessionId(Request);
                if (string.IsNullOrEmpty(sessionId))
                {
                    return Redirect("/");
                }
                return await NextQuestion(sessionId, assessment, questionNumberValue, false);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, $"Correlation Id: {correlationId} - An error occured getting question {questionNumber} for assessment type {assessment}");
                return StatusCode(500);
            }
        }

        [NonAction]
        public async Task<IActionResult> NextQuestion(string sessionId, string assessment, int questionNumber, bool invalidAnswer)
        {
            var correlationId = Guid.NewGuid();
            try
            {
                var nextQuestionResponse = await _apiServices.Question(sessionId, assessment, questionNumber, correlationId);

                if (nextQuestionResponse == null)
                {
                    throw new Exception($"Question not found for session {sessionId}");
                }

                
                var formRoute = GetAnswerFormPostRoute(nextQuestionResponse, assessment);
                int displayPercentComplete = nextQuestionResponse.PercentComplete;

                var model = new QuestionViewModel();
                model.Code = sessionId;
                model.ErrorMessage = !invalidAnswer ? string.Empty : model.NoAnswerErrorMessage;
                model.FormRoute = formRoute;
                model.Percentage = displayPercentComplete.ToString();
                model.PercentrageLeft = nextQuestionResponse.PercentComplete == 0 ? "" : nextQuestionResponse.PercentComplete.ToString();
                model.QuestionId = nextQuestionResponse.QuestionId;
                model.QuestionNumber = nextQuestionResponse.QuestionNumber;
                model.RecordedAnswer = nextQuestionResponse.RecordedAnswer;
                model.SessionId = sessionId;
                model.TraitCode = nextQuestionResponse.TraitCode;
                model.QuestionText = nextQuestionResponse.QuestionText;
                model.IsFilterAssessment = nextQuestionResponse.IsFilterAssessment;
                model.AssessmentType = assessment;
                
                AppendCookie(sessionId);
                var viewName = model.IsFilterAssessment ? "FilteringQuestion" : "Question";
                return View(viewName, model);
            }
            catch (System.Net.Http.HttpRequestException)
            {
                return Redirect("/");
            }
            catch (Exception ex)
            {
                _log.LogError(ex, $"Correlation Id: {correlationId} - An error occured while getting \"next\" question {questionNumber} for assessment type {assessment}");
                return StatusCode(500);
            }
        }

        private string GetAnswerFormPostRoute(AssessmentQuestionResponse assessmentQuestionResponse, string assessment)
        {
            var questionNumber = GetQuestionPageNumber(assessmentQuestionResponse.NextQuestionNumber ?? assessmentQuestionResponse.MaxQuestionsCount);
            var nextRoute = $"/q/{assessment}/{questionNumber}";
            return nextRoute;
        }

        public static string GetQuestionPageNumber(int questionNumber)
        {
            if (questionNumber < 10) return $"0{questionNumber.ToString()}";
            return questionNumber.ToString();
        }
    }
}