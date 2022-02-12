using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Leviathan.Core.Extensions
{
    public static class DiscordHttpHelper
    {
        public static async Task<T> PostOauthToken<T>(IEnumerable<KeyValuePair<string, string>> postData)
        {
            using (var client = new HttpClient())
            {
                using (var content = new FormUrlEncodedContent(postData))
                {
                    content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");

                    var result = await client.PostAsync("https://discord.com/api/oauth2/token", content);
                    result.EnsureSuccessStatusCode();
                    string resultContentString = await result.Content.ReadAsStringAsync();
                    T resultContent = JsonConvert.DeserializeObject<T>(resultContentString)!;
                    
                    return resultContent;
                }
            }
        }
        
        public static async Task<T> GetDiscordCurrentUser<T>(string bearerToken)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
                var result = await client.GetAsync("https://discordapp.com/api/users/@me");
                result.EnsureSuccessStatusCode();
                string resultContentString = await result.Content.ReadAsStringAsync();
                T resultContent = JsonConvert.DeserializeObject<T>(resultContentString)!;

                return resultContent;
            }
        }
    }
}