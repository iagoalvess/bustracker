using BusTracker.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusTracker.Infrastructure.Data
{
    /// <summary>
    /// Database context for the BusTracker application.
    /// </summary>
    public class AppDbContext : DbContext
    {
        /// <summary>
        /// Gets or sets the bus stops DbSet.
        /// </summary>
        public DbSet<BusStop> BusStops { get; set; }
        
        /// <summary>
        /// Gets or sets the bus positions DbSet.
        /// </summary>
        public DbSet<BusPosition> BusPositions { get; set; }
        
        /// <summary>
        /// Gets or sets the bus lines DbSet.
        /// </summary>
        public DbSet<BusLine> BusLines { get; set; }
        
        /// <summary>
        /// Gets or sets the bus line stops (relationships) DbSet.
        /// </summary>
        public DbSet<BusLineStop> BusLineStops { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AppDbContext"/> class.
        /// </summary>
        /// <param name="options">The database context options.</param>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// Configures the entity model and relationships.
        /// </summary>
        /// <param name="modelBuilder">The model builder instance.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BusStop>()
                .HasIndex(b => b.Location)
                .HasMethod("gist");

            modelBuilder.Entity<BusPosition>()
                .HasIndex(b => b.LineNumber);

            // Index for efficient cleanup queries filtering by Timestamp
            modelBuilder.Entity<BusPosition>()
                .HasIndex(b => b.Timestamp);

            modelBuilder.Entity<BusLineStop>()
                .HasOne(bls => bls.BusLine)
                .WithMany()
                .HasForeignKey(bls => bls.BusLineId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BusLineStop>()
                .HasOne(bls => bls.BusStop)
                .WithMany()
                .HasForeignKey(bls => bls.BusStopId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BusLineStop>()
                .HasIndex(bls => new { bls.BusLineId, bls.BusStopId });

            modelBuilder.Entity<BusLineStop>()
                .HasIndex(bls => bls.BusStopId);
        }
    }
}