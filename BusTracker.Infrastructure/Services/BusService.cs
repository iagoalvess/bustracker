using BusTracker.Core.DTOs;
using BusTracker.Core.Entities;
using BusTracker.Core.Interfaces;
using BusTracker.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace BusTracker.Infrastructure.Services
{
    /// <summary>
    /// Service implementation for bus-related operations.
    /// </summary>
    public class BusService : IBusService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDistanceCalculator _distanceCalculator;
        private readonly ILogger<BusService> _logger;

        public BusService(IUnitOfWork unitOfWork, IDistanceCalculator distanceCalculator, ILogger<BusService> logger)
        {
            _unitOfWork = unitOfWork;
            _distanceCalculator = distanceCalculator;
            _logger = logger;
        }

        /// <summary>
        /// Searches for bus stops matching the provided query.
        /// </summary>
        /// <param name="query">Search term (minimum 3 characters).</param>
        /// <returns>A collection of matching bus stops.</returns>
        public async Task<IEnumerable<BusStopResponseDto>> SearchStopsAsync(string query)
        {
            if (string.IsNullOrEmpty(query) || query.Length < 3)
            {
                return Enumerable.Empty<BusStopResponseDto>();
            }

            var queryLower = query.ToLower();
            var stops = await _unitOfWork.BusStops.FindAsync(
                s => s.Name.ToLower().Contains(queryLower) || s.Code == query);

            return stops.Take(20).Select(s => new BusStopResponseDto
            {
                Code = s.Code,
                Name = s.Name,
                Longitude = s.Location.X,
                Latitude = s.Location.Y
            });
        }

        /// <summary>
        /// Searches for bus lines matching the provided query.
        /// </summary>
        /// <param name="query">Search term for line number or name.</param>
        /// <returns>A collection of matching bus lines.</returns>
        public async Task<IEnumerable<BusLineResponseDto>> SearchLinesAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Enumerable.Empty<BusLineResponseDto>();
            }

            var queryLower = query.ToLower().Trim();
            var lines = await _unitOfWork.BusLines.FindAsync(
                x => x.DisplayNumber.ToLower().Contains(queryLower) || x.Name.ToLower().Contains(queryLower));

            return lines
                .OrderBy(x => x.DisplayNumber)
                .Take(30)
                .Select(x => new BusLineResponseDto
                {
                    Id = x.Id,
                    Number = x.DisplayNumber,
                    Description = x.Name
                });
        }

        /// <summary>
        /// Gets arrival prediction for a specific bus line at a stop.
        /// </summary>
        /// <param name="stopCode">The bus stop code.</param>
        /// <param name="lineNumber">The bus line number.</param>
        /// <returns>Prediction information with distance and estimated time.</returns>
        public async Task<BusPredictionResponseDto> GetPredictionAsync(string stopCode, string lineNumber)
        {
            var stop = await _unitOfWork.BusStops.FirstOrDefaultAsync(s => s.Code == stopCode);

            if (stop == null)
            {
                throw new KeyNotFoundException("Stop not found.");
            }

            var cleanLine = lineNumber.Trim().ToLower();
            var timeWindow = DateTime.UtcNow.AddMinutes(-5);

            var allPositions = await _unitOfWork.BusPositions.FindAsync(
                b => b.LineNumber.ToLower().Contains(cleanLine) && b.Timestamp >= timeWindow);

            var positionsList = allPositions.ToList();

            if (!positionsList.Any())
            {
                return new BusPredictionResponseDto
                {
                    Line = lineNumber,
                    Vehicle = string.Empty
                };
            }

            var vehicles = positionsList.GroupBy(b => b.VehicleNumber);
            var candidates = new List<(BusPosition Bus, double Distance)>();

            foreach (var vehicleGroup in vehicles)
            {
                var trajectory = vehicleGroup.OrderByDescending(x => x.Timestamp).Take(2).ToList();

                if (trajectory.Count >= 1)
                {
                    var current = trajectory[0];
                    var distance = _distanceCalculator.CalculateDistanceInMeters(
                        stop.Location.Y, stop.Location.X,
                        current.Location.Y, current.Location.X);

                    candidates.Add((current, distance));
                }
            }

            // Order all candidates by distance so we can get first and second closest.
            var ordered = candidates.OrderBy(x => x.Distance).ToList();

            if (!ordered.Any())
            {
                return new BusPredictionResponseDto
                {
                    Line = lineNumber,
                    Vehicle = string.Empty
                };
            }

            var bestBus = ordered[0];

            // Function local para calcular tempo/distancia para um candidato especifico.
            BusPredictionResponseDto MapCandidate((BusPosition Bus, double Distance) candidate)
            {
                double straightDistance = candidate.Distance;
                double tortuosityFactor = straightDistance < 500 ? 1.2 : 1.35;
                double estimatedRoadDistance = straightDistance * tortuosityFactor;
                double speedMetersPerMinute = 190;
                double timeInMinutes = estimatedRoadDistance / speedMetersPerMinute;
                timeInMinutes += (estimatedRoadDistance / 1000) * 0.6;

                return new BusPredictionResponseDto
                {
                    Line = candidate.Bus.LineNumber,
                    Vehicle = candidate.Bus.VehicleNumber,
                    DistanceInMeters = Math.Round(estimatedRoadDistance),
                    TimeInMinutes = Math.Round(timeInMinutes)
                };
            }

            var result = MapCandidate(bestBus);

            // Se existir um segundo mais proximo, calcula e devolve tambem.
            if (ordered.Count > 1)
            {
                var secondBus = ordered[1];
                result.SecondClosest = MapCandidate(secondBus);
            }

            return result;
        }
    }
}
