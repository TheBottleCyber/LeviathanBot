using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Leviathan.Core.Models.Options
{
    public class DiscordConfig
    {
        [JsonProperty(Required = Required.Always)]
        public ulong ClientId { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public string ClientSecret { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public string BotToken { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public string CallbackUrl { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public ulong ServerGuildId { get; set; }
    }
}