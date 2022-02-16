using ESI.NET;
using ESI.NET.Enumerations;
using Leviathan.Core.DatabaseContext;
using Leviathan.Core.Models.Options;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Serilog;

namespace Leviathan.Worker.Jobs.Runtime
{
    [DisallowConcurrentExecution]
    public class RuntimeUpdateEsiToken : IJob
    {
        private SqliteContext _sqliteContext;
        private ILogger _logger;
        private Settings _settings;

        public RuntimeUpdateEsiToken(SqliteContext sqliteContext, ILogger logger, Settings settings)
        {
            _sqliteContext = sqliteContext;
            _logger = logger;
            _settings = settings;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var characterId = context.JobDetail.JobDataMap.GetInt("character_id");
            
            _logger.Information($"Job {context.JobDetail.Key} started");
            
            var character = await _sqliteContext.Characters.FirstOrDefaultAsync(x => x.EsiCharacterID == characterId);

            if (character is not null)
            {
                var esiClient = new EsiClient(_settings.ESIConfig);

                try
                {
                    var token = await esiClient.SSO.GetToken(GrantType.RefreshToken, character.EsiTokenRefreshToken);
                    if (!string.IsNullOrEmpty(token.AccessToken) && !string.IsNullOrEmpty(token.RefreshToken))
                    {
                        character.EsiTokenAccessToken = token.AccessToken;
                        character.EsiTokenExpiresOn = DateTime.UtcNow.AddSeconds(token.ExpiresIn - 300);
                        character.EsiTokenRefreshToken = token.RefreshToken;
                        character.EsiSsoStatus = true;

                        _logger.Information($"Job {context.JobDetail.Key} at character_name: {character.EsiCharacterName} token is valid");
                    }
                    else
                    {
                        character.EsiSsoStatus = false;
                    }
                }
                catch (ArgumentException argumentException)
                {
                    switch (argumentException.Message)
                    {
                        case "Invalid refresh token. Token missing/expired.":
                            _logger.Information($"Job {context.JobDetail.Key} at character_name: {character.EsiCharacterName} token missing/expired");
                            break;
                        case "Invalid refresh token. Character grant missing/expired.":
                            _logger.Information($"Job {context.JobDetail.Key} at character_name: {character.EsiCharacterName} token grant missing/expired.");
                            break;
                        default:
                            _logger.Error(argumentException, $"Unhandled exception at job {context.JobDetail.Key} at character_name: {character.EsiCharacterName}");
                            break;
                    }

                    character.EsiSsoStatus = false;
                }
                catch (Exception ex)
                {
                    character.EsiSsoStatus = false;

                    _logger.Error(ex, $"Unhandled exception at job {context.JobDetail.Key} at character_name: {character.EsiCharacterName}");
                }
                finally
                {
                    await _sqliteContext.SaveChangesAsync();
                }
            }
            
            _logger.Information($"Job {context.JobDetail.Key} finished");
        }
    }
}