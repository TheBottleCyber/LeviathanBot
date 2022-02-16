using System.Globalization;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Leviathan.Bot.Jobs;
using Leviathan.Bot.Services;
using Leviathan.Core.DatabaseContext;
using Leviathan.Core.Extensions;
using Leviathan.Core.Models.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Serilog;
using Serilog.Events;

namespace Leviathan.Bot
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
        private DiscordSocketClient _discordSocketClient = null!;

        public Startup()
        {
            Log.Logger = new LoggerConfiguration()
                         .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                         .MinimumLevel.Override("Quartz.Core.QuartzScheduler", LogEventLevel.Warning)
                         .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
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
                Log.Information("Starting bot host");
                var builder = CreateHostBuilder(args).Build();
                _discordSocketClient = builder.Services.GetRequiredService<DiscordSocketClient>();
                await builder.Services.GetRequiredService<CommandHandler>().InitializeAsync();

                _discordSocketClient.Log += DiscordClientOnLog;
                _discordSocketClient.UserJoined += ClientOnUserJoined;
                _discordSocketClient.Ready += async () =>
                {
                    await builder.Services.GetRequiredService<InteractionService>()
                                 .RegisterCommandsToGuildAsync(_settings.DiscordConfig.ServerGuildId);
                };

                await _discordSocketClient.LoginAsync(TokenType.Bot, _settings.DiscordConfig.BotToken);
                await _discordSocketClient.StartAsync();
                
                await builder.RunAsync();
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
                var update_discord_names = new JobKey("update_discord_names", "startup");
                q.AddJob<UpdateDiscordNames>(update_discord_names);
                q.AddTrigger(t =>
                    t.WithIdentity("update_discord_names_trigger")
                     .ForJob(update_discord_names)
                     .WithSimpleSchedule(x => x.RepeatForever().WithIntervalInMinutes(5))
                );

                var update_discord_roles = new JobKey("update_discord_roles", "startup");
                q.AddJob<UpdateDiscordRoles>(update_discord_roles);
                q.AddTrigger(t =>
                    t.WithIdentity("update_discord_roles_trigger")
                     .ForJob(update_discord_roles)
                     .WithSimpleSchedule(x => x.RepeatForever().WithIntervalInMinutes(5))
                );

                q.UseMicrosoftDependencyInjectionJobFactory();
            });

            services.AddQuartzHostedService(options => { options.WaitForJobsToComplete = true; });

            services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig { GatewayIntents = GatewayIntents.All }));
            services.AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()));
            services.AddSingleton<CommandHandler>();
        }

        private async Task ClientOnUserJoined(SocketGuildUser arg)
        {
            if (_settings.BotConfig.WelcomeMessageEnabled &&
                _settings.BotConfig.WelcomeMessageChannelId is not 0 &&
                !string.IsNullOrEmpty(_settings.BotConfig.WelcomeMessage))
            {
                var discordServerGuild = _discordSocketClient.GetGuild(_settings.DiscordConfig.ServerGuildId);

                if (discordServerGuild is null) Log.Error($"Server guild with id: {_settings.DiscordConfig.ServerGuildId} not found");

                var discordChannel = await _discordSocketClient.GetChannelAsync(_settings.BotConfig.WelcomeMessageChannelId);

                if (discordChannel is IMessageChannel msgChannel)
                {
                    var message = _settings.BotConfig.WelcomeMessage
                                           .Replace("$user_mention", arg.Mention);

                    await msgChannel.SendMessageAsync(message);
                }
                else
                {
                    Log.Error($"Server welcome message channel with id: {_settings.BotConfig.WelcomeMessageChannelId} not found");
                }
            }
        }

        private static Task DiscordClientOnLog(LogMessage arg)
        {
            var severity = arg.Severity switch
            {
                LogSeverity.Critical => LogEventLevel.Fatal,
                LogSeverity.Debug => LogEventLevel.Debug,
                LogSeverity.Error => LogEventLevel.Error,
                LogSeverity.Info => LogEventLevel.Information,
                LogSeverity.Verbose => LogEventLevel.Verbose,
                LogSeverity.Warning => LogEventLevel.Warning,
                _ => LogEventLevel.Information
            };

            Log.Write(severity, arg.Exception, arg.Message);

            return Task.CompletedTask;
        }
    }
}