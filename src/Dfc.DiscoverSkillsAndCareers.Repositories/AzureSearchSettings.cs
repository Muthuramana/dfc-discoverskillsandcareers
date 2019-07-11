﻿using System.Diagnostics.CodeAnalysis;

namespace Dfc.DiscoverSkillsAndCareers.Repositories
{
    [ExcludeFromCodeCoverage]
    public class AzureSearchSettings
    {
        public string ServiceName { get; set; }
        public string IndexName { get; set; }
        public string ApiKey { get; set; }

    }
}