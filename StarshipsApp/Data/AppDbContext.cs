using Microsoft.EntityFrameworkCore;
using StarshipsApp.Models;

namespace StarshipsApp.Data
{
    // Database context for the application
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Starship> Starships => Set<Starship>();
    }
}