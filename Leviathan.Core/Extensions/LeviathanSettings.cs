using System.Diagnostics;
using ESI.NET;
using Leviathan.Core.Models.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Leviathan.Core.Extensions
{
    public static class LeviathanSettings
    {
        public static Settings GetSettingsFile(Logger? logger = null)
        {
            logger ??= new LoggerConfiguration()
                       .WriteTo.Console()
                       .CreateLogger();

            #if DEBUG
            var settingsFile = "C:/bot/settings.json";

            if (Environment.OSVersion.Platform != PlatformID.Win32NT) settingsFile = "/opt/leviathan/settings.json";
            #else
            var settingsFile = "settings.json";
            #endif

            var settingsFileFromEnvironment = Environment.GetEnvironmentVariable("LEVIATHAN_SETTINGS_FILE");
            if (!string.IsNullOrEmpty(settingsFileFromEnvironment)) settingsFile = settingsFileFromEnvironment;

            if (File.Exists(settingsFile))
            {
                logger.Debug($"Using settings.json located in {new FileInfo(settingsFile).FullName}");

                var settings = new ConfigurationBuilder()
                               .AddJsonFile(settingsFile)
                               .Build().Get<Settings>();

                // VerifySettingsFile(logger, settings);

                return settings;
            }
            else
            {
                const string exceptionText = "File settings.json not found";
                var exception = new FileNotFoundException(exceptionText);

                logger.Fatal(exception, exceptionText);
                throw exception;
            }
        }

        // public static void VerifySettingsFile(Logger logger, Settings settings)
        // {
        //     logger.Information("Verifying settings file started");
        //     logger.Debug($"Settings dump: {JsonConvert.SerializeObject(settings)}");
        //
        //     if (string.IsNullOrEmpty(settings.ESIConfig.CallbackUrl)) ;
        //
        //     logger.Information("Verifying settings file finished");
        // }

        public static string GetDatabaseFile(Settings settings)
        {
            var dbFile = settings.DatabaseConfig.ConnectionString;
            // var dbFileDirectory = new DirectoryInfo(dbFile);
            //
            // if (!File.Exists(dbFile) || !dbFileDirectory.Exists) throw new DirectoryNotFoundException($"Directory {dbFileDirectory.FullName} not exists");
            //     
            return dbFile;
        }
    }
}