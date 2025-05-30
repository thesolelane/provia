using Microsoft.AspNetCore.Mvc;

namespace JobTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InitializationController : ControllerBase
    {
        private readonly ILogger<InitializationController> _logger;

        public InitializationController(ILogger<InitializationController> logger)
        {
            _logger = logger;
        }

        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return Ok(new { message = "Initialization controller is ready" });
        }
    }
}