using BusTracker.Core.Entities;
using BusTracker.Core.Interfaces;
using BusTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BusTracker.Infrastructure.Repositories
{
    /// <summary>
    /// Unit of Work implementation for coordinating repository operations and transactions.
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IRepository<BusStop>? _busStops;
        private IRepository<BusPosition>? _busPositions;
        private IRepository<BusLine>? _busLines;
        private IRepository<BusLineStop>? _busLineStops;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets the repository for BusStop entities.
        /// </summary>
        public IRepository<BusStop> BusStops
        {
            get { return _busStops ??= new Repository<BusStop>(_context); }
        }

        /// <summary>
        /// Gets the repository for BusPosition entities.
        /// </summary>
        public IRepository<BusPosition> BusPositions
        {
            get { return _busPositions ??= new Repository<BusPosition>(_context); }
        }

        /// <summary>
        /// Gets the repository for BusLine entities.
        /// </summary>
        public IRepository<BusLine> BusLines
        {
            get { return _busLines ??= new Repository<BusLine>(_context); }
        }

        /// <summary>
        /// Gets the repository for BusLineStop entities.
        /// </summary>
        public IRepository<BusLineStop> BusLineStops
        {
            get { return _busLineStops ??= new Repository<BusLineStop>(_context); }
        }

        /// <summary>
        /// Saves all changes made in this unit of work to the database.
        /// </summary>
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Executes a delete operation for the given query.
        /// </summary>
        public async Task<int> ExecuteDeleteAsync<T>(IQueryable<T> query) where T : class
        {
            return await query.ExecuteDeleteAsync();
        }

        /// <summary>
        /// Disposes the unit of work, releasing the underlying context.
        /// </summary>
        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
