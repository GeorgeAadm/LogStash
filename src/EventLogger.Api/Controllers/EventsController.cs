using EventLogger.Api.Application.DTOs;
using EventLogger.Api.Application.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace EventLogger.Api.Controllers
{
    [ApiController]
    [Route("api/events")]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly ILogger<EventsController> _logger;

        public EventsController(IEventService eventService, ILogger<EventsController> logger)
        {
            _eventService = eventService;
            _logger = logger;
        }

        /// <summary>
        /// Record a new user event
        /// </summary>
        /// <param name="request">Event details</param>
        /// <returns>Created event information</returns>
        [HttpPost]
        [ProducesResponseType(typeof(CreateEventResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
        {
            try
            {
                _logger.LogInformation("Creating event for user {UserId}", request.UserId);
                
                var response = await _eventService.CreateEventAsync(request);
                
                return CreatedAtAction(
                    nameof(GetUserEvents), 
                    new { userId = response.UserId }, 
                    response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event");
                return StatusCode(500, new { error = "An error occurred while creating the event" });
            }
        }

        /// <summary>
        /// Get events for a specific user
        /// </summary>
        /// <param name="userId">User ID (email)</param>
        /// <param name="eventType">Optional: Filter by event type</param>
        /// <param name="fromDate">Optional: Filter events from this date</param>
        /// <param name="toDate">Optional: Filter events to this date</param>
        /// <param name="limit">Maximum number of events to return (default: 50, max: 1000)</param>
        /// <returns>List of user events</returns>
        [HttpGet("{userId}")]
        [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserEvents(
            [Required] string userId,
            [FromQuery] string? eventType = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] [Range(1, 1000)] int limit = 50)
        {
            try
            {
                _logger.LogInformation("Retrieving events for user {UserId}", userId);

                // Basic email validation
                if (!IsValidEmail(userId))
                {
                    return BadRequest(new { error = "Invalid userId format. Must be a valid email address." });
                }

                var query = new GetUserEventsQuery
                {
                    UserId = userId,
                    EventType = eventType,
                    FromDate = fromDate,
                    ToDate = toDate,
                    Limit = limit
                };

                var events = await _eventService.GetUserEventsAsync(query);

                if (!events.Any())
                {
                    return NotFound(new { message = $"No events found for user {userId}" });
                }

                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving events for user {UserId}", userId);
                return StatusCode(500, new { error = "An error occurred while retrieving events" });
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}