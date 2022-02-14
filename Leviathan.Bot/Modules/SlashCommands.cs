using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Leviathan.Bot.Services;
using Leviathan.Core.DatabaseContext;
using Leviathan.Core.Localization;
using Leviathan.Core.Models.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Leviathan.Bot.Modules
{
    public class SlashCommands : InteractionModuleBase<SocketInteractionContext>
    {
        public InteractionService Commands { get; set; } = null!;
        private CommandHandler _handler;
        private BotConfig _botConfig;
        private SqliteContext _sqliteContext;

        public SlashCommands(CommandHandler handler, BotConfig botConfig, SqliteContext sqliteContext)
        {
            _handler = handler;
            _botConfig = botConfig;
            _sqliteContext = sqliteContext;
        }

        [SlashCommand("about", "about bot")]
        public async Task About()
        {
            var dateUntilProcessStarted = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
            var daysLocalizedString = LocalizationHelper.GetLocalizedString("Days");
            var hoursLocalizedString = LocalizationHelper.GetLocalizedString("Hours");
            var minutesLocalizedString = LocalizationHelper.GetLocalizedString("Minutes");
            var secondsLocalizedString = LocalizationHelper.GetLocalizedString("Seconds");

            var stringDate = $"{dateUntilProcessStarted.Days} {daysLocalizedString} " +
                             $"{dateUntilProcessStarted.Hours} {hoursLocalizedString} " +
                             $"{dateUntilProcessStarted.Minutes} {minutesLocalizedString} " +
                             $"{dateUntilProcessStarted.Seconds} {secondsLocalizedString}";

            var runTimeLocalizedString = LocalizationHelper.GetLocalizedString("DiscordAboutCommandRunTime");
            var devLocalizedString = LocalizationHelper.GetLocalizedString("Developer");
            var inGameNickLocalizedString = LocalizationHelper.GetLocalizedString("DiscordAboutCommandInGameNick");

            await RespondAsync($"Leviathan v1.0.4 - EVE Online Discord Bot\n" +
                               $"{devLocalizedString}: TheBottle ({inGameNickLocalizedString} The Bottle)\n\n" +
                               $"{runTimeLocalizedString} {stringDate}");
        }

        [SlashCommand("time", "eve online time")]
        public async Task Time()
        {
            await RespondAsync($"{LocalizationHelper.GetLocalizedString("DiscordTimeCommand")} {DateTime.UtcNow}");
        }

        [SlashCommand("ping", "ping all listed peoples")]
        public async Task Ping()
        {
            var peoplesToPing = _botConfig.PingCommandPeoples;

            if (peoplesToPing is not null && peoplesToPing.Count > 0)
            {
                var discordIdsToPing = await _sqliteContext.Characters
                                                           .Where(x => peoplesToPing.Contains(x.EsiCharacterName))
                                                           .Select(x => x.DiscordUserId)
                                                           .ToListAsync();

                var mentionString = new StringBuilder();
                if (discordIdsToPing is not null && discordIdsToPing.Count > 0)
                {
                    foreach (var id in discordIdsToPing)
                    {
                        mentionString.Append($"{MentionUtils.MentionUser(id)} ");
                    }
                }

                if (mentionString.Length > 0)
                    await RespondAsync($"{mentionString}{_botConfig.PingCommandMessage}");
            }
        }
    }
}