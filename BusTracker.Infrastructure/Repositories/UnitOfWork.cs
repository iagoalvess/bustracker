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

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitOfWork"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public IRepository<BusStop> BusStops
        {
            get { return _busStops ??= new Repository<BusStop>(_context); }
        }

        /// <inheritdoc />
        public IRepository<BusPosition> BusPositions
        {
            get { return _busPositions ??= new Repository<BusPosition>(_context); }
        }

        /// <inheritdoc />
        public IRepository<BusLine> BusLines
        {
            get { return _busLines ??= new Repository<BusLine>(_context); }
        }

        /// <inheritdoc />
        public IRepository<BusLineStop> BusLineStops
        {
            get { return _busLineStops ??= new Repository<BusLineStop>(_context); }
        }

        /// <inheritdoc />
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task<int> ExecuteDeleteAsync<T>(IQueryable<T> query) where T : class
        {
            return await query.ExecuteDeleteAsync();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
