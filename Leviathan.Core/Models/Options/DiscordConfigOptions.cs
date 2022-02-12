using System.Text.Json.Serialization;

namespace Leviathan.Core.Models.Options
{
    public class DiscordConfigOptions
    {
        public ulong ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string BotToken { get; set; }
        public string CallbackUrl { get; set; }
        public ulong ServerGuildId { get; set; }
    }
}