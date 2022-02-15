using Discord.Net;
using Discord.WebSocket;
using Leviathan.Core.DatabaseContext;
using Leviathan.Core.Models.Options;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Serilog;

namespace Leviathan.Bot.Jobs
{
    [DisallowConcurrentExecution]
    public class UpdateDiscordNames : IJob
    {
        private SqliteContext _sqliteContext;
        private ILogger _logger;
        private Settings _settings;
        private DiscordSocketClient _discordSocketClient;

        public UpdateDiscordNames(SqliteContext sqliteContext, ILogger logger, Settings settings, DiscordSocketClient discordSocketClient)
        {
            _sqliteContext = sqliteContext;
            _logger = logger;
            _settings = settings;
            _discordSocketClient = discordSocketClient;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.Information("Job update_discord_names started");
            var discordServerGuild = _discordSocketClient.GetGuild(_settings.DiscordConfig.ServerGuildId);

            if (discordServerGuild is not null)
            {
                if (await _sqliteContext.Characters.AnyAsync())
                {
                    foreach (var character in _sqliteContext.Characters)
                    {
                        var discordUser = discordServerGuild.GetUser(character.DiscordUserId);
                        if (discordUser is null) continue;

                        var corporation = await _sqliteContext.Corporations.FirstOrDefaultAsync(x => x.CorporationId == character.EsiCorporationID);
                        var alliance = await _sqliteContext.Alliances.FirstOrDefaultAsync(x => x.AllianceId == character.EsiAllianceID);

                        var discordNicknameNeeded = "";

                        if (_settings.BotConfig.EnforceAllianceTicker)
                        {
                            var allianceTicker = "NULL";
                            if (alliance is not null)
                            {
                                allianceTicker = alliance.Ticker;
                            }

                            discordNicknameNeeded += $"[{allianceTicker}] ";
                        }

                        if (_settings.BotConfig.EnforceCorporationTicker && corporation is not null) discordNicknameNeeded += $"[{corporation.Ticker}] ";
                        discordNicknameNeeded += _settings.BotConfig.EnforceCharacterName ? character.EsiCharacterName : discordUser.Username;

                        if (discordUser.Nickname != discordNicknameNeeded)
                        {
                            try
                            {
                                _logger.Information($"Job update_discord_names trying rename user with username: {discordUser.Username}#{discordUser.Discriminator} to nickname: {discordNicknameNeeded}");
                                await discordUser.ModifyAsync(x => { x.Nickname = discordNicknameNeeded; });
                                _logger.Information($"Job update_discord_names rename user with username: {discordUser.Username}#{discordUser.Discriminator} success");
                            }
                            catch (HttpException)
                            {
                                _logger.Warning($"Job update_discord_names cannot rename user with username: {discordUser.Username}#{discordUser.Discriminator} not enough priveleges");
                            }
                            catch (Exception ex)
                            {
                                _logger.Error(ex, $"Job update_discord_names ModifyAsync user with username: {discordUser.Username}#{discordUser.Discriminator} failed");
                            }
                        }
                    }
                }
            }
            else
            {
                _logger.Error($"Job update_discord_names server guild with id: {_settings.DiscordConfig.ServerGuildId} not found");
            }

            _logger.Information("Job update_discord_names finished");
        }
    }
}