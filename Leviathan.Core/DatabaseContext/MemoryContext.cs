using Leviathan.Core.Models.Database;
using Microsoft.EntityFrameworkCore;

namespace Leviathan.Core.DatabaseContext
{
    public class MemoryContext : DbContext
    {
        public DbSet<Character> Characters { get; set; }
        
        public MemoryContext(DbContextOptions<MemoryContext> options) : base(options) { }
    }
}