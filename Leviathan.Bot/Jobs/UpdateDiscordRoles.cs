using Discord;
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
    public class UpdateDiscordRoles : IJob
    {
        private SqliteContext _sqliteContext;
        private ILogger _logger;
        private Settings _settings;
        private DiscordSocketClient _discordSocketClient;

        public UpdateDiscordRoles(SqliteContext sqliteContext, ILogger logger, Settings settings, DiscordSocketClient discordSocketClient)
        {
            _sqliteContext = sqliteContext;
            _logger = logger;
            _settings = settings;
            _discordSocketClient = discordSocketClient;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.Information($"Job {context.JobDetail.Key} started");
            
            if (_discordSocketClient.ConnectionState != ConnectionState.Connected)
            {
                _logger.Warning($"Job {context.JobDetail.Key} skipped because discord client connection state is {_discordSocketClient.ConnectionState}");
                return;
            }
            
            var discordServerGuild = _discordSocketClient.GetGuild(_settings.DiscordConfig.ServerGuildId);

            if (discordServerGuild is not null)
            {
                await discordServerGuild.DownloadUsersAsync();

                foreach (var discordUser in discordServerGuild.Users.Where(x => !x.IsBot))
                {
                    var character = await _sqliteContext.Characters.FirstOrDefaultAsync(x => x.DiscordUserId == discordUser.Id);

                    if (character is not null)
                    {
                        var corporation = await _sqliteContext.Corporations.FirstOrDefaultAsync(x => x.CorporationId == character.EsiCorporationID);
                        var alliance = await _sqliteContext.Alliances.FirstOrDefaultAsync(x => x.AllianceId == character.EsiAllianceID);

                        var listRoles = new Dictionary<string, bool>();
                        foreach (var authGroup in _settings.BotConfig.AuthGroups)
                        {
                            bool assigmentBoolean = false;

                            if (authGroup.AllowedCharacters is not null && authGroup.AllowedCharacters.Count > 0 &&
                                authGroup.AllowedCharacters.Contains(character.EsiCharacterName))
                            {
                                _logger.Information($"Job {context.JobDetail.Key} user with username: {discordUser.Username}#{discordUser.DiscriminatorValue}" +
                                                    $" matched character name: {character.EsiCharacterName}");

                                assigmentBoolean = true;
                            }

                            if (authGroup.AllowedCorporations is not null && authGroup.AllowedCorporations.Count > 0 &&
                                corporation is not null &&
                                authGroup.AllowedCorporations.Contains(corporation.Ticker))
                            {
                                _logger.Information($"Job {context.JobDetail.Key} user with username: {discordUser.Username}#{discordUser.DiscriminatorValue}" +
                                                    $" matched corporation ticker: {corporation.Ticker}");

                                assigmentBoolean = true;
                            }

                            if (authGroup.AllowedAlliances is not null && authGroup.AllowedAlliances.Count > 0 &&
                                alliance is not null &&
                                authGroup.AllowedAlliances.Contains(alliance.Ticker))
                            {
                                _logger.Information($"Job {context.JobDetail.Key} user with username: {discordUser.Username}#{discordUser.DiscriminatorValue}" +
                                                    $" matched alliance ticker: {alliance.Ticker}");

                                assigmentBoolean = true;
                            }

                            if (_settings.BotConfig.RemoveRolesIfTokenIsInvalid &&
                                !character.EsiSsoStatus)
                            {
                                _logger.Information($"Job {context.JobDetail.Key} user with username: {discordUser.Username}#{discordUser.DiscriminatorValue}" +
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

                        if (discordUserRoles is not null && discordGuildRoles is not null)
                        {
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
                                            _logger.Information($"Job {context.JobDetail.Key} adding role {discordGuildRole.Name} at user with username: {discordUser.Username}#{discordUser.DiscriminatorValue}");

                                            await discordUser.AddRoleAsync(discordGuildRole, RequestOptions.Default);
                                        }
                                    }
                                    else
                                    {
                                        if (!role.Value)
                                        {
                                            _logger.Information($"Job {context.JobDetail.Key} removing role {discordGuildRole.Name} at user with username: {discordUser.Username}#{discordUser.DiscriminatorValue}");

                                            await discordUser.RemoveRoleAsync(discordGuildRole, RequestOptions.Default);
                                        }
                                    }
                                }
                                else
                                {
                                    _logger.Error($"Job {context.JobDetail.Key} role with name: {role.Key} not found");
                                }
                            }
                        }
                    }
                    else
                    {
                        if (_settings.BotConfig.RemoveRolesIfTokenIsInvalid)
                        {
                            var overAllConfigRoles = new List<string>();

                            foreach (var authGroup in _settings.BotConfig.AuthGroups)
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
                                        _logger.Information($"Job {context.JobDetail.Key} try remove role \"{role.Name}\" at user: " +
                                                            $"{discordUser.Username}#{discordUser.DiscriminatorValue}");

                                        await discordUser.RemoveRoleAsync(role);

                                        _logger.Information($"Job {context.JobDetail.Key} remove role \"{role.Name}\" at user: " +
                                                            $"{discordUser.Username}#{discordUser.DiscriminatorValue} success");
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.Error(ex, $"Job {context.JobDetail.Key} unhandled exception");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                _logger.Error($"Job {context.JobDetail.Key} server guild with id: {_settings.DiscordConfig.ServerGuildId} not found");
            }

            _logger.Information($"Job {context.JobDetail.Key} finished");
        }
    }
}