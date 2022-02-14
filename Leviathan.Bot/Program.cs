using System;
using System.Configuration;
using System.Globalization;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Leviathan.Bot.Jobs;
using Leviathan.Bot.Services;
using Leviathan.Core.DatabaseContext;
using Leviathan.Core.Extensions;
using Leviathan.Core.Models.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Quartz;
using Quartz.Impl;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using IScheduler = Quartz.IScheduler;

namespace Leviathan.Bot
{
    public class Program
    {
        public static DiscordConfigOptions DiscordConfigOptions { get; set; } = new DiscordConfigOptions();
        public static BotConfigOptions BotConfigOptions { get; set; } = new BotConfigOptions();
        public static DiscordSocketClient DiscordSocketClient { get; set; } = null!;
        private InteractionService _commands;
        private IScheduler _scheduler;

        private ServiceProvider ConfigureServices()
        {
            var config = LeviathanSettings.GetSettingsFile();
            config.GetSection("DiscordConfig").Bind(DiscordConfigOptions);
            config.GetSection("BotConfig").Bind(BotConfigOptions);
            var log = new LoggerConfiguration().WriteTo.Console().CreateLogger();

            log.Information($"Settings dump: {JsonConvert.SerializeObject(BotConfigOptions)}");
            
            var botConfigSettings = config.GetSection("BotConfig");
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(botConfigSettings["Language"]);
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(botConfigSettings["Language"]);

            var services = new ServiceCollection();
            services.AddSingleton(DiscordConfigOptions);
            services.AddSingleton(BotConfigOptions);
            services.AddSingleton(log);
            services.AddDbContext<SqliteContext>(opt => opt.UseSqlite(@$"DataSource={LeviathanSettings.GetDatabaseFile(config)};"));
            services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig { GatewayIntents = GatewayIntents.All }));
            services.AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()));
            services.AddSingleton<CommandHandler>();

            return services.BuildServiceProvider();
        }

        public static Task Main(string[] args) => new Program().Start(args);

        public async Task Start(string[] args)
        {
            await using (var services = ConfigureServices())
            {
                var client = services.GetRequiredService<DiscordSocketClient>();
                var commands = services.GetRequiredService<InteractionService>();
                Log.Logger = services.GetRequiredService<Logger>();

                DiscordSocketClient = client;
                _commands = commands;

                client.Log += DiscordClientOnLog;
                client.UserJoined += ClientOnUserJoined;
                client.Ready += async () =>
                {
                    await _commands.RegisterCommandsToGuildAsync(DiscordConfigOptions.ServerGuildId);

                    //TODO high: move quartz to dependency injection
                    _scheduler = await new StdSchedulerFactory().GetScheduler();
                    await _scheduler.Start();
                    await CreateStartupJobs();
                };

                await client.LoginAsync(TokenType.Bot, DiscordConfigOptions.BotToken);
                await client.StartAsync();
                await services.GetRequiredService<CommandHandler>().InitializeAsync();

                await Task.Delay(Timeout.Infinite);
            }
        }

        private async Task ClientOnUserJoined(SocketGuildUser arg)
        {
            if (BotConfigOptions.WelcomeMessageEnabled &&
                BotConfigOptions.WelcomeMessageChannelId is not 0 &&
                !string.IsNullOrEmpty(BotConfigOptions.WelcomeMessage))
            {
                var discordServerGuild = DiscordSocketClient.GetGuild(DiscordConfigOptions.ServerGuildId);

                if (discordServerGuild is null)
                {
                    Log.Error($"Server guild with id: {DiscordConfigOptions.ServerGuildId} not found");
                }

                var discordChannel = await DiscordSocketClient.GetChannelAsync(BotConfigOptions.WelcomeMessageChannelId);

                if (discordChannel is IMessageChannel msgChannel)
                {
                    var message = BotConfigOptions.WelcomeMessage
                                                  .Replace("$user_mention", arg.Mention);

                    await msgChannel.SendMessageAsync(message);
                }
                else
                {
                    Log.Error($"Server welcome message channel with id: {BotConfigOptions.WelcomeMessageChannelId} not found");
                }
            }
        }

        private async Task CreateStartupJobs()
        {
            await QuartzJobHelper.SimplyCreateJob<UpdateDiscordNames>(_scheduler,
                "update_discord_names", x => x.RepeatForever().WithIntervalInMinutes(5));

            await QuartzJobHelper.SimplyCreateJob<UpdateDiscordRoles>(_scheduler,
                "update_discord_roles", x => x.RepeatForever().WithIntervalInMinutes(5));
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