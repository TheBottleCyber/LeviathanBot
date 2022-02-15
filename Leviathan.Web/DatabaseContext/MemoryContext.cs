using Leviathan.Core.Models.Database;
using Microsoft.EntityFrameworkCore;

namespace Leviathan.Web.DatabaseContext
{
    public class MemoryContext : DbContext
    {
        public DbSet<Character> Characters { get; set; }
        
        public MemoryContext(DbContextOptions<MemoryContext> options) : base(options) { }
    }
}