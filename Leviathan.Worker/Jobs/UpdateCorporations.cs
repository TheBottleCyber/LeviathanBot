using ESI.NET;
using Leviathan.Core.DatabaseContext;
using Leviathan.Core.Models.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Quartz;
using Serilog;

namespace Leviathan.Jobs
{
    public class UpdateCorporations : IJob
    {
        private SqliteContext _sqliteContext;
        private ILogger _logger;
        private Settings _settings;

        public UpdateCorporations(SqliteContext sqliteContext, ILogger logger, Settings settings)
        {
            _sqliteContext = sqliteContext;
            _logger = logger;
            _settings = settings;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.Information($"Job update_corporations started");

            if (await _sqliteContext.Corporations.AnyAsync())
            {
                var esiClient = new EsiClient(_settings.ESIConfig);

                _logger.Information($"Job update_corporations corporations count: {await _sqliteContext.Corporations.CountAsync()}");

                foreach (var corporation in _sqliteContext.Corporations)
                {
                    _logger.Information($"Job update_corporations updating corpotation with name: {corporation.Name} ticker: {corporation.Ticker} id: {corporation.CorporationId}");
                    var corporationResponse = await esiClient.Corporation.Information(corporation.CorporationId);

                    corporation.Name = corporationResponse.Data.Name;
                    corporation.Ticker = corporationResponse.Data.Ticker;
                    corporation.AllianceId = corporationResponse.Data.AllianceId;

                    _logger.Information($"Job update_corporations updating corpotation with name: {corporation.Name} ticker: {corporation.Ticker} id: {corporation.CorporationId} finished");
                }

                await _sqliteContext.SaveChangesAsync();
            }

            _logger.Information($"Job update_corporations finished");
        }
    }
}