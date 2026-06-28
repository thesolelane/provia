using System;
using System.ComponentModel.DataAnnotations;

namespace JobTracker.Models
{
    /// <summary>
    /// Registry of Massachusetts towns and their permit platforms
    /// Used to route permit submissions to the correct municipal system
    /// </summary>
    public class MunicipalPortal
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string TownName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? County { get; set; }

        [Required]
        [StringLength(50)]
        public string PlatformType { get; set; } = string.Empty; // ACCELA, OPENGOV, PERMITEYES, CUSTOM, NONE

        [StringLength(500)]
        public string? PortalUrl { get; set; }

        [StringLength(500)]
        public string? ApiBaseUrl { get; set; }

        [StringLength(100)]
        public string? ApiVersion { get; set; }

        public bool HasOnlineSubmission { get; set; } = false;

        public bool HasOnlinePayment { get; set; } = false;

        public bool HasInspectionScheduling { get; set; } = false;

        public bool HasStatusTracking { get; set; } = false;

        [StringLength(100)]
        public string? BuildingDeptPhone { get; set; }

        [StringLength(200)]
        public string? BuildingDeptEmail { get; set; }

        [StringLength(500)]
        public string? BuildingDeptAddress { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Audit log for tracking all permit-related actions
    /// Essential for compliance and debugging municipal integrations
    /// </summary>
    public class PermitAuditLog
    {
        [Key]
        public int Id { get; set; }

        public int? CompanyId { get; set; }

        public int? UserId { get; set; }

        public int? JobId { get; set; }

        public int? JobPermitId { get; set; }

        [Required]
        [StringLength(50)]
        public string ActionType { get; set; } = string.Empty; // CREATE, UPDATE, SUBMIT, FETCH_GIS, GENERATE_PDF, PORTAL_SYNC, etc.

        [Required]
        [StringLength(100)]
        public string EntityType { get; set; } = string.Empty; // Job, JobPermit, PropertyProfile, PermitDocument, etc.

        public int? EntityId { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public string? OldValues { get; set; } // JSON of previous values

        public string? NewValues { get; set; } // JSON of new values

        [StringLength(100)]
        public string? MunicipalPortal { get; set; } // Which portal was involved (if any)

        [StringLength(100)]
        public string? ExternalReferenceId { get; set; } // Permit # from municipal system

        [StringLength(50)]
        public string? Status { get; set; } // SUCCESS, FAILED, PENDING

        [StringLength(1000)]
        public string? ErrorMessage { get; set; }

        [StringLength(50)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Secure storage for company's municipal portal credentials
    /// Each company can have credentials for multiple municipal systems
    /// </summary>
    public class CompanyPortalCredential
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CompanyId { get; set; }
        public Company? Company { get; set; }

        [Required]
        public int MunicipalPortalId { get; set; }
        public MunicipalPortal? MunicipalPortal { get; set; }

        [StringLength(200)]
        public string? Username { get; set; }

        [StringLength(500)]
        public string? EncryptedPassword { get; set; } // Encrypted, never store plain text

        [StringLength(500)]
        public string? EncryptedApiKey { get; set; } // Encrypted API key if applicable

        [StringLength(500)]
        public string? EncryptedClientSecret { get; set; } // For OAuth integrations

        [StringLength(200)]
        public string? OAuthRefreshToken { get; set; } // Encrypted refresh token

        public DateTime? TokenExpiresAt { get; set; }

        [StringLength(100)]
        public string? AccountNumber { get; set; } // Contractor account # with municipality

        [StringLength(100)]
        public string? LicenseNumber { get; set; } // CSL or contractor license on file

        public bool IsActive { get; set; } = true;

        public bool IsVerified { get; set; } = false; // Has credential been tested?

        public DateTime? LastUsedAt { get; set; }

        public DateTime? LastVerifiedAt { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedByUserId { get; set; }
    }
}
