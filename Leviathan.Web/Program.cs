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

namespace Leviathan.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
        }
    }

    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IWebHostEnvironment IWebHostEnvironment { get; }

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            IWebHostEnvironment = environment;
            Configuration = LeviathanSettings.GetSettingsFile();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<DiscordConfigOptions>(Configuration.GetSection("DiscordConfig"));
            services.Configure<EsiConfig>(Configuration.GetSection("ESIConfig"));
            services.AddDbContext<SqliteContext>(opt => opt.UseSqlite(@$"DataSource={LeviathanSettings.GetDatabaseFile(Configuration)};"));
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