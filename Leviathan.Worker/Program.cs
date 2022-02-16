using System.Globalization;
using Leviathan.Core.DatabaseContext;
using Leviathan.Core.Extensions;
using Leviathan.Core.Models.Options;
using Leviathan.Worker.Jobs;
using Leviathan.Worker.Jobs.Schedule;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Serilog;
using Serilog.Events;

namespace Leviathan.Worker
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await new Startup().Start(args);
        }
    }

    public class Startup
    {
        private Settings _settings;

        public Startup()
        {
            Log.Logger = new LoggerConfiguration()
                         .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                         .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                         .Enrich.FromLogContext()
                         .WriteTo.Console()
                         .CreateLogger();

            _settings = LeviathanSettings.GetSettingsFile();

            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(_settings.BotConfig.Language);
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(_settings.BotConfig.Language);
        }

        public async Task Start(string[] args)
        {
            try
            {
                Log.Information("Starting worker host");

                await CreateHostBuilder(args).Build().RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args).UseSerilog().ConfigureServices(ConfigureServices);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(_settings);
            services.AddSingleton(Log.Logger);
            services.AddDbContext<SqliteContext>(opt => opt.UseSqlite(@$"DataSource={_settings.DatabaseConfig.ConnectionString};"));
            services.AddQuartz(q =>
            {
                var update_esi_tokens = new JobKey("update_esi_token", "schedule");
                q.AddJob<ScheduleUpdateEsiToken>(update_esi_tokens);
                q.AddTrigger(t =>
                    t.WithIdentity("update_esi_token_trigger")
                     .ForJob(update_esi_tokens)
                     .StartNow()
                     .WithSimpleSchedule(x => x.RepeatForever().WithIntervalInMinutes(5))
                );
                
                var update_characters = new JobKey("update_characters", "startup");
                q.AddJob<StartupUpdateCharacters>(update_characters);
                q.AddTrigger(t =>
                    t.WithIdentity("update_characters_trigger")
                     .ForJob(update_characters)
                     .StartNow()
                     .WithSimpleSchedule(x => x.RepeatForever().WithIntervalInMinutes(5))
                );
                
                var update_corporations = new JobKey("update_corporation", "schedule");
                q.AddJob<ScheduleUpdateCorporation>(update_corporations);
                q.AddTrigger(t =>
                    t.WithIdentity("update_corporation_trigger")
                     .ForJob(update_corporations)
                     .StartNow()
                     .WithSimpleSchedule(x => x.RepeatForever().WithIntervalInMinutes(5))
                );

                q.UseMicrosoftDependencyInjectionJobFactory();
            });

            services.AddQuartzHostedService(options => { options.WaitForJobsToComplete = true; });
        }
    }
}