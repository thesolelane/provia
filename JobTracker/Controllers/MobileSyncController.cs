using System;
using System.Threading.Tasks;
using JobTracker.Models;
using JobTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace JobTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MobileSyncController : ControllerBase
    {
        private readonly IMobileSyncService _syncService;
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<MobileSyncController> _logger;

        public MobileSyncController(
            IMobileSyncService syncService,
            ITenantContext tenantContext,
            ILogger<MobileSyncController> logger)
        {
            _syncService = syncService;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        /// <summary>
        /// Mobile app pushes local changes to server
        /// Receives array of CREATE, UPDATE, DELETE operations
        /// </summary>
        [HttpPost("push")]
        [Authorize]
        public async Task<ActionResult<SyncResponse>> PushSync([FromBody] SyncRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid sync request" });
            }

            try
            {
                var companyId = _tenantContext.CompanyId;
                var userId = _tenantContext.UserId;

                if (string.IsNullOrEmpty(request.DeviceId))
                {
                    return BadRequest(new { message = "Device ID is required" });
                }

                _logger.LogInformation($"Sync push received from device {request.DeviceId}: {request.Items.Count} items");

                var response = await _syncService.ProcessSyncRequestAsync(companyId, userId, request);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing sync push");
                return StatusCode(500, new { message = "Sync failed", error = ex.Message });
            }
        }

        /// <summary>
        /// Mobile app pulls changes from server
        /// Gets all updates since last sync
        /// </summary>
        [HttpPost("pull")]
        [Authorize]
        public async Task<ActionResult<SyncPullResponse>> PullSync([FromBody] SyncPullRequest request)
        {
            if (string.IsNullOrEmpty(request.DeviceId))
            {
                return BadRequest(new { message = "Device ID is required" });
            }

            try
            {
                var companyId = _tenantContext.CompanyId;
                var userId = _tenantContext.UserId;

                _logger.LogInformation($"Sync pull requested from device {request.DeviceId}");

                var response = await _syncService.GetUpdatesAsync(companyId, userId, request);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing sync pull");
                return StatusCode(500, new { message = "Pull failed", error = ex.Message });
            }
        }

        /// <summary>
        /// Register a mobile device for sync
        /// Called on app first launch
        /// </summary>
        [HttpPost("register-device")]
        [Authorize]
        public async Task<ActionResult<object>> RegisterDevice([FromBody] SyncDeviceRegistration registration)
        {
            if (string.IsNullOrEmpty(registration.DeviceId))
            {
                return BadRequest(new { message = "Device ID is required" });
            }

            try
            {
                var companyId = _tenantContext.CompanyId;
                var userId = _tenantContext.UserId;

                var success = await _syncService.RegisterDeviceAsync(
                    companyId,
                    userId,
                    registration.DeviceId,
                    registration.DeviceName,
                    registration.DeviceOS);

                if (success)
                {
                    _logger.LogInformation($"Device {registration.DeviceId} registered for user {userId}");
                    return Ok(new { message = "Device registered successfully", deviceId = registration.DeviceId });
                }
                else
                {
                    return StatusCode(500, new { message = "Failed to register device" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering device");
                return StatusCode(500, new { message = "Registration failed", error = ex.Message });
            }
        }

        /// <summary>
        /// Get status of pending sync items (for debugging/monitoring)
        /// </summary>
        [HttpGet("status/{deviceId}")]
        [Authorize]
        public async Task<ActionResult<object>> GetSyncStatus(string deviceId)
        {
            try
            {
                var companyId = _tenantContext.CompanyId;

                var pendingItems = await _syncService.GetPendingItemsAsync(companyId, deviceId);

                return Ok(new
                {
                    deviceId = deviceId,
                    pendingItemsCount = pendingItems.Count,
                    pendingItems = pendingItems
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sync status");
                return StatusCode(500, new { message = "Failed to get status", error = ex.Message });
            }
        }

        /// <summary>
        /// Clear old synced items (admin maintenance)
        /// </summary>
        [HttpPost("cleanup")]
        [Authorize]
        public async Task<ActionResult<object>> CleanupSyncQueue([FromBody] CleanupRequest request)
        {
            try
            {
                var companyId = _tenantContext.CompanyId;

                // Default: remove items older than 30 days
                var cutoffDate = DateTime.UtcNow.AddDays(-(request.DaysToKeep ?? 30));

                var success = await _syncService.ClearSyncQueueAsync(companyId, cutoffDate);

                if (success)
                {
                    _logger.LogInformation($"Cleaned up sync queue for company {companyId}");
                    return Ok(new { message = "Cleanup completed", cutoffDate = cutoffDate });
                }
                else
                {
                    return StatusCode(500, new { message = "Cleanup failed" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up sync queue");
                return StatusCode(500, new { message = "Cleanup failed", error = ex.Message });
            }
        }
    }

    // Helper request models
    public class SyncDeviceRegistration
    {
        public string DeviceId { get; set; } = string.Empty;
        public string? DeviceName { get; set; }
        public string? DeviceOS { get; set; }
        public string? AppVersion { get; set; }
    }

    public class CleanupRequest
    {
        public int? DaysToKeep { get; set; } = 30;
    }
}
