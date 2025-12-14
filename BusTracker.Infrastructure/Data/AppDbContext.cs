using BusTracker.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusTracker.Infrastructure.Data
{
    /// <summary>
    /// Database context for the BusTracker application.
    /// </summary>
    public class AppDbContext : DbContext
    {
        public DbSet<BusStop> BusStops { get; set; }
        public DbSet<BusPosition> BusPositions { get; set; }
        public DbSet<BusLine> BusLines { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BusStop>()
                .HasIndex(b => b.Location)
                .HasMethod("gist");

            modelBuilder.Entity<BusPosition>()
                .HasIndex(b => b.LineNumber);
        }
    }
}