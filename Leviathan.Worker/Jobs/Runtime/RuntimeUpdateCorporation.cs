using ESI.NET;
using Leviathan.Core.DatabaseContext;
using Leviathan.Core.Models.Options;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Serilog;

namespace Leviathan.Jobs.Runtime
{
    [DisallowConcurrentExecution]
    public class RuntimeUpdateCorporation : IJob
    {
        private SqliteContext _sqliteContext;
        private ILogger _logger;
        private Settings _settings;

        public RuntimeUpdateCorporation(SqliteContext sqliteContext, ILogger logger, Settings settings)
        {
            _sqliteContext = sqliteContext;
            _logger = logger;
            _settings = settings;
        }
        
        public async Task Execute(IJobExecutionContext context)
        {
            var corporationId = context.JobDetail.JobDataMap.GetInt("corporation_id");
            
            _logger.Information($"Job {context.JobDetail.Key} started");
            
            var corporation = await _sqliteContext.Corporations.FirstOrDefaultAsync(x => x.CorporationId == corporationId);

            if (corporation is not null)
            {
                var corporationResponse = await new EsiClient(_settings.ESIConfig).Corporation.Information(corporation.CorporationId);

                corporation.Name = corporationResponse.Data.Name;
                corporation.Ticker = corporationResponse.Data.Ticker;
                corporation.AllianceId = corporationResponse.Data.AllianceId;
                corporation.ExpiresOn = DateTime.UtcNow.AddHours(1);

                await _sqliteContext.SaveChangesAsync();

                _logger.Information($"Job {context.JobDetail.Key} ticker: {corporation.Ticker} finished");
            }
        }
    }
}