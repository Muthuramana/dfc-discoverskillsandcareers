﻿using Dfc.DiscoverSkillsAndCareers.CmsFunctionApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dfc.DiscoverSkillsAndCareers.CmsFunctionApp.DataRequesters
{
    public interface IGetFilteringQuestionData
    {
        Task<List<FilteringQuestion>> GetData(string siteFinityApiUrlbase, string siteFinityService, string questionSetId);
    }
}
