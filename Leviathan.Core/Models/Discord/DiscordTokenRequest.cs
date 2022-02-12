using Newtonsoft.Json;

namespace Leviathan.Core.Models.Discord
{
    public class DiscordTokenRequest
    {
        [JsonProperty("client_id")]
        public ulong Id { get; set; }
        
        [JsonProperty("client_secret")]
        public string ClientSecret { get; set; }
        
        [JsonProperty("grant_type")]
        public string GrantType = "authorization_code";
        
        [JsonProperty("code")]
        public string Code { get; set; }
        
        [JsonProperty("redirect_uri")]
        public string RedirectUrl { get; set; }

        public DiscordTokenRequest(ulong id, string clientSecret, string code, string redirectUrl)
        {
            Id = id;
            ClientSecret = clientSecret;
            Code = code;
            RedirectUrl = redirectUrl;
        }
    }

}