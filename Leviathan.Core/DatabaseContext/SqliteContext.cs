using System.ComponentModel.DataAnnotations.Schema;
using System.Configuration;
using Leviathan.Core.Extensions;
using Leviathan.Core.Models.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
#pragma warning disable CS8618

namespace Leviathan.Core.DatabaseContext
{
    [Obsolete("do not use, only for migrations")]
    public class SqliteContextFactory : IDesignTimeDbContextFactory<SqliteContext>
    {
        public SqliteContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SqliteContext>();
            optionsBuilder.UseSqlite(@$"DataSource=localMigration.db;");

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