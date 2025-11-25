using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTracker.Models
{
    // Represents a pending sync item waiting to be uploaded
    public class SyncQueueItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CompanyId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public string DeviceId { get; set; } = string.Empty;

        // Type of entity: "TimeEntry", "LocationTracking", "JobUpdate", "IssueReport", etc.
        [Required]
        public string EntityType { get; set; } = string.Empty;

        // ID of the entity in the local mobile database
        [Required]
        public string LocalEntityId { get; set; } = string.Empty;

        // Server ID if already synced (for updates)
        public int? ServerEntityId { get; set; }

        // Operation: "CREATE", "UPDATE", "DELETE"
        [Required]
        public string Operation { get; set; } = "CREATE";

        // JSON payload of the data to sync
        [Required]
        public string DataPayload { get; set; } = string.Empty;

        // Status: "PENDING", "SYNCING", "SUCCESS", "FAILED", "CONFLICT"
        [Required]
        public string Status { get; set; } = "PENDING";

        // Number of sync attempts
        public int RetryCount { get; set; } = 0;

        // Error message if failed
        public string? ErrorMessage { get; set; }

        // When the mobile device created this record
        public DateTime LocalCreatedAt { get; set; } = DateTime.UtcNow;

        // When the mobile device last modified this record (for conflict detection)
        public DateTime LocalModifiedAt { get; set; } = DateTime.UtcNow;

        // When the server received and processed it
        public DateTime? ServerSyncedAt { get; set; }

        // For automatic cleanup of old synced items
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // Tracks conflicts when the same data was modified offline by multiple sources
    public class SyncConflict
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CompanyId { get; set; }

        // The entity that has conflicting versions
        [Required]
        public string EntityType { get; set; } = string.Empty;

        [Required]
        public int EntityId { get; set; }

        // Device that tried to sync conflicting data
        [Required]
        public string ConflictingDeviceId { get; set; } = string.Empty;

        // JSON of the conflicting data from mobile
        [Required]
        public string MobileVersion { get; set; } = string.Empty;

        // JSON of the current server version
        [Required]
        public string ServerVersion { get; set; } = string.Empty;

        // Who resolved the conflict
        public int? ResolvedByUserId { get; set; }

        // How it was resolved: "SERVER_WINS", "MOBILE_WINS", "MERGED", "MANUAL"
        public string? ResolutionStrategy { get; set; }

        // The final version that was kept
        public string? FinalVersion { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
    }

    // Tracks device registration for sync purposes
    public class SyncDevice
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CompanyId { get; set; }

        [Required]
        public int UserId { get; set; }

        // Unique device identifier (from mobile app)
        [Required]
        public string DeviceId { get; set; } = string.Empty;

        // Device info: model, OS, version
        public string? DeviceName { get; set; }
        public string? DeviceOS { get; set; }
        public string? DeviceOSVersion { get; set; }
        public string? AppVersion { get; set; }

        // Last sync information
        public DateTime? LastSyncAt { get; set; }
        public int? LastSyncedQueueItemId { get; set; }
        public int TotalSyncedItems { get; set; } = 0;

        // Device status
        public bool IsActive { get; set; } = true;
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    // Request model for mobile app to initiate sync
    public class SyncRequest
    {
        [Required]
        public string DeviceId { get; set; } = string.Empty;

        public string? DeviceName { get; set; }
        public string? DeviceOS { get; set; }
        public string? DeviceOSVersion { get; set; }

        // Array of pending sync items from mobile local storage
        [Required]
        public List<SyncItem> Items { get; set; } = new();

        // Last sync timestamp the device has
        public DateTime? LastSyncAt { get; set; }
    }

    // Individual item in sync request
    public class SyncItem
    {
        [Required]
        public string LocalId { get; set; } = string.Empty;

        [Required]
        public string EntityType { get; set; } = string.Empty;

        [Required]
        public string Operation { get; set; } = "CREATE"; // CREATE, UPDATE, DELETE

        [Required]
        public string Data { get; set; } = string.Empty; // JSON

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    }

    // Response model for successful sync
    public class SyncResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public List<SyncResult> Results { get; set; } = new();
        public DateTime ServerTime { get; set; } = DateTime.UtcNow;
    }

    // Result of syncing a single item
    public class SyncResult
    {
        [Required]
        public string LocalId { get; set; } = string.Empty;

        public int? ServerId { get; set; }

        public string Status { get; set; } = "SUCCESS"; // SUCCESS, FAILED, CONFLICT

        public string? ErrorMessage { get; set; }

        public string? ConflictStrategy { get; set; }
    }

    // Pull request - mobile app asking what's changed on server since last sync
    public class SyncPullRequest
    {
        [Required]
        public string DeviceId { get; set; } = string.Empty;

        public DateTime? LastSyncAt { get; set; }
    }

    // Pull response - server sending updates to mobile app
    public class SyncPullResponse
    {
        public bool Success { get; set; }
        public DateTime ServerTime { get; set; } = DateTime.UtcNow;
        public List<EntityUpdate> Updates { get; set; } = new();
        public List<string> DeletedIds { get; set; } = new();
    }

    // Update received from server for local mobile storage
    public class EntityUpdate
    {
        [Required]
        public string EntityType { get; set; } = string.Empty;

        [Required]
        public int ServerId { get; set; }

        [Required]
        public string Data { get; set; } = string.Empty; // JSON

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
