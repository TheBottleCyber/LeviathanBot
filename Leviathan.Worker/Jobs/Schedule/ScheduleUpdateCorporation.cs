using ESI.NET;
using Leviathan.Core.DatabaseContext;
using Leviathan.Core.Extensions;
using Leviathan.Core.Models.Options;
using Leviathan.Jobs.Runtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Quartz;
using Serilog;

namespace Leviathan.Jobs.Schedule
{
    [DisallowConcurrentExecution]
    public class ScheduleUpdateCorporation : IJob
    {
        private SqliteContext _sqliteContext;

        public ScheduleUpdateCorporation(SqliteContext sqliteContext)
        {
            _sqliteContext = sqliteContext;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (await _sqliteContext.Corporations.AnyAsync())
            {
                foreach (var corporation in _sqliteContext.Corporations)
                {
                    var jobKey = new JobKey($"{corporation.CorporationId}", "runtime_update_corporation");

                    if (!await context.Scheduler.CheckExists(jobKey))
                    {
                        var updateEsiTokenJob = JobBuilder.Create<RuntimeUpdateCorporation>()
                                                          .WithIdentity(jobKey)
                                                          .UsingJobData("corporation_id", corporation.CorporationId)
                                                          .Build();

                        await QuartzJobHelper.SimplyScheduleDelayedJob(context.Scheduler, corporation.ExpiresOn, updateEsiTokenJob);
                    }
                }
            }
        }
    }
}