using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace TestRateLimiter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RateLimitTestController : ControllerBase
    {
        private readonly ILogger<RateLimitTestController> _logger;

        public RateLimitTestController(ILogger<RateLimitTestController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { message = "This endpoint uses the default rate limit (5 requests per 10 seconds)" });
        }

        [HttpGet("limited")]
        public IActionResult GetLimited()
        {
            return Ok(new { message = "This endpoint uses the WeatherForecast rate limit (2 requests per 5 seconds)" });
        }
    }
}
