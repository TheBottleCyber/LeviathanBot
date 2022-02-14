using System.Globalization;
using Leviathan.Core.Extensions;
using Leviathan.Core.Models.Options;
using Leviathan.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Serilog;
using Serilog.Events;

namespace Leviathan
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
            _settings = LeviathanSettings.GetSettingsFile();

            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(_settings.BotConfig.Language);
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(_settings.BotConfig.Language);

            Log.Logger = new LoggerConfiguration()
                         .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                         .Enrich.FromLogContext()
                         .WriteTo.Console()
                         .CreateLogger();
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

            services.AddQuartz(q =>
            {
                var update_esi_token = new JobKey("update_esi_token", "startup");
                q.AddJob<UpdateEsiTokens>(update_esi_token);
                q.AddTrigger(t =>
                    t.WithIdentity("update_esi_token_trigger")
                     .ForJob(update_esi_token)
                     .StartNow()
                     .WithSimpleSchedule(x => x.RepeatForever().WithIntervalInMinutes(15))
                );

                var update_character_affiliation = new JobKey("update_character_affiliation", "startup");
                q.AddJob<UpdateCharactersAffiliation>(update_character_affiliation);
                q.AddTrigger(t =>
                    t.WithIdentity("update_character_affiliation_trigger")
                     .ForJob(update_character_affiliation)
                     .StartNow()
                     .WithSimpleSchedule(x => x.RepeatForever().WithIntervalInMinutes(5))
                );

                var update_corporations = new JobKey("update_corporations", "startup");
                q.AddJob<UpdateCorporations>(update_corporations);
                q.AddTrigger(t =>
                    t.WithIdentity("update_corporations_trigger")
                     .ForJob(update_corporations)
                     .StartNow()
                     .WithSimpleSchedule(x => x.RepeatForever().WithIntervalInHours(2))
                );

                q.UseMicrosoftDependencyInjectionJobFactory();
            });

            services.AddQuartzHostedService(options => { options.WaitForJobsToComplete = true; });
        }
    }
}