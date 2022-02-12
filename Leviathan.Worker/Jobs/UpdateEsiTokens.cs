using System.Reflection;
using ESI.NET;
using ESI.NET.Enumerations;
using ESI.NET.Models.Character;
using ESI.NET.Models.SSO;
using Leviathan.Core.DatabaseContext;
using Leviathan.Core.Extensions;
using Leviathan.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Quartz;
using Serilog;

namespace Leviathan.Jobs
{
    [DisallowConcurrentExecution]
    public class UpdateEsiTokens : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            using var log = new LoggerConfiguration().WriteTo.Console().CreateLogger();

            log.Information($"Job update_esi_tokens started");

            await using (var sqliteContext = new SqliteContext())
            {
                if (await sqliteContext.Characters.AnyAsync())
                {
                    var characters = sqliteContext.Characters.Where(x => !string.IsNullOrEmpty(x.EsiTokenAccessToken) &&
                                                                         !string.IsNullOrEmpty(x.EsiTokenRefreshToken));

                    var esiClient = new EsiClient(Program.EsiConfigOptions);
                    foreach (var character in characters)
                    {
                        log.Information($"Job update_esi_tokens at character_name: {character.EsiCharacterName}" +
                                        $" with character_id: {character.EsiCharacterID} token update started");

                        try
                        {
                            var token = await esiClient.SSO.GetToken(GrantType.RefreshToken, character.EsiTokenRefreshToken);
                            if (!string.IsNullOrEmpty(token.AccessToken) && !string.IsNullOrEmpty(token.RefreshToken))
                            {
                                character.EsiTokenAccessToken = token.AccessToken;
                                character.EsiTokenExpiresIn = token.ExpiresIn;
                                character.EsiTokenRefreshToken = token.RefreshToken;

                                character.EsiSsoStatus = true;
                                
                                log.Information($"Job update_esi_tokens at character_name: {character.EsiCharacterName}" +
                                                $" with character_id: {character.EsiCharacterID} token is valid");
                            }
                        }
                        catch (ArgumentException argumentException)
                        {
                            character.EsiSsoStatus = false;
                            
                            if (argumentException.Message == "Invalid refresh token. Token missing/expired.")
                            {
                                log.Information($"Job update_esi_tokens at character_name: {character.EsiCharacterName}" +
                                                $" with character_id: {character.EsiCharacterID} token missing/expired");
                            }
                            else if (argumentException.Message == "Invalid refresh token. Character grant missing/expired.")
                            {
                                log.Information($"Job update_esi_tokens at character_name: {character.EsiCharacterName}" +
                                                $" with character_id: {character.EsiCharacterID} token grant missing/expired.");
                            }
                            else
                            {
                                log.Error(argumentException, "Unhandled exception at job update_esi_tokens at" +
                                                             $" character_name: {character.EsiCharacterName} with character_id: {character.EsiCharacterID}");
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Error(ex, "Unhandled exception at job update_esi_tokens at" +
                                          $" character_name: {character.EsiCharacterName} with character_id: {character.EsiCharacterID}");
                        }

                        log.Information($"Job update_esi_tokens at character_name: {character.EsiCharacterName}" +
                                        $" with character_id: {character.EsiCharacterID} token update finished");
                    }

                    await sqliteContext.SaveChangesAsync();
                }
            }

            log.Information($"Job update_esi_tokens finised");
        }
    }
}