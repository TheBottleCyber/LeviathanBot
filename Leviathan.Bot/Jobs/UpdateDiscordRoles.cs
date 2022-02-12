using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Leviathan.Core.DatabaseContext;
using Leviathan.Core.Models.Database;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Quartz;
using Serilog;

namespace Leviathan.Bot.Jobs
{
    [DisallowConcurrentExecution]
    public class UpdateDiscordRoles : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var log = new LoggerConfiguration().WriteTo.Console().CreateLogger();

            log.Information("Job update_discord_roles started");
            var discordServerGuild = Program.DiscordSocketClient.GetGuild(Program.DiscordConfigOptions.ServerGuildId);

            if (discordServerGuild is not null)
            {
                await using (var sqliteContext = new SqliteContext())
                {
                    await discordServerGuild.DownloadUsersAsync();

                    foreach (var discordUser in discordServerGuild.Users.Where(x => !x.IsBot))
                    {
                        var character = await sqliteContext.Characters.FirstOrDefaultAsync(x => x.DiscordUserId == discordUser.Id);

                        if (character is not null)
                        {
                            var corporation = await sqliteContext.Corporations.FirstOrDefaultAsync(x => x.CorporationId == character.EsiCorporationID);
                            var alliance = await sqliteContext.Alliances.FirstOrDefaultAsync(x => x.AllianceId == character.EsiAllianceID);

                            var listRoles = new Dictionary<string, bool>();
                            foreach (var authGroup in Program.BotConfigOptions.AuthGroups)
                            {
                                bool assigmentBoolean = false;

                                if (authGroup.AllowedCharacters.Count > 0 &&
                                    authGroup.AllowedCharacters.Contains(character.EsiCharacterName))
                                {
                                    log.Information($"Job update_discord_roles user with username: {discordUser.Username}#{discordUser.DiscriminatorValue}" +
                                                    $" matched character name: {character.EsiCharacterName}");
                                    
                                    assigmentBoolean = true;
                                }

                                if (authGroup.AllowedCorporations.Count > 0 &&
                                    authGroup.AllowedCorporations.Contains(corporation!.Ticker))
                                {
                                    log.Information($"Job update_discord_roles user with username: {discordUser.Username}#{discordUser.DiscriminatorValue}" +
                                                    $" matched corporation ticker: {corporation.Ticker}");
                                    
                                    assigmentBoolean = true;
                                }

                                if (authGroup.AllowedAlliances.Count > 0 &&
                                    alliance is not null &&
                                    authGroup.AllowedAlliances.Contains(alliance.Ticker))
                                {
                                    log.Information($"Job update_discord_roles user with username: {discordUser.Username}#{discordUser.DiscriminatorValue}" +
                                                    $" matched alliance ticker: {alliance.Ticker}");
                                    
                                    assigmentBoolean = true;
                                }

                                if (Program.BotConfigOptions.RemoveRolesIfTokenIsInvalid &&
                                    !character.EsiSsoStatus)
                                {
                                    log.Information($"Job update_discord_roles user with username: {discordUser.Username}#{discordUser.DiscriminatorValue}" +
                                                    $" esi token invalid, removing roles");
                                    
                                    assigmentBoolean = false;
                                }
                                
                                foreach (var role in authGroup.DiscordRoles)
                                {
                                    listRoles.Add(role, assigmentBoolean);
                                }
                            }

                            var discordUserRoles = discordUser.Roles;
                            var discordGuildRoles = discordServerGuild.Roles;

                            foreach (var role in listRoles)
                            {
                                var discordUserRole = discordUserRoles.FirstOrDefault(x => x.Name == role.Key);
                                var discordGuildRole = discordGuildRoles.FirstOrDefault(x => x.Name == role.Key);

                                if (discordGuildRole is not null)
                                {
                                    if (discordUserRole is null)
                                    {
                                        if (role.Value)
                                        {
                                            log.Information($"Job update_discord_roles adding role {discordGuildRole.Name} at user with username: {discordUser.Username}#{discordUser.DiscriminatorValue}");

                                            await discordUser.AddRoleAsync(discordGuildRole, RequestOptions.Default);
                                        }
                                    }
                                    else
                                    {
                                        if (!role.Value)
                                        {
                                            log.Information($"Job update_discord_roles removing role {discordGuildRole.Name} at user with username: {discordUser.Username}#{discordUser.DiscriminatorValue}");
                                            
                                            await discordUser.RemoveRoleAsync(discordGuildRole, RequestOptions.Default);
                                        }
                                    }
                                }
                                else
                                {
                                    log.Error($"Job update_discord_roles role with name: {role.Key} not found");
                                }
                            }
                        }
                        else
                        {
                            if (Program.BotConfigOptions.RemoveRolesIfTokenIsInvalid)
                            {
                                var overAllConfigRoles = new List<string>();

                                foreach (var authGroup in Program.BotConfigOptions.AuthGroups)
                                {
                                    overAllConfigRoles.AddRange(authGroup.DiscordRoles);
                                }

                                var allRolesDefined = new List<IRole>();
                                foreach (var role in discordServerGuild.Roles)
                                {
                                    if (overAllConfigRoles.Contains(role.Name))
                                    {
                                        allRolesDefined.Add(role);
                                    }
                                }

                                if (allRolesDefined.Count > 0)
                                {
                                    foreach (var role in allRolesDefined.Where(role => discordUser.Roles.Contains(role)))
                                    {
                                        try
                                        {
                                            log.Information($"Job update_discord_roles try remove role \"{role.Name}\" at user: " +
                                                            $"{discordUser.Username}#{discordUser.DiscriminatorValue}");

                                            await discordUser.RemoveRoleAsync(role);

                                            log.Information($"Job update_discord_roles remove role \"{role.Name}\" at user: " +
                                                            $"{discordUser.Username}#{discordUser.DiscriminatorValue} success");
                                        }
                                        catch (Exception ex)
                                        {
                                            log.Error(ex, "Job update_discord_roles unhandled exception");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                log.Error($"Job update_discord_roles server guild with id: {Program.DiscordConfigOptions.ServerGuildId} not found");
            }

            log.Information("Job update_discord_roles finished");
        }
    }
}