using System.Text.Json.Serialization;

namespace Leviathan.Core.Models.Options
{
    public class BotConfigOptions
    {
        public List<string> DiscordAdminRoles { get; set; }
        public string Language { get; set; }
        public bool WelcomeMessageEnabled { get; set; }
        public string WelcomeMessage { get; set; }
        public ulong WelcomeMessageChannelId { get; set; }
        public bool EnforceCorporationTicker { get; set; }
        public bool EnforceAllianceTicker { get; set; }
        public bool EnforceCharacterName { get; set; }
        public bool RemoveRolesIfTokenIsInvalid { get; set; }
        public List<AuthGroups> AuthGroups { get; set; }
    }
    
    public class AuthGroups
    {
        public List<string> AllowedCorporations { get; set; } = new List<string>();
        public List<string> AllowedCharacters { get; set; } = new List<string>();
        public List<string> AllowedAlliances { get; set; } = new List<string>();
        public List<string> DiscordRoles { get; set; }
    }
}