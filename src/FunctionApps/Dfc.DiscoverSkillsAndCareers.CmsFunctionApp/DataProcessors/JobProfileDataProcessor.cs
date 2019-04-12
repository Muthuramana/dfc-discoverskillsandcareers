﻿using Dfc.DiscoverSkillsAndCareers.CmsFunctionApp.DataRequesters;
using Dfc.DiscoverSkillsAndCareers.CmsFunctionApp.Services;
using Dfc.DiscoverSkillsAndCareers.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using Dfc.DiscoverSkillsAndCareers.Models;

namespace Dfc.DiscoverSkillsAndCareers.CmsFunctionApp.DataProcessors
{
    public class JobProfileDataProcessor : IJobProfileDataProcessor
    {
        readonly ILogger<JobProfileDataProcessor> Logger;
        readonly ISiteFinityHttpService HttpService;
        readonly IGetJobProfileData GetJobProfileData;
        readonly AppSettings AppSettings;
        readonly IJobProfileRepository JobProfileRepository;

        public JobProfileDataProcessor(
            ILogger<JobProfileDataProcessor> logger,
            ISiteFinityHttpService httpService,
            IGetJobProfileData getJobProfileData,
            IOptions<AppSettings> appSettings,
            IJobProfileRepository jobProfileRepository)
        {
            Logger = logger;
            HttpService = httpService;
            GetJobProfileData = getJobProfileData;
            AppSettings = appSettings.Value;
            JobProfileRepository = jobProfileRepository;
        }

        public async Task RunOnce()
        {
            Logger.LogInformation("Begin poll for JobProfiles");

            var data = await GetJobProfileData.GetData(AppSettings.SiteFinityApiUrlbase, AppSettings.SiteFinityApiWebService);

            Logger.LogInformation($"Have {data?.Count} job profiles to save");

            foreach (var jobProfile in data)
            {
                await JobProfileRepository.CreateJobProfile(new JobProfile()
                {
                    CareerPathAndProgression = jobProfile.CareerPathAndProgression,
                    JobProfileCategories = jobProfile.JobProfileCategories,
                    Overview = jobProfile.Overview,
                    PartitionKey = "jobprofile-cms",
                    SalaryExperienced = jobProfile.SalaryExperienced,
                    SalaryStarter = jobProfile.SalaryStarter,
                    SocCode = jobProfile.SocCode,
                    Title = jobProfile.Title,
                    UrlName = jobProfile.UrlName,
                    WYDDayToDayTasks = jobProfile.WYDDayToDayTasks,
                    ShiftPattern = jobProfile.ShiftPattern,
                    TypicalHours = jobProfile.TypicalHours
                });
            }

            Logger.LogInformation("End poll for JobProfiles");
        }
    }
}
