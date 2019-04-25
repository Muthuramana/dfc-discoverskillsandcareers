﻿using Newtonsoft.Json;
using System;

namespace Dfc.DiscoverSkillsAndCareers.Models
{
    public class Question
    {
        [JsonProperty("partitionKey")]
        public string PartitionKey { get; set; }
        [JsonProperty("id")]
        public string QuestionId { get; set; }
        [JsonProperty("texts")]
        public QuestionText[] Texts { get; set; } = {};
        [JsonProperty("traitCode")]
        public string TraitCode { get; set; }
        [JsonProperty("isNegative")]
        public bool IsNegative { get; set; }
        [JsonProperty("order")]
        public int Order { get; set; }
        [JsonProperty("excludesJobProfiles")]
        public string[] ExcludesJobProfiles { get; set; } = {};
        [JsonProperty("filterTrigger")]
        public string FilterTrigger { get; set; }
        [JsonProperty("sfid")]
        public string SfId { get; set; }
        [JsonProperty("positiveResultDisplayText")]
        public string PositiveResultDisplayText { get; set; }
        [JsonProperty("negativeResultDisplayText")]
        public string NegativeResultDisplayText { get; set; }
        [JsonProperty("lastUpdatedDt")]
        public DateTime LastUpdatedDt { get; set; }
    }
}
