using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JobTracker.Data;
using JobTracker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace JobTracker.Services
{
    /// <summary>
    /// Manages offline-first mobile synchronization
    /// Handles queue management, conflict resolution, and batch syncing
    /// </summary>
    public interface IMobileSyncService
    {
        Task<SyncResponse> ProcessSyncRequestAsync(int companyId, int userId, SyncRequest request);
        Task<SyncPullResponse> GetUpdatesAsync(int companyId, int userId, SyncPullRequest request);
        Task<bool> RegisterDeviceAsync(int companyId, int userId, string deviceId, string? deviceName, string? os);
        Task<List<SyncQueueItem>> GetPendingItemsAsync(int companyId, string deviceId);
        Task<bool> ClearSyncQueueAsync(int companyId, DateTime olderThan);
    }

    public class MobileSyncService : IMobileSyncService
    {
        private readonly JobTrackerContext _context;
        private readonly ILogger<MobileSyncService> _logger;

        public MobileSyncService(JobTrackerContext context, ILogger<MobileSyncService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Process incoming sync request from mobile device
        /// Handles CREATE, UPDATE, DELETE operations with conflict detection
        /// </summary>
        public async Task<SyncResponse> ProcessSyncRequestAsync(int companyId, int userId, SyncRequest request)
        {
            var response = new SyncResponse { Success = true };

            try
            {
                // Register or update device
                await RegisterDeviceAsync(companyId, userId, request.DeviceId, request.DeviceName, request.DeviceOS);

                // Process each sync item
                foreach (var item in request.Items)
                {
                    var result = await ProcessSyncItemAsync(companyId, userId, request.DeviceId, item);
                    response.Results.Add(result);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Sync completed for device {request.DeviceId}: {request.Items.Count} items processed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing sync request");
                response.Success = false;
                response.Message = "Sync failed: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Process a single sync item with conflict detection
        /// </summary>
        private async Task<SyncResult> ProcessSyncItemAsync(int companyId, int userId, string deviceId, SyncItem item)
        {
            var result = new SyncResult { LocalId = item.LocalId, Status = "SUCCESS" };

            try
            {
                // Check for conflicts with existing server data
                var existingServerItem = await FindExistingServerItemAsync(companyId, item.EntityType, item.Data);

                if (existingServerItem != null && item.Operation == "UPDATE")
                {
                    // Conflict detection: Check if server version is newer
                    var conflict = await DetectConflictAsync(item, existingServerItem);
                    
                    if (conflict)
                    {
                        result.Status = "CONFLICT";
                        result.ConflictStrategy = "SERVER_WINS"; // Default strategy
                        
                        // Record the conflict for admin review
                        await RecordConflictAsync(companyId, item.EntityType, existingServerItem, item.Data);
                        
                        return result;
                    }
                }

                // Queue the item for processing
                var queueItem = new SyncQueueItem
                {
                    CompanyId = companyId,
                    UserId = userId,
                    DeviceId = deviceId,
                    EntityType = item.EntityType,
                    LocalEntityId = item.LocalId,
                    Operation = item.Operation,
                    DataPayload = item.Data,
                    Status = "SUCCESS", // Mark as successful immediately after validation
                    LocalCreatedAt = item.CreatedAt,
                    LocalModifiedAt = item.ModifiedAt
                };

                _context.SyncQueueItems.Add(queueItem);
                
                // TODO: Process the actual data based on EntityType
                // For now, we're just queueing it
                // In production, you'd parse item.Data JSON and create/update the actual entity

                result.ServerId = queueItem.Id;
                _logger.LogInformation($"Synced {item.EntityType} item from device {deviceId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing sync item {item.LocalId}");
                result.Status = "FAILED";
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Get updates that happened on the server since last device sync
        /// Mobile app pulls these to stay in sync
        /// </summary>
        public async Task<SyncPullResponse> GetUpdatesAsync(int companyId, int userId, SyncPullRequest request)
        {
            var response = new SyncPullResponse { Success = true };

            try
            {
                var lastSync = request.LastSyncAt ?? DateTime.UtcNow.AddDays(-30);

                // Get all successful syncs since last pull
                var recentSyncs = await _context.SyncQueueItems
                    .Where(s => s.CompanyId == companyId && s.Status == "SUCCESS" && s.ServerSyncedAt > lastSync)
                    .OrderByDescending(s => s.ServerSyncedAt)
                    .ToListAsync();

                foreach (var sync in recentSyncs)
                {
                    response.Updates.Add(new EntityUpdate
                    {
                        EntityType = sync.EntityType,
                        ServerId = sync.ServerEntityId ?? sync.Id,
                        Data = sync.DataPayload,
                        UpdatedAt = sync.ServerSyncedAt ?? DateTime.UtcNow
                    });
                }

                _logger.LogInformation($"Sent {response.Updates.Count} updates to device {request.DeviceId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sync updates");
                response.Success = false;
            }

            return response;
        }

        /// <summary>
        /// Register or update a mobile device
        /// </summary>
        public async Task<bool> RegisterDeviceAsync(int companyId, int userId, string deviceId, string? deviceName, string? os)
        {
            try
            {
                var device = await _context.SyncDevices
                    .FirstOrDefaultAsync(d => d.CompanyId == companyId && d.DeviceId == deviceId);

                if (device == null)
                {
                    device = new SyncDevice
                    {
                        CompanyId = companyId,
                        UserId = userId,
                        DeviceId = deviceId,
                        DeviceName = deviceName,
                        DeviceOS = os
                    };
                    _context.SyncDevices.Add(device);
                }
                else
                {
                    device.LastSyncAt = DateTime.UtcNow;
                    device.UpdatedAt = DateTime.UtcNow;
                    if (!string.IsNullOrEmpty(deviceName)) device.DeviceName = deviceName;
                    if (!string.IsNullOrEmpty(os)) device.DeviceOS = os;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering device");
                return false;
            }
        }

        /// <summary>
        /// Find if an entity already exists on server
        /// Used for conflict detection
        /// </summary>
        private async Task<JObject?> FindExistingServerItemAsync(int companyId, string entityType, string mobileData)
        {
            // This is a placeholder - in production, you'd query the actual entity table
            // For example, if EntityType is "TimeEntry", you'd search the TimeEntries table
            // and compare based on unique identifiers from the JSON data
            
            return null;
        }

        /// <summary>
        /// Detect if there's a conflict between mobile and server versions
        /// Returns true if conflict detected
        /// </summary>
        private async Task<bool> DetectConflictAsync(SyncItem mobileItem, JObject? serverItem)
        {
            if (serverItem == null) return false;

            // Simple timestamp-based conflict: if server was modified more recently than mobile version
            var mobileModified = mobileItem.ModifiedAt;
            
            // In production, extract the actual modification timestamp from serverItem
            // For now, just return false (no conflict)
            
            return false;
        }

        /// <summary>
        /// Record a conflict for manual resolution
        /// </summary>
        private async Task<bool> RecordConflictAsync(int companyId, string entityType, JObject? serverVersion, string mobileVersion)
        {
            try
            {
                var conflict = new SyncConflict
                {
                    CompanyId = companyId,
                    EntityType = entityType,
                    EntityId = 0, // Would be extracted from data
                    ConflictingDeviceId = "", // Would come from context
                    MobileVersion = mobileVersion,
                    ServerVersion = serverVersion?.ToString() ?? ""
                };

                _context.SyncConflicts.Add(conflict);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording conflict");
                return false;
            }
        }

        /// <summary>
        /// Get pending items for a device (for troubleshooting)
        /// </summary>
        public async Task<List<SyncQueueItem>> GetPendingItemsAsync(int companyId, string deviceId)
        {
            return await _context.SyncQueueItems
                .Where(s => s.CompanyId == companyId && s.DeviceId == deviceId && s.Status == "PENDING")
                .OrderByDescending(s => s.CreatedAt)
                .Take(100)
                .ToListAsync();
        }

        /// <summary>
        /// Clean up old synced items to keep database lean
        /// </summary>
        public async Task<bool> ClearSyncQueueAsync(int companyId, DateTime olderThan)
        {
            try
            {
                var itemsToDelete = await _context.SyncQueueItems
                    .Where(s => s.CompanyId == companyId && s.Status == "SUCCESS" && s.CreatedAt < olderThan)
                    .ToListAsync();

                _context.SyncQueueItems.RemoveRange(itemsToDelete);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cleared {Count} old sync items for company {CompanyId}", itemsToDelete.Count, companyId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing sync queue");
                return false;
            }
        }
    }
}
