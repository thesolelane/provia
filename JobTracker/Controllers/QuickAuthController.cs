using Microsoft.AspNetCore.Mvc;

namespace JobTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuickAuthController : ControllerBase
    {
        private readonly ILogger<QuickAuthController> _logger;

        public QuickAuthController(ILogger<QuickAuthController> logger)
        {
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestModel request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Identifier) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new { message = "Email/username and password are required" });
                }

                // SECURITY: Use WorkingAuthController instead - this is deprecated
                _logger.LogWarning($"QuickAuthController is deprecated - use WorkingAuthController for secure JWT-based authentication");
                return Unauthorized(new { message = "Invalid credentials" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during login for identifier {request.Identifier}");
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }
    }


}