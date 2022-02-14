using Newtonsoft.Json;

namespace Leviathan.Core.Models.Options
{
    public class DatabaseConfig
    {
        [JsonProperty(Required = Required.Always)]
        public string ConnectionString { get; set; }
    }
}