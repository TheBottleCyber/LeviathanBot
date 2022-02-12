using System.Diagnostics;
using Discord.Interactions;
using Leviathan.Bot.Services;

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
            var stringDate = $"{dateUntilProcessStarted.Days} Days {dateUntilProcessStarted.Hours} Hours {dateUntilProcessStarted.Minutes} Minutes {dateUntilProcessStarted.Seconds} Seconds";
            
            await RespondAsync($"Leviathan v1.0.0 - EVE Online Discord Bot\n" +
                               "Developer: TheBottle (In-game Name: The Bottle)\n\n" +
                               $"Run Time: {stringDate}");
        }
        
        [SlashCommand("time", "eve online time")]
        public async Task Time()
        {
            await RespondAsync($"Time in EVE Online now: {DateTime.UtcNow}");
        }
    }
}