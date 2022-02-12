using System.Net;
using ESI.NET;
using Leviathan.Core.DatabaseContext;
using Leviathan.Core.Models.Database;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Serilog;

namespace Leviathan.Jobs
{
    public class UpdateCorporations : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            using var log = new LoggerConfiguration().WriteTo.Console().CreateLogger();

            log.Information($"Job update_corporations started");
            
            await using (var sqliteContext = new SqliteContext())
            {
                if (await sqliteContext.Corporations.AnyAsync())
                {
                    var esiClient = new EsiClient(Program.EsiConfigOptions);

                    log.Information($"Job update_corporations job corporations count: {await sqliteContext.Corporations.CountAsync()}");
                    
                    foreach (var corporation in sqliteContext.Corporations)
                    {
                        log.Information($"Job update_corporations updating corpotation with name: {corporation.Name} ticker: {corporation.Ticker} id: {corporation.CorporationId}");
                        var corporationResponse = await esiClient.Corporation.Information(corporation.CorporationId);
                        
                        if (corporationResponse.StatusCode == HttpStatusCode.OK)
                        {
                            corporation.Name = corporationResponse.Data.Name;
                            corporation.Ticker = corporationResponse.Data.Ticker;
                            corporation.AllianceId = corporationResponse.Data.AllianceId;
                        }
                        
                        log.Information($"Job update_corporations updating corpotation with name: {corporation.Name} ticker: {corporation.Ticker} id: {corporation.CorporationId} finished");
                    }

                    await sqliteContext.SaveChangesAsync();
                }
            }
            
            log.Information($"Job update_corporations finished");
        }
    }
}