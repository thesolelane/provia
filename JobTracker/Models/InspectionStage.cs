using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTracker.Models
{
    /// <summary>
    /// Multi-stage inspection tracking for licensed sub-contractors
    /// Stages: Rough → Second → Finish → Final Master Permit Signoff
    /// </summary>
    public class InspectionStage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int JobBidId { get; set; }
        public JobBid? JobBid { get; set; }

        [Required]
        public int CompanyId { get; set; }

        // Inspection Stage: ROUGH, SECOND, FINISH, FINAL_SIGNOFF
        [Required]
        [StringLength(20)]
        public string Stage { get; set; } = "ROUGH";

        // Inspection Status: PENDING, PASSED, FAILED, INCOMPLETE
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "PENDING";

        // Inspector info
        public int? InspectorUserId { get; set; }
        public User? Inspector { get; set; }

        // Inspection details
        [StringLength(1000)]
        public string? InspectionNotes { get; set; }

        // Pass/Fail
        public bool Passed { get; set; } = false;
        public DateTime? InspectionDate { get; set; }

        // For rough inspection: Items to check off
        [StringLength(500)]
        public string? ChecklistItems { get; set; }

        // Required permits for this stage
        [StringLength(500)]
        public string? RequiredPermits { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Uploaded permit document tracking - sub uploads signed permits as proof
    /// For inspection stage verification (differs from generated PermitDocument in PermitDocument.cs)
    /// </summary>
    public class UploadedPermitDoc
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int InspectionStageId { get; set; }
        public InspectionStage? InspectionStage { get; set; }

        [Required]
        public int JobBidId { get; set; }
        public JobBid? JobBid { get; set; }

        [Required]
        public int CompanyId { get; set; }

        // Permit type: ELECTRICAL, PLUMBING, HVAC, SAFETY, MASTER, etc
        [Required]
        [StringLength(50)]
        public string PermitType { get; set; } = string.Empty;

        // Permit number from city/municipality
        [StringLength(100)]
        public string? PermitNumber { get; set; }

        // Document file path or URL
        [StringLength(500)]
        public string? DocumentUrl { get; set; }

        // File metadata
        [StringLength(200)]
        public string? FileName { get; set; }

        [StringLength(20)]
        public string? FileType { get; set; }

        // Permit status
        [StringLength(20)]
        public string Status { get; set; } = "PENDING_REVIEW";

        // Review info
        public int? ReviewedByUserId { get; set; }
        public User? ReviewedBy { get; set; }

        [StringLength(500)]
        public string? ReviewNotes { get; set; }

        public DateTime? ReviewedAt { get; set; }

        // Issue/Expiry dates
        public DateTime? IssuedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }

        // Uploaded by
        public int? UploadedByUserId { get; set; }
        public User? UploadedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// DTO for inspection operations
    /// </summary>
    public class InspectionStageDto
    {
        public int Id { get; set; }
        public int JobBidId { get; set; }
        public string Stage { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? InspectionNotes { get; set; }
        public bool Passed { get; set; }
        public DateTime? InspectionDate { get; set; }
        public string? ChecklistItems { get; set; }
        public string? RequiredPermits { get; set; }
        public List<PermitDocumentDto> PermitDocuments { get; set; } = new();
    }

    /// <summary>
    /// DTO for permit documents
    /// </summary>
    public class PermitDocumentDto
    {
        public int Id { get; set; }
        public string PermitType { get; set; } = string.Empty;
        public string? PermitNumber { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public DateTime? IssuedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? ReviewNotes { get; set; }
    }
}
