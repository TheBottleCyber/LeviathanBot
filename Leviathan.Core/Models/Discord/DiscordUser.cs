using Newtonsoft.Json;

namespace Leviathan.Core.Models.Discord
{
    public class DiscordUser
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }
    }
}