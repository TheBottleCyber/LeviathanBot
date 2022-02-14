using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Leviathan.Core.Models.Options
{
    public class BotConfig
    {
        public string Language { get; set; }
        public List<string> PingCommandPeoples { get; set; }
        public string PingCommandMessage { get; set; }
        public bool WelcomeMessageEnabled { get; set; }
        public string WelcomeMessage { get; set; }
        public ulong WelcomeMessageChannelId { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public bool EnforceCorporationTicker { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public bool EnforceAllianceTicker { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public bool EnforceCharacterName { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public bool RemoveRolesIfTokenIsInvalid { get; set; }
        
        public List<AuthGroups> AuthGroups { get; set; }
    }
    
    public class AuthGroups
    {
        public List<string> AllowedCorporations { get; set; } = new List<string>();
        public List<string> AllowedCharacters { get; set; } = new List<string>();
        public List<string> AllowedAlliances { get; set; } = new List<string>();
        
        [JsonProperty(Required = Required.Always)]
        public List<string> DiscordRoles { get; set; }
    }
}