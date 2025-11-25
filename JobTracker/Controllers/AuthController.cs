using Microsoft.AspNetCore.Mvc;

namespace JobTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;

        public AuthController(ILogger<AuthController> logger)
        {
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Identifier) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new { message = "Email/username and password are required" });
                }

                // SECURITY: Use WorkingAuthController instead - this is deprecated
                _logger.LogWarning($"AuthController is deprecated - use WorkingAuthController for secure JWT-based authentication");
                return Unauthorized(new { message = "Invalid credentials" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during login via AuthController for identifier {request.Identifier}");
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                // This endpoint would normally validate the JWT token and return user info
                // For now, return a simple response
                return Ok(new
                {
                    id = 7,
                    email = "cooper@preferredbuildersusa.com",
                    firstName = "Anthony",
                    lastName = "Cooper",
                    role = "Admin",
                    companyId = 1,
                    companyName = "Preferred Builders USA, LLC"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user via AuthController");
                return StatusCode(500, new { message = "An error occurred while getting user info" });
            }
        }
    }

    public class LoginRequest
    {
        public string Identifier { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}