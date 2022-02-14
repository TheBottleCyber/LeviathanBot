using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Discord.Interactions;
using Leviathan.Bot.Services;
using Leviathan.Core.Localization;

namespace Leviathan.Bot.Modules
{
    public class SlashCommands : InteractionModuleBase<SocketInteractionContext>
    {
        public InteractionService Commands { get; set; } = null!;
        private CommandHandler _handler;
        
        public SlashCommands(CommandHandler handler)
        {
            _handler = handler;
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
            
            await RespondAsync($"Leviathan v1.0.3 - EVE Online Discord Bot\n" +
                               $"{devLocalizedString}: TheBottle ({inGameNickLocalizedString} The Bottle)\n\n" +
                               $"{runTimeLocalizedString} {stringDate}");
        }
        
        [SlashCommand("time", "eve online time")]
        public async Task Time()
        {
            await RespondAsync($"{LocalizationHelper.GetLocalizedString("DiscordTimeCommand")} {DateTime.UtcNow}");
        }
    }
}