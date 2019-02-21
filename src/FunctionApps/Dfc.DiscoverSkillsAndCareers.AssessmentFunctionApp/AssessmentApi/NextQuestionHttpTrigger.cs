using Dfc.DiscoverSkillsAndCareers.Models;
using Dfc.DiscoverSkillsAndCareers.Repositories;
using Dfc.DiscoverSkillsAndCareers.AssessmentFunctionApp.Models;
using DFC.Common.Standard.Logging;
using DFC.Functions.DI.Standard.Attributes;
using DFC.HTTP.Standard;
using DFC.Swagger.Standard.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dfc.DiscoverSkillsAndCareers.AssessmentFunctionApp
{
    public static class NextQuestionHttpTrigger
    {
        [FunctionName("NextQuestionHttpTrigger")]
        [ProducesResponseType(typeof(Question), (int)HttpStatusCode.OK)]
        [Response(HttpStatusCode = (int)HttpStatusCode.OK, Description = "Gets the next question for a given user session", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NotFound, Description = "No such session exists", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "The request is malformed", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Unauthorized, Description = "API key is unknown or invalid", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient access", ShowSchema = false)]
        [Display(Name = "Get", Description = "")]

        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "assessment/{sessionId}/next")]HttpRequest req, 
            string sessionId,
            ILogger log,
            [Inject]ILoggerHelper loggerHelper,
            [Inject]IHttpRequestHelper httpRequestHelper,
            [Inject]IHttpResponseMessageHelper httpResponseMessageHelper,
            [Inject]IUserSessionRepository userSessionRepository,
            [Inject]IQuestionRepository questionRepository)
        {
            loggerHelper.LogMethodEnter(log);

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
                loggerHelper.LogInformationMessage(log, correlationGuid, "Session Id not supplied");
                return httpResponseMessageHelper.BadRequest();
            }

            var userSession = await userSessionRepository.GetUserSession(sessionId);
            if (userSession == null)
            {
                loggerHelper.LogInformationMessage(log, correlationGuid, string.Format("Session Id does not exist {0}", sessionId));
                return httpResponseMessageHelper.NoContent();
            }

            var question = await questionRepository.GetQuestion(userSession.CurrentQuestion, userSession.QuestionSetVersion);
            if (question == null)
            {
                loggerHelper.LogInformationMessage(log, correlationGuid, $"Question number {userSession.CurrentQuestion} could not be found on session {userSession.PrimaryKey}");
                return httpResponseMessageHelper.NoContent();
            }

            await userSessionRepository.UpdateUserSession(userSession);

            int percentComplete = Convert.ToInt32(((userSession.RecordedAnswers.Count) / Convert.ToDecimal(userSession.MaxQuestions)) * 100);
            var nextQuestionResponse = new NextQuestionResponse()
            {
                IsComplete = userSession.IsComplete,
                NextQuestionNumber = GetNextQuestionNumber(userSession.CurrentQuestion, userSession.MaxQuestions),
                QuestionId = question.QuestionId,
                QuestionText = question.Texts.Where(x => x.LanguageCode.ToLower() == "en").FirstOrDefault()?.Text,
                TraitCode = question.TraitCode,
                QuestionNumber = question.Order,
                SessionId = question.PartitionKey,
                PercentComplete = percentComplete
            };

            loggerHelper.LogMethodExit(log);

            return httpResponseMessageHelper.Ok(JsonConvert.SerializeObject(nextQuestionResponse));
        }

        public static int? GetNextQuestionNumber(int questionNumber, int maxQuestions)
        {
            if (questionNumber + 1 > maxQuestions)
            {
                return null;
            }
            
            return questionNumber + 1;
        }
    }
}