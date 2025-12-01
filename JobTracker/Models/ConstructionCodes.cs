using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTracker.Models
{
    /// <summary>
    /// Cost Type - Global cost type codes (0-5)
    /// Labor, Material, Equipment, Subcontract, Fees, Misc
    /// </summary>
    public class CostType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int Code { get; set; } // 0-5

        [Required]
        [StringLength(50)]
        public string Label { get; set; } = string.Empty; // "Labor", "Material", etc.

        [StringLength(200)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Construction Department - 3-digit department codes (100-900)
    /// General Conditions, Demolition, Framing, Plumbing, Electrical, etc.
    /// </summary>
    public class ConstructionDepartment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DeptCode { get; set; } // 100, 200, 300, ... 900

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty; // "Flooring", "Electrical", etc.

        [StringLength(300)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<DepartmentSubcategory>? Subcategories { get; set; }
        public virtual ICollection<ConstructionCode>? Codes { get; set; }
    }

    /// <summary>
    /// Department Subcategory - 2-digit subcodes per department (00-99)
    /// Water Supply, Sewer/DWV, Heating, Gas Piping, etc.
    /// </summary>
    public class DepartmentSubcategory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DepartmentId { get; set; }
        public ConstructionDepartment? Department { get; set; }

        [Required]
        public int DeptCode { get; set; } // Parent dept code for easy lookups

        [Required]
        [StringLength(2)]
        public string SubCode { get; set; } = "00"; // "01", "02", etc.

        [Required]
        [StringLength(100)]
        public string Label { get; set; } = string.Empty; // "Water supply", "Sewer/DWV", etc.

        [StringLength(300)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<ConstructionCode>? Codes { get; set; }
    }

    /// <summary>
    /// Construction Code - Full 6-digit cost codes (flattened)
    /// Format: DDD L TT (Dept + CostType + Subcode)
    /// Example: 720102 = Flooring + Labor + Tile flooring
    /// </summary>
    public class ConstructionCode
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(6)]
        public string FullCode { get; set; } = string.Empty; // "720102"

        [Required]
        public int DeptCode { get; set; } // 720

        [Required]
        public int CostTypeCode { get; set; } // 1 (Labor)

        [Required]
        [StringLength(2)]
        public string SubCode { get; set; } = "00"; // "02"

        // Denormalized for quick lookups
        [StringLength(100)]
        public string? DeptName { get; set; } // "Flooring"

        [StringLength(50)]
        public string? CostTypeLabel { get; set; } // "Labor"

        [StringLength(100)]
        public string? SubLabel { get; set; } // "Tile flooring"

        // Full display name
        [StringLength(300)]
        public string? DisplayName { get; set; } // "Flooring - Labor - Tile flooring"

        // For Wave sync
        [StringLength(100)]
        public string? WaveProductId { get; set; }
        public DateTime? LastSyncedToWave { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Foreign keys
        public int? DepartmentId { get; set; }
        public ConstructionDepartment? Department { get; set; }

        public int? SubcategoryId { get; set; }
        public DepartmentSubcategory? Subcategory { get; set; }
    }

    /// <summary>
    /// Code Sync Log - Track syncs to external systems (Wave, QB, etc.)
    /// </summary>
    public class CodeSyncLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ConstructionCodeId { get; set; }
        public ConstructionCode? ConstructionCode { get; set; }

        [Required]
        [StringLength(50)]
        public string ExternalSystem { get; set; } = "WAVE"; // WAVE, QUICKBOOKS, etc.

        [StringLength(100)]
        public string? ExternalId { get; set; } // Wave product ID, etc.

        [Required]
        [StringLength(20)]
        public string SyncAction { get; set; } = "CREATE"; // CREATE, UPDATE, DELETE

        [Required]
        [StringLength(20)]
        public string SyncStatus { get; set; } = "SUCCESS"; // SUCCESS, FAILED, PENDING

        [StringLength(500)]
        public string? ErrorMessage { get; set; }

        public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
    }

    // ============ DTOs ============

    /// <summary>
    /// DTO for cost type
    /// </summary>
    public class CostTypeDto
    {
        public int Code { get; set; }
        public string Label { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    /// <summary>
    /// DTO for department with subcategories
    /// </summary>
    public class DepartmentDto
    {
        public int DeptCode { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<SubcategoryDto> Subcategories { get; set; } = new();
    }

    /// <summary>
    /// DTO for subcategory
    /// </summary>
    public class SubcategoryDto
    {
        public int DeptCode { get; set; }
        public string SubCode { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    /// <summary>
    /// DTO for full construction code
    /// </summary>
    public class ConstructionCodeDto
    {
        public string FullCode { get; set; } = string.Empty;
        public int DeptCode { get; set; }
        public int CostTypeCode { get; set; }
        public string SubCode { get; set; } = string.Empty;
        public string? DeptName { get; set; }
        public string? CostTypeLabel { get; set; }
        public string? SubLabel { get; set; }
        public string? DisplayName { get; set; }
        public string? WaveProductId { get; set; }
        public DateTime? LastSyncedToWave { get; set; }
    }

    /// <summary>
    /// Request DTO for generating codes
    /// </summary>
    public class GenerateCodesRequest
    {
        public int? DeptCode { get; set; } // Generate for specific dept only
        public bool OverwriteExisting { get; set; } = false;
    }

    /// <summary>
    /// Request DTO for syncing codes to Wave
    /// </summary>
    public class SyncToWaveRequest
    {
        public List<string>? FullCodes { get; set; } // Specific codes, or null for all
        public bool ForceSync { get; set; } = false;
    }
}
