using System.Globalization;
using ESI.NET;
using Leviathan.Core.DatabaseContext;
using Leviathan.Core;
using Leviathan.Core.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Leviathan.Core.Models.Options;
using Leviathan.Core.Localization;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;

namespace Leviathan.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                         .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                         .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning) 
                         .Enrich.FromLogContext()
                         .WriteTo.Console()
                         .CreateLogger();
            
            try
            {
                Log.Information("Starting web host");

                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args).UseSerilog().ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
        }
    }

    public class Startup
    {
        private Settings _settings;

        public Startup(IWebHostEnvironment environment)
        {
            _settings = LeviathanSettings.GetSettingsFile();

            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(_settings.BotConfig.Language);
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(_settings.BotConfig.Language);
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(_settings);
            services.AddDbContext<SqliteContext>(opt => opt.UseSqlite(@$"DataSource={_settings.DatabaseConfig.ConnectionString};"));
            services.AddDbContext<MemoryContext>(opt => opt.UseInMemoryDatabase("users"));
            services.AddRazorPages();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
            });
        }
    }
}