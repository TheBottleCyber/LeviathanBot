using System.ComponentModel.DataAnnotations.Schema;
using System.Configuration;
using Leviathan.Core.Extensions;
using Leviathan.Core.Models.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Leviathan.Core.DatabaseContext
{
    public class SqliteContextFactory : IDesignTimeDbContextFactory<SqliteContext>
    {
        public SqliteContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SqliteContext>();
            var config = LeviathanSettings.GetSettingsFile();
            optionsBuilder.UseSqlite(@$"DataSource={LeviathanSettings.GetDatabaseFile(config)};");

            return new SqliteContext(optionsBuilder.Options);
        }
    }
    
    public class SqliteContext : DbContext
    {
        public DbSet<Character> Characters { get; set; }
        public DbSet<Corporation> Corporations { get; set; }
        public DbSet<Alliance> Alliances { get; set; }
        
        public SqliteContext(DbContextOptions<SqliteContext> options) : base(options)
        {
            Database.Migrate();
        }
    }
}