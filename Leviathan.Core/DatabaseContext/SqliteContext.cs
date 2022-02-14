using System.ComponentModel.DataAnnotations.Schema;
using System.Configuration;
using Leviathan.Core.Extensions;
using Leviathan.Core.Models.Database;
using Microsoft.EntityFrameworkCore;

namespace Leviathan.Core.DatabaseContext
{
    public class SqliteContext : DbContext
    {
        public DbSet<Character> Characters { get; set; }
        public DbSet<Corporation> Corporations { get; set; }
        public DbSet<Alliance> Alliances { get; set; }
        
        public SqliteContext(DbContextOptions<SqliteContext> options) : base(options)
        {
            EnsureCreated();
        }

        public SqliteContext()
        {
            EnsureCreated();
        }

        public void EnsureCreated()
        {
            var config = LeviathanSettings.GetSettingsFile();
            var databaseFile = LeviathanSettings.GetDatabaseFile(config);
            
            // reducing the delay before database requests
            if (!File.Exists(databaseFile)) Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var config = LeviathanSettings.GetSettingsFile();
            optionsBuilder.UseSqlite(@$"DataSource={LeviathanSettings.GetDatabaseFile(config)};");
        }
    }
}