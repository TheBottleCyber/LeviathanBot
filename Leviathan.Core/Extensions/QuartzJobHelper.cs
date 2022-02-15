using Quartz;

namespace Leviathan.Core.Extensions
{
    public static class QuartzJobHelper
    {
        public static async Task SimplyScheduleDelayedJob(this IScheduler scheduler, DateTime dateTime, IJobDetail jobDetail)
        {
            var getDifferentFromDateTime = dateTime - DateTime.UtcNow;
            var startTimeUtc = getDifferentFromDateTime.Ticks > 0 ? DateTime.UtcNow.AddTicks(getDifferentFromDateTime.Ticks) : DateTime.UtcNow;

            var trigger = (ISimpleTrigger) TriggerBuilder.Create()
                                                         .ForJob(jobDetail)
                                                         .StartAt(startTimeUtc)
                                                         .Build();

            await scheduler.ScheduleJob(jobDetail, trigger);
        }
    }
}