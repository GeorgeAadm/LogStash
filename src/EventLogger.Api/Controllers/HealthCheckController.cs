using Microsoft.AspNetCore.Mvc;

namespace EventLogger.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthCheckController : ControllerBase
    {
        /// <summary>
        /// Simple health check endpoint
        /// </summary>
        /// <returns>Health status</returns>
        [HttpGet("/api/healthcheck")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public IActionResult GetHealth()
        {
            return Ok(new 
            { 
                status = "healthy", 
                timestamp = DateTime.UtcNow,
                service = "EventLogger.Api"
            });
        }
    }
}