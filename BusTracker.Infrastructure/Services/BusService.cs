using BusTracker.Core.DTOs;
using BusTracker.Core.Entities;
using BusTracker.Core.Interfaces;
using BusTracker.Core.Interfaces.Services;
using BusTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusTracker.Infrastructure.Services
{
    /// <summary>
    /// Service implementation for bus-related operations.
    /// </summary>
    public class BusService : IBusService
    {
        private const int MinQueryLength = 3;
        private const int MaxStopResults = 20;
        private const int MaxLineResults = 30;
        private const int PositionTimeWindowMinutes = 5;
        private const double StopProximityThresholdMeters = 150;
        private const double MovingAwayMultiplier = 1.5;
        private const double MovingAwayMinDeltaMeters = 100;
        private const int MinPositionsForTrajectoryAnalysis = 3;
        private const int MinPositionsAfterMinForAnalysis = 2;
        private const double DistanceToleranceMeters = 20;
        private const double PassedStopDeltaMeters = 200;
        private const double CloseProximityMeters = 100;
        private const double StationaryToleranceMeters = 30;
        private const double SinglePositionMaxDistanceMeters = 500;
        private const double ShortDistanceThresholdMeters = 500;
        private const double ShortDistanceTortuosityFactor = 1.2;
        private const double LongDistanceTortuosityFactor = 1.35;
        private const double AverageSpeedMetersPerMinute = 190;
        private const double TrafficDelayPerKilometer = 0.6;
        private const int MetersPerKilometer = 1000;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IDistanceCalculator _distanceCalculator;
        private readonly ILogger<BusService> _logger;
        private readonly AppDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="BusService"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work for data access.</param>
        /// <param name="distanceCalculator">The distance calculator service.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="context">The database context.</param>
        public BusService(IUnitOfWork unitOfWork, IDistanceCalculator distanceCalculator, ILogger<BusService> logger, AppDbContext context)
        {
            _unitOfWork = unitOfWork;
            _distanceCalculator = distanceCalculator;
            _logger = logger;
            _context = context;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<BusStopResponseDto>> SearchStopsAsync(string query)
        {
            if (string.IsNullOrEmpty(query) || query.Length < MinQueryLength)
            {
                return Enumerable.Empty<BusStopResponseDto>();
            }

            var queryLower = query.ToLower();
            var stops = await _unitOfWork.BusStops.FindAsync(
                s => s.Name.ToLower().Contains(queryLower) || s.Code == query);

            return stops.Take(MaxStopResults).Select(s => new BusStopResponseDto
            {
                Code = s.Code,
                Name = s.Name,
                Longitude = s.Location.X,
                Latitude = s.Location.Y
            });
        }

        /// <inheritdoc />
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
                .Take(MaxLineResults)
                .Select(x => new BusLineResponseDto
                {
                    Id = x.Id,
                    Number = x.DisplayNumber,
                    Description = x.Name
                });
        }

        /// <inheritdoc />
        public async Task<BusPredictionResponseDto> GetPredictionAsync(string stopCode, string lineNumber)
        {
            var stop = await _unitOfWork.BusStops.FirstOrDefaultAsync(s => s.Code == stopCode);

            if (stop == null)
            {
                throw new KeyNotFoundException("Stop not found.");
            }

            var cleanLine = lineNumber.Trim().ToLower();
            
            var lineStopsExist = await _context.BusLineStops
                .Include(bls => bls.BusLine)
                .AnyAsync(bls => bls.BusStopId == stop.Id && 
                                 bls.BusLine.DisplayNumber.ToLower().Contains(cleanLine));
            
            if (!lineStopsExist)
            {
                throw new InvalidOperationException($"Line {lineNumber} does not serve stop {stopCode}.");
            }

            var timeWindow = DateTime.UtcNow.AddMinutes(-PositionTimeWindowMinutes);

            var allPositions = await _unitOfWork.BusPositions.FindAsync(
                b => b.LineNumber.ToLower().Contains(cleanLine) && b.Timestamp >= timeWindow);

            var positionsList = allPositions.ToList();

            if (positionsList.Count == 0)
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
                var allVehiclePositions = vehicleGroup.OrderBy(x => x.Timestamp).ToList();
                
                if (allVehiclePositions.Count == 0)
                    continue;

                var distancesOverTime = allVehiclePositions.Select(pos => new
                {
                    Position = pos,
                    Distance = _distanceCalculator.CalculateDistanceInMeters(
                        stop.Location.Y, stop.Location.X,
                        pos.Location.Y, pos.Location.X)
                }).ToList();

                var currentDistance = distancesOverTime.Last().Distance;
                var minHistoricalDistance = distancesOverTime.Min(x => x.Distance);
                var minDistanceIndex = distancesOverTime.FindIndex(x => x.Distance == minHistoricalDistance);

                bool hasPassedStop = false;

                if (minHistoricalDistance < StopProximityThresholdMeters && 
                    currentDistance > minHistoricalDistance * MovingAwayMultiplier && 
                    currentDistance > minHistoricalDistance + MovingAwayMinDeltaMeters)
                {
                    hasPassedStop = true;
                }
                else if (distancesOverTime.Count >= MinPositionsForTrajectoryAnalysis)
                {
                    var positionsAfterMin = distancesOverTime.Skip(minDistanceIndex).ToList();
                    
                    if (positionsAfterMin.Count >= MinPositionsAfterMinForAnalysis)
                    {
                        bool isMovingAway = true;
                        for (int i = 1; i < positionsAfterMin.Count; i++)
                        {
                            if (positionsAfterMin[i].Distance < positionsAfterMin[i - 1].Distance - DistanceToleranceMeters)
                            {
                                isMovingAway = false;
                                break;
                            }
                        }

                        if (isMovingAway && currentDistance > minHistoricalDistance + PassedStopDeltaMeters)
                        {
                            hasPassedStop = true;
                        }
                    }
                }

                if (hasPassedStop)
                {
                    continue;
                }

                if (distancesOverTime.Count >= 2)
                {
                    var previousDistance = distancesOverTime[distancesOverTime.Count - 2].Distance;

                    if (currentDistance < previousDistance || 
                        (currentDistance < CloseProximityMeters && Math.Abs(currentDistance - previousDistance) < StationaryToleranceMeters))
                    {
                        candidates.Add((distancesOverTime.Last().Position, currentDistance));
                    }
                }
                else
                {
                    if (currentDistance < SinglePositionMaxDistanceMeters)
                    {
                        candidates.Add((distancesOverTime.Last().Position, currentDistance));
                    }
                }
            }

            var ordered = candidates.OrderBy(x => x.Distance).ToList();

            if (ordered.Count == 0)
            {
                return new BusPredictionResponseDto
                {
                    Line = lineNumber,
                    Vehicle = string.Empty
                };
            }

            var bestBus = ordered[0];
            var result = MapCandidate(bestBus);

            if (ordered.Count > 1)
            {
                var secondBus = ordered[1];
                result.SecondClosest = MapCandidate(secondBus);
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<LineAtStopResponseDto>> GetLinesAtStopAsync(string stopCode)
        {
            var stop = await _unitOfWork.BusStops.FirstOrDefaultAsync(s => s.Code == stopCode);
            
            if (stop == null)
            {
                throw new KeyNotFoundException("Stop not found.");
            }

            var lineStops = await _context.BusLineStops
                .Include(bls => bls.BusLine)
                .Where(bls => bls.BusStopId == stop.Id)
                .ToListAsync();
            
            return lineStops
                .GroupBy(bls => new { bls.BusLine.DisplayNumber, bls.BusLine.Name })
                .Select(g => new LineAtStopResponseDto
                {
                    LineNumber = g.Key.DisplayNumber,
                    LineName = g.Key.Name
                })
                .OrderBy(l => l.LineNumber);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<StopOnLineResponseDto>> GetStopsOnLineAsync(string lineNumber)
        {
            var cleanLine = lineNumber.Trim().ToLower();
            
            var lineStops = await _context.BusLineStops
                .Include(bls => bls.BusLine)
                .Include(bls => bls.BusStop)
                .Where(bls => bls.BusLine.DisplayNumber.ToLower().Contains(cleanLine))
                .ToListAsync();
            
            if (!lineStops.Any())
            {
                throw new KeyNotFoundException($"Line {lineNumber} not found or has no stops.");
            }

            return lineStops
                .OrderBy(bls => bls.Sequence)
                .Select(bls => new StopOnLineResponseDto
                {
                    StopCode = bls.BusStop.Code,
                    StopName = bls.BusStop.Name,
                    Sequence = bls.Sequence,
                    Longitude = bls.BusStop.Location.X,
                    Latitude = bls.BusStop.Location.Y
                });
        }

        private static BusPredictionResponseDto MapCandidate((BusPosition Bus, double Distance) candidate)
        {
            double straightDistance = candidate.Distance;
            double tortuosityFactor = straightDistance < ShortDistanceThresholdMeters 
                ? ShortDistanceTortuosityFactor 
                : LongDistanceTortuosityFactor;
            double estimatedRoadDistance = straightDistance * tortuosityFactor;
            double timeInMinutes = estimatedRoadDistance / AverageSpeedMetersPerMinute;
            timeInMinutes += (estimatedRoadDistance / MetersPerKilometer) * TrafficDelayPerKilometer;

            return new BusPredictionResponseDto
            {
                Line = candidate.Bus.LineNumber,
                Vehicle = candidate.Bus.VehicleNumber,
                DistanceInMeters = Math.Round(estimatedRoadDistance),
                TimeInMinutes = Math.Round(timeInMinutes)
            };
        }
    }
}
