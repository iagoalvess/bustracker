using BusTracker.API.Models;
using BusTracker.Core.DTOs;
using BusTracker.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace BusTracker.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class BusController : ControllerBase
    {
        private readonly IBusService _busService;
        private readonly ILogger<BusController> _logger;

        public BusController(IBusService busService, ILogger<BusController> logger)
        {
            _busService = busService;
            _logger = logger;
        }

        /// <summary>
        /// Searches for bus stops by name or code.
        /// </summary>
        /// <param name="query">Search term (minimum 3 characters).</param>
        /// <returns>A list of matching bus stops.</returns>
        /// <response code="200">Returns the list of matching bus stops.</response>
        /// <response code="400">If the query is invalid.</response>
        /// <remarks>
        /// Sample request:
        /// 
        ///     GET /api/bus/stops?query=central
        /// 
        /// </remarks>
        [HttpGet("stops")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("search")]
        [ProducesResponseType(typeof(IEnumerable<BusStopResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchStops([FromQuery] string query)
        {
            if (string.IsNullOrEmpty(query) || query.Length < 3)
                return BadRequest(new ErrorResponse { Error = "Please enter at least 3 characters." });

            var stops = await _busService.SearchStopsAsync(query);
            return Ok(stops);
        }

        /// <summary>
        /// Gets bus arrival prediction for a specific stop and line.
        /// </summary>
        /// <param name="stopCode">The bus stop code.</param>
        /// <param name="lineNum">The bus line number.</param>
        /// <returns>Prediction information including distance and estimated time.</returns>
        /// <response code="200">Returns the prediction information.</response>
        /// <response code="400">If stop code or line number is missing.</response>
        /// <response code="404">If the stop is not found.</response>
        /// <remarks>
        /// Sample request:
        /// 
        ///     GET /api/bus/prediction?stopCode=12345&amp;lineNum=5101
        /// 
        /// </remarks>
        [HttpGet("prediction")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("prediction")]
        [ProducesResponseType(typeof(BusPredictionResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPrediction([FromQuery] string stopCode, [FromQuery] string lineNum)
        {
            if (string.IsNullOrWhiteSpace(stopCode) || string.IsNullOrWhiteSpace(lineNum))
                return BadRequest(new ErrorResponse { Error = "Please provide both stop code and bus line number." });

            try
            {
                var prediction = await _busService.GetPredictionAsync(stopCode, lineNum);
                return Ok(prediction);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ErrorResponse { Error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ErrorResponse { Error = ex.Message });
            }
        }

        /// <summary>
        /// Searches for bus lines by number or name.
        /// </summary>
        /// <param name="query">Search term for line number or name.</param>
        /// <returns>A list of matching bus lines.</returns>
        /// <response code="200">Returns the list of matching bus lines.</response>
        /// <response code="400">If the query is empty.</response>
        /// <remarks>
        /// Sample request:
        /// 
        ///     GET /api/bus/lines?query=5101
        /// 
        /// </remarks>
        [HttpGet("lines")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("search")]
        [ProducesResponseType(typeof(IEnumerable<BusLineResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchLines([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(new ErrorResponse { Error = "Please enter the line number or name." });

            var lines = await _busService.SearchLinesAsync(query);
            return Ok(lines);
        }

        /// <summary>
        /// Gets all bus lines that serve a specific stop.
        /// </summary>
        /// <param name="stopCode">The bus stop code.</param>
        /// <returns>A list of lines serving this stop.</returns>
        /// <response code="200">Returns the list of lines at the stop.</response>
        /// <response code="404">If the stop is not found.</response>
        /// <remarks>
        /// Sample request:
        /// 
        ///     GET /api/bus/stop/12345/lines
        /// 
        /// </remarks>
        [HttpGet("stop/{stopCode}/lines")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("search")]
        [ProducesResponseType(typeof(IEnumerable<LineAtStopResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLinesAtStop(string stopCode)
        {
            try
            {
                var lines = await _busService.GetLinesAtStopAsync(stopCode);
                return Ok(lines);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ErrorResponse { Error = ex.Message });
            }
        }

        /// <summary>
        /// Gets all stops served by a specific bus line.
        /// </summary>
        /// <param name="lineNumber">The bus line number.</param>
        /// <returns>A list of stops on this line, ordered by sequence.</returns>
        /// <response code="200">Returns the list of stops on the line.</response>
        /// <response code="404">If the line is not found.</response>
        /// <remarks>
        /// Sample request:
        /// 
        ///     GET /api/bus/line/5101/stops
        /// 
        /// </remarks>
        [HttpGet("line/{lineNumber}/stops")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("search")]
        [ProducesResponseType(typeof(IEnumerable<StopOnLineResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetStopsOnLine(string lineNumber)
        {
            try
            {
                var stops = await _busService.GetStopsOnLineAsync(lineNumber);
                return Ok(stops);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ErrorResponse { Error = ex.Message });
            }
        }
    }
}