using ESI.NET;
using Newtonsoft.Json;

namespace Leviathan.Core.Models.Options
{
    public class Settings
    {
        public EsiConfig ESIConfig { get; set; }
        public DiscordConfig DiscordConfig { get; set; }
        public DatabaseConfig DatabaseConfig { get; set; }
        public BotConfig BotConfig { get; set; }
    }
}