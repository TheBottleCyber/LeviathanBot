using System.Configuration;
using System.Globalization;
using System.Reflection;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using ESI.NET;
using ESI.NET.Enumerations;
using Leviathan.Core.Extensions;
using Leviathan.Core.Models;
using Leviathan.Jobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Quartz;
using Quartz.Impl;
using Serilog;

namespace Leviathan
{
    public class Program
    {
        public static IScheduler Scheduler { get; set; } = null!;
        public static IOptions<EsiConfig> EsiConfigOptions { get; set; } = null!;

        public static async Task Main()
        {
            //TODO: make dependency injection
            var config = LeviathanSettings.GetSettingsFile();
            EsiConfigOptions = Options.Create(config.GetRequiredSection("ESIConfig").Get<EsiConfig>());
            Scheduler = await new StdSchedulerFactory().GetScheduler();

            var botConfigSettings = config.GetSection("BotConfig");
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(botConfigSettings["Language"]);
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(botConfigSettings["Language"]);
            
            await Scheduler.Start();
            await CreateStartupJobs();
            
            await Task.Delay(Timeout.Infinite);
        }

        private static async Task CreateStartupJobs()
        {
            await QuartzJobHelper.SimplyCreateJob<UpdateEsiTokens>(Scheduler, 
                "update_esi_token", x => x.RepeatForever().WithIntervalInMinutes(10));
            
            await QuartzJobHelper.SimplyCreateJob<UpdateCharactersAffiliation>(Scheduler,
                "update_character_affiliation", x => x.RepeatForever().WithIntervalInMinutes(5));
            
            await QuartzJobHelper.SimplyCreateJob<UpdateCorporations>(Scheduler,
                "update_corporations", x => x.RepeatForever().WithIntervalInHours(1));
        }
    }
}