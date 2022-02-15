using ESI.NET;
using ESI.NET.Enumerations;
using Leviathan.Core.DatabaseContext;
using Leviathan.Core.Models.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Quartz;
using Serilog;

namespace Leviathan.Jobs
{
    [DisallowConcurrentExecution]
    public class UpdateEsiTokens : IJob
    {
        private SqliteContext _sqliteContext;
        private ILogger _logger;
        private Settings _settings;

        public UpdateEsiTokens(SqliteContext sqliteContext, ILogger logger, Settings settings)
        {
            _sqliteContext = sqliteContext;
            _logger = logger;
            _settings = settings;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.Information($"Job update_esi_tokens started");

            if (await _sqliteContext.Characters.AnyAsync())
            {
                var characters = _sqliteContext.Characters.Where(x => !string.IsNullOrEmpty(x.EsiTokenAccessToken) &&
                                                                      !string.IsNullOrEmpty(x.EsiTokenRefreshToken));

                var esiClient = new EsiClient(_settings.ESIConfig);
                foreach (var character in characters)
                {
                    try
                    {
                        var token = await esiClient.SSO.GetToken(GrantType.RefreshToken, character.EsiTokenRefreshToken);
                        if (!string.IsNullOrEmpty(token.AccessToken) && !string.IsNullOrEmpty(token.RefreshToken))
                        {
                            character.EsiTokenAccessToken = token.AccessToken;
                            character.EsiTokenExpiresIn = token.ExpiresIn;
                            character.EsiTokenRefreshToken = token.RefreshToken;

                            character.EsiSsoStatus = true;

                            _logger.Information($"Job update_esi_tokens at character_name: {character.EsiCharacterName}" +
                                                $" with character_id: {character.EsiCharacterID} token is valid");
                        }
                    }
                    catch (ArgumentException argumentException)
                    {
                        character.EsiSsoStatus = false;

                        if (argumentException.Message == "Invalid refresh token. Token missing/expired.")
                        {
                            _logger.Information($"Job update_esi_tokens at character_name: {character.EsiCharacterName}" +
                                                $" with character_id: {character.EsiCharacterID} token missing/expired");
                        }
                        else if (argumentException.Message == "Invalid refresh token. Character grant missing/expired.")
                        {
                            _logger.Information($"Job update_esi_tokens at character_name: {character.EsiCharacterName}" +
                                                $" with character_id: {character.EsiCharacterID} token grant missing/expired.");
                        }
                        else
                        {
                            _logger.Error(argumentException, "Unhandled exception at job update_esi_tokens at" +
                                                             $" character_name: {character.EsiCharacterName} with character_id: {character.EsiCharacterID}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Unhandled exception at job update_esi_tokens at" +
                                          $" character_name: {character.EsiCharacterName} with character_id: {character.EsiCharacterID}");
                    }
                }

                await _sqliteContext.SaveChangesAsync();
            }

            _logger.Information($"Job update_esi_tokens finised");
        }
    }
}