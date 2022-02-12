using Quartz;

namespace Leviathan.Core.Extensions
{
    public static class QuartzJobHelper
    {
        public static async Task SimplyCreateJob<T>(this IScheduler scheduler,string jobName, Action<SimpleScheduleBuilder> action) where T : IJob
        {
            var job = JobBuilder.Create<T>().WithIdentity(jobName).Build();
            var trigger = TriggerBuilder.Create().StartNow().WithSimpleSchedule(action).Build();
            
            await scheduler.ScheduleJob(job, trigger);
        }
    }
}