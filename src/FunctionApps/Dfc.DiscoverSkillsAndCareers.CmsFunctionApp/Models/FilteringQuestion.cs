﻿using Newtonsoft.Json;
using System.Collections.Generic;

namespace Dfc.DiscoverSkillsAndCareers.CmsFunctionApp.Models
{
    public class FilteringQuestion
    {
        [JsonProperty("Id")]
        public string Id { get; set; }
        [JsonProperty("QuestionText")]
        public string Title { get; set; }
        [JsonProperty("Description")]
        public string Description { get; set; }
        [JsonProperty("Order")]
        public int? Order { get; set; }
        [JsonProperty("ExcludesJobProfiles")]
        public List<string> ExcludesJobProfiles { get; set; }
    }
}