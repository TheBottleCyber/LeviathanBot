using System.Linq;
using System.Net;
using ESI.NET;
using ESI.NET.Enumerations;
using Leviathan.Core.DatabaseContext;
using Leviathan.Core.Extensions;
using Leviathan.Core.Models;
using Leviathan.Core.Models.Database;
using Leviathan.Core.Models.Discord;
using Leviathan.Core.Models.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Quartz;

namespace Leviathan.Core.Controllers
{
    public class LinkController : Controller
    {
        private readonly MemoryContext _memoryContext;
        private readonly SqliteContext _sqliteContext;
        private readonly DiscordConfigOptions _discordConfigOptions;
        private readonly IOptions<EsiConfig> _esiConfig;

        public LinkController(MemoryContext memoryContext, SqliteContext sqliteContext,
            IOptions<DiscordConfigOptions> discordConfigOptions, IOptions<EsiConfig> esiConfig)
        {
            _memoryContext = memoryContext;
            _sqliteContext = sqliteContext;

            _discordConfigOptions = discordConfigOptions.Value;
            _esiConfig = esiConfig;
        }

        [HttpGet("/evecallback")]
        public async Task<IActionResult> EveEsi(string code, string state)
        {
            var user = await _memoryContext.Characters.FirstOrDefaultAsync(x => x.State == state);

            if (user is not null)
            {
                var esiClient = new EsiClient(_esiConfig);
                
                var esiToken = await esiClient.SSO.GetToken(GrantType.AuthorizationCode, code);
                var authorizedCharacter = await esiClient.SSO.Verify(esiToken);

                if (authorizedCharacter is not null)
                {
                    user.EsiCharacterName = authorizedCharacter.CharacterName;
                    user.EsiAllianceID = authorizedCharacter.AllianceID;
                    user.EsiCharacterID = authorizedCharacter.CharacterID;
                    user.EsiCorporationID = authorizedCharacter.CorporationID;
                    user.EsiTokenAccessToken = authorizedCharacter.Token;
                    user.EsiTokenExpiresIn = esiToken.ExpiresIn;
                    user.EsiTokenRefreshToken = authorizedCharacter.RefreshToken;
                    user.EsiSsoStatus = true;

                    await _memoryContext.SaveChangesAsync();

                    //TODO: place block to memory
                    if (user.EsiCorporationID is not 0)
                    {
                        if (!await _sqliteContext.Corporations.AnyAsync(x => x.CorporationId == user.EsiCorporationID))
                        {
                            var corporationResponse = await esiClient.Corporation.Information(user.EsiCorporationID);

                            if (corporationResponse.StatusCode == HttpStatusCode.OK)
                            {
                                var corporation = new Corporation(corporationResponse.Data.AllianceId,
                                    corporationResponse.Data.Name, user.EsiCorporationID, corporationResponse.Data.Ticker);

                                await _sqliteContext.Corporations.AddAsync(corporation);
                                await _sqliteContext.SaveChangesAsync();
                            }
                        }
                    }

                    //TODO: place block to memory
                    if (user.EsiAllianceID is not 0)
                    {
                        if (!await _sqliteContext.Alliances.AnyAsync(x => x.AllianceId == user.EsiAllianceID))
                        {
                            var allianceResponse = await esiClient.Alliance.Information(user.EsiAllianceID);

                            if (allianceResponse.StatusCode == HttpStatusCode.OK)
                            {
                                var alliance = new Alliance(allianceResponse.Data.Name, user.EsiAllianceID, allianceResponse.Data.Ticker);

                                await _sqliteContext.Alliances.AddAsync(alliance);
                                await _sqliteContext.SaveChangesAsync();
                            }
                        }
                    }

                    return Ok("You can close this window");
                }
            }

            return BadRequest();
        }

        [HttpGet("/discordcallback")]
        public async Task<IActionResult> Discord(string code, string state)
        {
            var user = await _memoryContext.Characters.FirstOrDefaultAsync(x => x.State == state);

            if (user is not null)
            {
                var discordToken = new DiscordTokenRequest(_discordConfigOptions.ClientId,
                    _discordConfigOptions.ClientSecret, code, _discordConfigOptions.CallbackUrl);

                //TODO: normal conversion
                var postData =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(discordToken))!;

                var discord_token = await DiscordHttpHelper.PostOauthToken<DiscordTokenResponse>(postData);
                var discordUser = await DiscordHttpHelper.GetDiscordCurrentUser<DiscordUser>(discord_token.AccessToken);
                var discordUserId = Convert.ToUInt64(discordUser.Id);

                user.DiscordUserId = discordUserId;
                await _memoryContext.SaveChangesAsync();

                return Ok("You can close this window");
            }

            return BadRequest();
        }

        [HttpGet("/proceed")]
        public async Task<IActionResult> Proceed(string state)
        {
            var user = await _memoryContext.Characters.FirstOrDefaultAsync(x => x.State == state);

            if (user is not null)
            {
                if (user.DiscordUserId != 0 && user.EsiCharacterID != 0)
                {
                    if (!await _sqliteContext.Characters.AnyAsync(x => x.EsiCharacterID == user.EsiCharacterID))
                    {
                        var tempUserId = user.Id;
                        user.Id = 0;
                        await _sqliteContext.AddAsync(user);
                        await _sqliteContext.SaveChangesAsync();

                        user.Id = tempUserId;
                        _memoryContext.Remove(user);
                        await _memoryContext.SaveChangesAsync();
                    }
                    else
                    {
                        var oldUser = await _sqliteContext.Characters.FirstOrDefaultAsync(x => x.EsiCharacterID == user.EsiCharacterID);

                        if (oldUser is not null)
                        {
                            oldUser.EsiCharacterName = user.EsiCharacterName;
                            oldUser.EsiAllianceID = user.EsiAllianceID;
                            oldUser.EsiCorporationID = user.EsiCorporationID;
                            oldUser.EsiTokenAccessToken = user.EsiTokenAccessToken;
                            oldUser.EsiTokenExpiresIn = user.EsiTokenExpiresIn;
                            oldUser.EsiTokenRefreshToken = user.EsiTokenRefreshToken;
                            oldUser.DiscordUserId = user.DiscordUserId;
                            oldUser.State = user.State;
                            oldUser.EsiSsoStatus = true;

                            await _sqliteContext.SaveChangesAsync();
                            _memoryContext.Characters.Remove(user);
                            await _memoryContext.SaveChangesAsync();
                        }
                    }

                    return Ok("You can close this window");
                }
                
                return BadRequest("Seems not all tokens provided, come back and reauth");
            }

            return NotFound("Invalid tokens, come back and reauth");
        }
    }
}