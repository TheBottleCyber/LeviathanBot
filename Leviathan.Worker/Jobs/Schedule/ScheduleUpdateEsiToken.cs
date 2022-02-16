using ESI.NET;
using ESI.NET.Enumerations;
using Leviathan.Core.DatabaseContext;
using Leviathan.Core.Extensions;
using Leviathan.Core.Models.Options;
using Leviathan.Worker.Jobs.Runtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Quartz;
using Serilog;

namespace Leviathan.Worker.Jobs.Schedule
{
    [DisallowConcurrentExecution]
    public class ScheduleUpdateEsiToken : IJob
    {
        private SqliteContext _sqliteContext;

        public ScheduleUpdateEsiToken(SqliteContext sqliteContext)
        {
            _sqliteContext = sqliteContext;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (await _sqliteContext.Characters.AnyAsync())
            {
                var characters = _sqliteContext.Characters
                                               .Where(x => !string.IsNullOrEmpty(x.EsiTokenAccessToken) &&
                                                           !string.IsNullOrEmpty(x.EsiTokenRefreshToken));

                foreach (var character in characters)
                {
                    var jobKey = new JobKey($"{character.EsiCharacterID}", "runtime_update_esi_token");

                    if (!await context.Scheduler.CheckExists(jobKey))
                    {
                        var updateEsiTokenJob = JobBuilder.Create<RuntimeUpdateEsiToken>()
                                                          .WithIdentity(jobKey)
                                                          .UsingJobData("character_id", character.EsiCharacterID)
                                                          .Build();

                        await QuartzJobHelper.SimplyScheduleDelayedJob(context.Scheduler, character.EsiTokenExpiresOn, updateEsiTokenJob);
                    }
                }
            }
        }
    }
}