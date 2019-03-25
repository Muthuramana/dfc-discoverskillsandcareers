﻿using Dfc.DiscoverSkillsAndCareers.Models;
using System.Threading.Tasks;

namespace Dfc.DiscoverSkillsAndCareers.AssessmentFunctionApp.Services
{
    public interface IAssessmentCalculationService
    {
        Task CalculateAssessment(UserSession userSession);
    }
}