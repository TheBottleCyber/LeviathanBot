using ESI.NET;
using Leviathan.Core.DatabaseContext;
using Leviathan.Core.Models.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Quartz;
using Serilog;

namespace Leviathan.Worker.Jobs
{
    [DisallowConcurrentExecution]
    public class StartupUpdateCharacters : IJob
    {
        private SqliteContext _sqliteContext;
        private ILogger _logger;
        private Settings _settings;

        public StartupUpdateCharacters(SqliteContext sqliteContext, ILogger logger, Settings settings)
        {
            _sqliteContext = sqliteContext;
            _logger = logger;
            _settings = settings;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.Information($"Job {context.JobDetail.Key} started");

            if (await _sqliteContext.Characters.AnyAsync())
            {
                var characterIdList = await _sqliteContext.Characters
                                                          .Where(x => x.EsiCharacterID > 0 && !string.IsNullOrEmpty(x.EsiTokenAccessToken))
                                                          .Select(x => x.EsiCharacterID).ToListAsync();

                _logger.Information($"Job {context.JobDetail.Key} job characters count: {characterIdList.Count}");

                //TODO: add chunking, possible max ids restriction according https://esi.evetech.net/ui/#/Character/post_characters_affiliation
                var affiliationEsiResponse = await new EsiClient(_settings.ESIConfig).Character.Affiliation(characterIdList.ToArray());
                if (affiliationEsiResponse.Data is not null)
                {
                    foreach (var affiliation in affiliationEsiResponse.Data)
                    {
                        var character = await _sqliteContext.Characters.FirstOrDefaultAsync(x => x.EsiCharacterID == affiliation.CharacterId);

                        if (character is not null)
                        {
                            character.EsiAllianceID = affiliation.AllianceId;
                            character.EsiCorporationID = affiliation.CorporationId;
                        }
                    }
                }

                await _sqliteContext.SaveChangesAsync();
            }

            _logger.Information($"Job {context.JobDetail.Key} finished");
        }
    }
}