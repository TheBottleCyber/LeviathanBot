using System.Diagnostics;
using System.Text;
using Discord;
using Discord.Interactions;
using Humanizer;
using Leviathan.Bot.Services;
using Leviathan.Core.DatabaseContext;
using Leviathan.Core.Localization;
using Leviathan.Core.Models.Options;
using Microsoft.EntityFrameworkCore;

namespace Leviathan.Bot.Modules
{
    public class SlashCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private CommandHandler _handler;
        private Settings _settings;
        private SqliteContext _sqliteContext;

        public SlashCommands(CommandHandler handler, SqliteContext sqliteContext, Settings settings)
        {
            _handler = handler;
            _sqliteContext = sqliteContext;
            _settings = settings;
        }

        public InteractionService Commands { get; set; } = null!;

        [SlashCommand("about", "about bot")]
        public async Task About()
        {
            var dateUntilProcessStarted = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
            var stringDate = dateUntilProcessStarted.Humanize();
            
            var runTimeLocalizedString = LocalizationHelper.GetLocalizedString("DiscordAboutCommandRunTime");
            var devLocalizedString = LocalizationHelper.GetLocalizedString("Developer");
            var inGameNickLocalizedString = LocalizationHelper.GetLocalizedString("DiscordAboutCommandInGameNick");

            await RespondAsync("Leviathan v1.1.0 - EVE Online Discord Bot\n" +
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
            var peoplesToPing = _settings.BotConfig.PingCommandPeoples;

            if (peoplesToPing is not null && peoplesToPing.Count > 0)
            {
                var discordIdsToPing = await _sqliteContext.Characters
                                                           .Where(x => peoplesToPing.Contains(x.EsiCharacterName))
                                                           .Select(x => x.DiscordUserId)
                                                           .ToListAsync();

                var mentionString = new StringBuilder();
                if (discordIdsToPing is not null && discordIdsToPing.Count > 0)
                    foreach (var id in discordIdsToPing)
                        mentionString.Append($"{MentionUtils.MentionUser(id)} ");

                if (mentionString.Length > 0)
                    await RespondAsync($"{mentionString}{_settings.BotConfig.PingCommandMessage}");
            }
        }
    }
}