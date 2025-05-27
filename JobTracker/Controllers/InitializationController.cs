using Microsoft.AspNetCore.Mvc;
using JobTracker.Services;

namespace JobTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InitializationController : ControllerBase
    {
        private readonly MaterialStoreService _materialStoreService;
        private readonly ILogger<InitializationController> _logger;

        public InitializationController(MaterialStoreService materialStoreService, ILogger<InitializationController> logger)
        {
            _materialStoreService = materialStoreService;
            _logger = logger;
        }

        [HttpPost("seed-material-stores")]
        public async Task<IActionResult> SeedMaterialStores()
        {
            try
            {
                await _materialStoreService.SeedMaterialStoresAsync();
                return Ok(new { message = "Material stores seeded successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding material stores");
                return StatusCode(500, new { message = "Failed to seed material stores" });
            }
        }
    }
}