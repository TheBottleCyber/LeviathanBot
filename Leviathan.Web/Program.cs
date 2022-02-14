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
        public static int Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                         .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                         .Enrich.FromLogContext()
                         .WriteTo.Console()
                         .CreateLogger();
            
            try
            {
                Log.Information("Starting web host");

                CreateHostBuilder(args).Build().Run();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                return 1;
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
        public Settings Settings { get; }
        public IWebHostEnvironment IWebHostEnvironment { get; }
        private string _dataSource;

        public Startup(IWebHostEnvironment environment)
        {
            IWebHostEnvironment = environment;
            Settings = LeviathanSettings.GetSettingsFile();
            _dataSource = LeviathanSettings.GetDatabaseFile(Settings);
            
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(Settings.BotConfig.Language);
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Settings.BotConfig.Language);
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(Settings);
            services.AddDbContext<SqliteContext>(opt => opt.UseSqlite(@$"DataSource={_dataSource};"));
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