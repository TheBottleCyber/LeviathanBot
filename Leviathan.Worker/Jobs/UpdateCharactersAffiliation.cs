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
    public class UpdateCharactersAffiliation : IJob
    {
        private Settings _settings;

        public UpdateCharactersAffiliation(Settings settings)
        {
            _settings = settings;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            using var log = new LoggerConfiguration().WriteTo.Console().CreateLogger();

            log.Information("Job update_character_affiliation started");

            await using (var sqliteContext = new SqliteContext())
            {
                if (await sqliteContext.Characters.AnyAsync())
                {
                    var characterIdList = await sqliteContext.Characters
                                                             .Where(x => x.EsiCharacterID > 0 && !string.IsNullOrEmpty(x.EsiTokenAccessToken))
                                                             .Select(x => x.EsiCharacterID).ToListAsync();

                    log.Information($"Job update_character_affiliation job characters count: {characterIdList.Count}");

                    //TODO: add chunking, possible max ids restriction according https://esi.evetech.net/ui/#/Character/post_characters_affiliation
                    var affiliationEsiResponse = await new EsiClient(Options.Create(_settings.ESIConfig)).Character.Affiliation(characterIdList.ToArray());
                    if (affiliationEsiResponse.Data is not null)
                    {
                        foreach (var affiliation in affiliationEsiResponse.Data)
                        {
                            var character = await sqliteContext.Characters.FirstOrDefaultAsync(x => x.EsiCharacterID == affiliation.CharacterId);

                            if (character is not null)
                            {
                                character.EsiAllianceID = affiliation.AllianceId;
                                character.EsiCorporationID = affiliation.CorporationId;
                            }
                        }
                    }

                    await sqliteContext.SaveChangesAsync();
                }
            }

            log.Information("Job update_character_affiliation finished");
        }
    }
}