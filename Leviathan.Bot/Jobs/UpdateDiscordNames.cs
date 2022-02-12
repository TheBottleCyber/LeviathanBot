using Discord.WebSocket;
using ESI.NET;
using Leviathan.Core.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Quartz;
using Serilog;

namespace Leviathan.Bot.Jobs
{
    [DisallowConcurrentExecution]
    public class UpdateDiscordNames : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var log = new LoggerConfiguration().WriteTo.Console().CreateLogger();
            
            log.Information("Job update_discord_names started");
            var discordServerGuild = Program.DiscordSocketClient.GetGuild(Program.DiscordConfigOptions.ServerGuildId);

            if (discordServerGuild is not null)
            {
                await using (var sqliteContext = new SqliteContext())
                {
                    if (await sqliteContext.Characters.AnyAsync())
                    {
                        foreach (var character in sqliteContext.Characters)
                        {
                            var discordUser = discordServerGuild.GetUser(character.DiscordUserId);
                            if (discordUser is null) continue;

                            var corporation = await sqliteContext.Corporations.FirstOrDefaultAsync(x => x.CorporationId == character.EsiCorporationID);
                            var alliance = await sqliteContext.Alliances.FirstOrDefaultAsync(x => x.AllianceId == character.EsiAllianceID);

                            var discordNicknameNeeded = "";

                            if (Program.BotConfigOptions.EnforceAllianceTicker)
                            {
                                var allianceTicker = "NULL";
                                if (alliance is not null)
                                {
                                    allianceTicker = alliance.Ticker;
                                }

                                discordNicknameNeeded += $"[{allianceTicker}] ";
                            }

                            if (Program.BotConfigOptions.EnforceCorporationTicker) discordNicknameNeeded += $"[{corporation!.Ticker}] ";
                            discordNicknameNeeded += Program.BotConfigOptions.EnforceCharacterName ? character.EsiCharacterName : discordUser.Username;

                            if (discordUser.Nickname != discordNicknameNeeded)
                            {
                                try
                                {
                                    log.Information($"Job update_discord_names trying rename user with username: {discordUser.Username}#{discordUser.Discriminator} to nickname: {discordNicknameNeeded}");
                                    await discordUser.ModifyAsync(x => { x.Nickname = discordNicknameNeeded; });
                                    log.Information($"Job update_discord_names rename user with username: {discordUser.Username}#{discordUser.Discriminator} success");
                                }
                                catch (Discord.Net.HttpException)
                                {
                                    log.Warning($"Job update_discord_names cannot rename user with username: {discordUser.Username}#{discordUser.Discriminator} not enough priveleges");
                                }
                                catch (Exception ex)
                                {
                                    log.Error(ex, $"Job update_discord_names ModifyAsync user with username: {discordUser.Username}#{discordUser.Discriminator} failed");
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                log.Error($"Job update_discord_names server guild with id: {Program.DiscordConfigOptions.ServerGuildId} not found");
            }

            log.Information("Job update_discord_names finished");
        }
    }
}