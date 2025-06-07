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

                // Simple hardcoded validation for demo purposes
                if (request.Identifier == "cooper@preferredbuildersusa.com" && request.Password == "12345678")
                {
                    // Generate simple token - format: userId:email:role:timestamp
                    var token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"7:cooper@preferredbuildersusa.com:1510:{DateTime.UtcNow.Ticks}"));

                    _logger.LogInformation($"User logged in successfully");

                    return Ok(new
                    {
                        token = token,
                        user = new
                        {
                            id = 7,
                            email = "cooper@preferredbuildersusa.com",
                            firstName = "Anthony",
                            lastName = "Cooper",
                            role = 1510,
                            companyId = 1,
                            companyName = "Preferred Builders USA, LLC"
                        }
                    });
                }
                else
                {
                    _logger.LogWarning($"Login attempt failed: Invalid credentials for {request.Identifier}");
                    return Unauthorized(new { message = "Invalid credentials" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during login for identifier {request.Identifier}");
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }
    }


}