using Leviathan.Core.Models.Database;
using Microsoft.EntityFrameworkCore;

namespace Leviathan.Core.DatabaseContext
{
    public class MemoryContext : DbContext
    {
        public MemoryContext(DbContextOptions<MemoryContext> options) : base(options) { }
 
        public DbSet<Character> Characters { get; set; }
    }
}