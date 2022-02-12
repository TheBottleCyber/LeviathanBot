using Microsoft.Extensions.Configuration;

namespace Leviathan.Core.Extensions
{
    public class LeviathanSettings
    {
        public static IConfigurationRoot GetSettingsFile()
        {
            #if DEBUG
            var settingsFile = "C:/bot/settings.json";

            if (Environment.OSVersion.Platform != PlatformID.Win32NT) settingsFile = "/opt/leviathan/settings.json";
            #else
            var settingsFile = "settings.json";
            #endif

            var settingsFileFromEnvironment = Environment.GetEnvironmentVariable("LEVIATHAN_SETTINGS_FILE");
            if (!string.IsNullOrEmpty(settingsFileFromEnvironment)) settingsFile = settingsFileFromEnvironment;

            var config = new ConfigurationBuilder()
                         .AddJsonFile(settingsFile)
                         .Build();

            return config;
        }

        public static string GetDatabaseFile(IConfiguration configurationRoot)
        {
            return configurationRoot.GetSection("DatabaseConfig").GetValue<string>("ConnectionString");
        }
    }
}