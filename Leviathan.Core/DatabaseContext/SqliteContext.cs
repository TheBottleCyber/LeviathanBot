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
            Database.EnsureCreated();
        }
    }
}