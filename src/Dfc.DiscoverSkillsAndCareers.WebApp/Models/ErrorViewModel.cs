using System;

namespace Dfc.DiscoverSkillsAndCareers.WebApp.Models
{
    public class ErrorViewModel : BaseViewModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}