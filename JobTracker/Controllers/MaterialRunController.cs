using Microsoft.AspNetCore.Mvc;
using JobTracker.Services;
using JobTracker.Data;
using Microsoft.EntityFrameworkCore;
using JobTracker.Models;

namespace JobTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MaterialRunController : ControllerBase
    {
        private readonly MaterialRunService _materialRunService;
        private readonly MaterialStoreService _materialStoreService;
        private readonly JobTrackerContext _context;
        private readonly ILogger<MaterialRunController> _logger;

        public MaterialRunController(
            MaterialRunService materialRunService,
            MaterialStoreService materialStoreService,
            JobTrackerContext context,
            ILogger<MaterialRunController> logger)
        {
            _materialRunService = materialRunService;
            _materialStoreService = materialStoreService;
            _context = context;
            _logger = logger;
        }

        [HttpGet("stores")]
        public async Task<IActionResult> GetMaterialStores([FromQuery] string? storeType = null)
        {
            try
            {
                List<MaterialStore> stores;
                
                if (!string.IsNullOrEmpty(storeType))
                {
                    stores = await _materialStoreService.GetStoresByTypeAsync(storeType);
                }
                else
                {
                    stores = await _context.MaterialStores
                        .Where(s => s.IsActive)
                        .OrderBy(s => s.DistanceFromOffice)
                        .ToListAsync();
                }

                return Ok(stores.Select(s => new
                {
                    s.Id,
                    s.StoreName,
                    s.StoreType,
                    s.Address,
                    s.City,
                    s.State,
                    s.PhoneNumber,
                    s.DistanceFromOffice,
                    s.Hours
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting material stores");
                return StatusCode(500, new { message = "Failed to get material stores" });
            }
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartMaterialRun([FromBody] StartMaterialRunRequest request)
        {
            try
            {
                var result = await _materialRunService.StartMaterialRun(
                    request.UserId, 
                    request.JobId, 
                    request.MaterialStoreId, 
                    request.Purpose,
                    request.Latitude, 
                    request.Longitude);

                if (result.Success)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting material run");
                return StatusCode(500, new { message = "Failed to start material run" });
            }
        }

        [HttpPost("arrive-at-store")]
        public async Task<IActionResult> ArriveAtStore([FromBody] MaterialRunLocationRequest request)
        {
            try
            {
                var result = await _materialRunService.ArriveAtStore(
                    request.MaterialRunId, 
                    request.Latitude, 
                    request.Longitude);

                if (result.Success)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording store arrival");
                return StatusCode(500, new { message = "Failed to record store arrival" });
            }
        }

        [HttpPost("depart-from-store")]
        public async Task<IActionResult> DepartFromStore([FromBody] MaterialRunLocationRequest request)
        {
            try
            {
                var result = await _materialRunService.DepartFromStore(
                    request.MaterialRunId, 
                    request.Latitude, 
                    request.Longitude);

                if (result.Success)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording store departure");
                return StatusCode(500, new { message = "Failed to record store departure" });
            }
        }

        [HttpPost("complete")]
        public async Task<IActionResult> CompleteMaterialRun([FromBody] CompleteMaterialRunRequest request)
        {
            try
            {
                var result = await _materialRunService.CompleteMaterialRun(
                    request.MaterialRunId, 
                    request.Latitude, 
                    request.Longitude,
                    request.Notes);

                if (result.Success)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing material run");
                return StatusCode(500, new { message = "Failed to complete material run" });
            }
        }

        [HttpGet("active/{userId}")]
        public async Task<IActionResult> GetActiveMaterialRuns(int userId)
        {
            try
            {
                var runs = await _materialRunService.GetActiveMaterialRuns(userId);
                
                return Ok(runs.Select(mr => new
                {
                    mr.Id,
                    mr.Purpose,
                    mr.DepartureTime,
                    mr.ArrivalAtStoreTime,
                    mr.DepartureFromStoreTime,
                    Job = new { mr.Job.JobNumber, mr.Job.Name },
                    Store = new { mr.MaterialStore.StoreName, mr.MaterialStore.Address }
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active material runs for user {UserId}", userId);
                return StatusCode(500, new { message = "Failed to get active material runs" });
            }
        }

        [HttpGet("history/{userId}")]
        public async Task<IActionResult> GetMaterialRunHistory(int userId, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var query = _context.MaterialRuns
                    .Include(mr => mr.Job)
                    .Include(mr => mr.MaterialStore)
                    .Where(mr => mr.UserId == userId && mr.IsCompleted);

                if (startDate.HasValue)
                    query = query.Where(mr => mr.DepartureTime >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(mr => mr.DepartureTime <= endDate.Value);

                var runs = await query
                    .OrderByDescending(mr => mr.DepartureTime)
                    .Select(mr => new
                    {
                        mr.Id,
                        mr.Purpose,
                        mr.DepartureTime,
                        mr.ReturnTime,
                        mr.TotalTimeHours,
                        mr.TravelTimeHours,
                        mr.LocationVerified,
                        Job = new { mr.Job.JobNumber, mr.Job.Name },
                        Store = new { mr.MaterialStore.StoreName, mr.MaterialStore.Address }
                    })
                    .ToListAsync();

                return Ok(runs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting material run history for user {UserId}", userId);
                return StatusCode(500, new { message = "Failed to get material run history" });
            }
        }
    }

    public class StartMaterialRunRequest
    {
        public int UserId { get; set; }
        public int JobId { get; set; }
        public int MaterialStoreId { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class MaterialRunLocationRequest
    {
        public int MaterialRunId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class CompleteMaterialRunRequest
    {
        public int MaterialRunId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}