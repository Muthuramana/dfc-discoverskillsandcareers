﻿namespace Dfc.DiscoverSkillsAndCareers.WebApp.Models
{
    public class IndexViewModel
    {
        public string SessionId { get; set; }
        public bool HasReloadError { get; set; }
        public string ResumeErrorMessage { get; set; } = "The code could not be found";
        public string MissingCodeErrorMessage { get; set; } = "Please enter your reference";
    }
}
