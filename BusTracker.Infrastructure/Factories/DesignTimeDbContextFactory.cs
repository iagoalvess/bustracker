using BusTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BusTracker.Infrastructure.Factories
{
    /// <summary>
    /// Factory for creating DbContext at design time (for EF Core migrations).
    /// </summary>
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        /// <summary>
        /// Creates a new instance of the AppDbContext for design time operations.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>A configured AppDbContext instance.</returns>
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? "Host=localhost;Port=5432;Database=bustrackerdb;Username=postgres;Password=123";
            
            optionsBuilder.UseNpgsql(connectionString, o => o.UseNetTopologySuite());
            
            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
