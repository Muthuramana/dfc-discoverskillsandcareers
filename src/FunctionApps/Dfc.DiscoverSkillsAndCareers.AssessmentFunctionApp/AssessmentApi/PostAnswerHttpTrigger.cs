using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Dfc.DiscoverSkillsAndCareers.AssessmentFunctionApp.Models;
using Dfc.DiscoverSkillsAndCareers.AssessmentFunctionApp.Services;
using Dfc.DiscoverSkillsAndCareers.Models;
using Dfc.DiscoverSkillsAndCareers.Repositories;
using DFC.Functions.DI.Standard.Attributes;
using DFC.HTTP.Standard;
using DFC.Swagger.Standard.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Dfc.DiscoverSkillsAndCareers.AssessmentFunctionApp.AssessmentApi
{
    public static class PostAnswerHttpTrigger
    {
        [FunctionName("PostAnswerHttpTrigger")]
        [ProducesResponseType(typeof(PostAnswerResponse), (int)HttpStatusCode.OK)]
        [Response(HttpStatusCode = (int)HttpStatusCode.OK, Description = "Stores the answer for the given question against the current session", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NotFound, Description = "No such session exists", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "The answer value provided is not valid", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Unauthorized, Description = "API key is unknown or invalid", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient access", ShowSchema = false)]
        [Display(Name = "PostAnswer", Description = "Stores an answer for a given question against the current session.")]

        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "assessment/{sessionId}")]HttpRequest req,
            string sessionId,
            ILogger log,
            [Inject]IHttpRequestHelper httpRequestHelper,
            [Inject]IHttpResponseMessageHelper httpResponseMessageHelper,
            [Inject]IUserSessionRepository userSessionRepository,
            [Inject]IQuestionRepository questionRepository,
            [Inject]IAssessmentCalculationService resultsService,
            [Inject]IFilterAssessmentCalculationService filterAssessmentCalculationService)
        {
            try
            {
                var correlationId = httpRequestHelper.GetDssCorrelationId(req);
                if (string.IsNullOrEmpty(correlationId))
                    log.LogInformation("Unable to locate 'DssCorrelationId' in request header");

                if (!Guid.TryParse(correlationId, out var correlationGuid))
                {
                    log.LogInformation("Unable to parse 'DssCorrelationId' to a Guid");
                    correlationGuid = Guid.NewGuid();
                }

                if (string.IsNullOrEmpty(sessionId))
                {
                    log.LogInformation($"CorrelationId: {correlationGuid} - Session Id not supplied");
                    return httpResponseMessageHelper.BadRequest();
                }

                PostAnswerRequest postAnswerRequest;
                using (var streamReader = new StreamReader(req.Body))
                {
                    var body = streamReader.ReadToEnd();
                    postAnswerRequest = JsonConvert.DeserializeObject<PostAnswerRequest>(body);
                }

                AnswerOption answerValue;
                if (Enum.TryParse(postAnswerRequest.SelectedOption, out answerValue) == false)
                {
                    log.LogInformation(
                        $"CorrelationId: {correlationGuid} - Answer supplied is invalid {postAnswerRequest.SelectedOption}");
                    return httpResponseMessageHelper.BadRequest();
                }

                var userSession = await userSessionRepository.GetUserSession(sessionId);
                if (userSession == null)
                {
                    log.LogInformation($"CorrelationId: {correlationGuid} - Session Id does not exist {sessionId}");
                    return httpResponseMessageHelper.NoContent();
                }

                var question = await questionRepository.GetQuestion(postAnswerRequest.QuestionId);
                if (question == null)
                {
                    log.LogInformation(
                        $"CorrelationId: {correlationGuid} - Question Id does not exist {postAnswerRequest.QuestionId}");
                    return httpResponseMessageHelper.NoContent();
                }

                userSession.AddAnswer(answerValue, question);

                await TryEvaluateSession(log, resultsService, filterAssessmentCalculationService, userSession);
                
                var displayFinish = question.IsFilterQuestion
                    ? userSession.FilteredAssessmentState.IsComplete
                    : userSession.AssessmentState.IsComplete;

                if (!question.IsFilterQuestion) {
                    userSession.AssessmentState.CurrentQuestion = question.Order;
                }
                
                var result = new PostAnswerResponse()
                {
                    IsSuccess = true,
                    IsComplete = displayFinish,
                    IsFilterAssessment = question.IsFilterQuestion,
                    JobCategorySafeUrl = question.IsFilterQuestion ? userSession.FilteredAssessmentState.JobFamilyNameUrlSafe : null,
                    NextQuestionNumber = question.IsFilterQuestion ? userSession.FilteredAssessmentState.MoveToNextQuestion() : userSession.AssessmentState.MoveToNextQuestion()
                };
                
                await userSessionRepository.UpdateUserSession(userSession);

                return httpResponseMessageHelper.Ok(JsonConvert.SerializeObject(result));
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Fatal exception {message}", ex.Message);
                return new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError };
            }
        }

        
        private static async Task TryEvaluateSession(ILogger log, IAssessmentCalculationService resultsService,
            IFilterAssessmentCalculationService filterAssessmentCalculationService, UserSession userSession)
        {
            var state = userSession.CurrentState;

            if (state.IsComplete)
            {
                if (userSession.IsFilterAssessment)
                {
                    await filterAssessmentCalculationService.CalculateAssessment(userSession, log);
                    
                }
                else
                {
                    await resultsService.CalculateAssessment(userSession, log);
                    
                }
            }
            else
            {
                userSession.CurrentState.MoveToNextQuestion();
            }
        }


        

        
        
    }
}
