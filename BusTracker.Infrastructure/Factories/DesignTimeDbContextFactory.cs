using BusTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BusTracker.Infrastructure.Factories
{
    /// <summary>
    /// Factory for creating DbContext at design time (for migrations).
    /// </summary>
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? "Host=localhost;Port=5433;Database=bustrackerdb;Username=postgres;Password=postgres";
            
            optionsBuilder.UseNpgsql(connectionString, o => o.UseNetTopologySuite());
            
            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
