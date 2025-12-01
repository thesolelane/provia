using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTracker.Models
{
    /// <summary>
    /// Code Book - Store jurisdiction code references (780 CMR, 248 CMR, etc.)
    /// Massachusetts codes: Building, Plumbing/Gas, Electrical, Fire, Accessibility
    /// </summary>
    public class CodeBook
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty; // "780 CMR", "248 CMR", "527 CMR 12.00", etc.

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty; // "Massachusetts State Building Code"

        [StringLength(50)]
        public string Edition { get; set; } = string.Empty; // "10th Edition", "2021 I-Codes", etc.

        [StringLength(100)]
        public string Jurisdiction { get; set; } = "Massachusetts";

        // Which trade/department this code applies to
        [StringLength(50)]
        public string AppliesToDepartment { get; set; } = string.Empty; // BUILDING, ELECTRICAL, PLUMBING, FIRE, ACCESSIBILITY

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? ReferenceUrl { get; set; } // Link to official code document

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<CodeRule>? CodeRules { get; set; }
    }

    /// <summary>
    /// Code Rule - Conditions that trigger code requirements based on scope selections
    /// When a scope is selected, matching rules auto-attach permits and inspections
    /// </summary>
    public class CodeRule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CodeBookId { get; set; }
        public CodeBook? CodeBook { get; set; }

        [Required]
        [StringLength(100)]
        public string RuleName { get; set; } = string.Empty; // "New Bedroom Egress", "Service Upgrade", etc.

        // Trigger conditions - which scope selections trigger this rule
        [Required]
        [StringLength(100)]
        public string TriggerScope { get; set; } = string.Empty; // "NEW_BEDROOM", "ELECTRICAL_SERVICE_UPGRADE", etc.

        // What this rule requires
        [StringLength(100)]
        public string? RequiredPermitType { get; set; } // "BUILDING", "ELECTRICAL", "PLUMBING", etc.

        [StringLength(100)]
        public string? RequiredInspectionType { get; set; } // "ROUGH_ELECTRICAL", "FINAL_BUILDING", etc.

        // Code section reference
        [StringLength(100)]
        public string? CodeSection { get; set; } // "Section R310", "Article 230", etc.

        [StringLength(500)]
        public string? Description { get; set; }

        // Priority for inspection ordering (lower = earlier)
        public int InspectionOrder { get; set; } = 100;

        // Which department reviews this
        [StringLength(50)]
        public string Department { get; set; } = "BUILDING"; // BUILDING, ELECTRICAL, PLUMBING, MECHANICAL, FIRE

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Scope Category - Define available scope categories for Job Options Wizard
    /// Exterior, Interior, Plumbing, Electrical, HVAC, Sheet Metal, Finishes, Accessibility
    /// </summary>
    public class ScopeCategory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string CategoryCode { get; set; } = string.Empty; // "EXTERIOR", "PLUMBING", "ELECTRICAL", etc.

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty; // "Exterior Work", "Plumbing Systems", etc.

        [StringLength(500)]
        public string? Description { get; set; }

        // Display order in wizard
        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        // Navigation
        public virtual ICollection<ScopeItem>? ScopeItems { get; set; }
    }

    /// <summary>
    /// Scope Item - Individual scope options within a category
    /// These are the checkboxes in the Job Options Wizard
    /// </summary>
    public class ScopeItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ScopeCategoryId { get; set; }
        public ScopeCategory? ScopeCategory { get; set; }

        [Required]
        [StringLength(50)]
        public string ItemCode { get; set; } = string.Empty; // "ROOFING", "SIDING", "SERVICE_UPGRADE", etc.

        [Required]
        [StringLength(100)]
        public string ItemName { get; set; } = string.Empty; // "Roofing Replacement", "Siding Installation", etc.

        [StringLength(500)]
        public string? Description { get; set; }

        // Which trade typically does this work
        [StringLength(50)]
        public string? TradeType { get; set; } // "ROOFING", "ELECTRICAL", "PLUMBING", etc.

        // Does this require a licensed sub with permit authority?
        public bool RequiresLicensedTrade { get; set; } = false;

        // Which department reviews this
        [StringLength(50)]
        public string? Department { get; set; } // BUILDING, ELECTRICAL, PLUMBING, MECHANICAL, FIRE

        // Display order within category
        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Job Scope - Links a job to selected scope items
    /// This is what gets saved when supervisor completes the Job Options Wizard
    /// </summary>
    public class JobScope
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int JobId { get; set; }
        public Job? Job { get; set; }

        [Required]
        public int ScopeItemId { get; set; }
        public ScopeItem? ScopeItem { get; set; }

        [Required]
        public int CompanyId { get; set; }

        // Additional details for this scope selection
        [StringLength(500)]
        public string? Notes { get; set; }

        // Quantity or extent (e.g., "1500 sq ft" for roofing)
        [StringLength(100)]
        public string? Quantity { get; set; }

        // Status of this scope item
        [StringLength(20)]
        public string Status { get; set; } = "PENDING"; // PENDING, IN_PROGRESS, COMPLETE, CANCELLED

        // Who is assigned to this scope
        public int? AssignedUserId { get; set; }
        public User? AssignedUser { get; set; }

        // Is this sub the permit holder for this trade?
        public bool IsPermitHolder { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Trade Assignment - Assigns trades/workers to jobs with specific roles
    /// Tracks who is doing what on each job
    /// </summary>
    public class TradeAssignment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int JobId { get; set; }
        public Job? Job { get; set; }

        [Required]
        public int UserId { get; set; }
        public User? User { get; set; }

        [Required]
        public int CompanyId { get; set; }

        // Assignment type: FOREMAN, SUBCONTRACTOR, FIELD_OPERATOR
        [Required]
        [StringLength(30)]
        public string AssignmentType { get; set; } = string.Empty;

        // Trade specialty for subs
        [StringLength(50)]
        public string? TradeType { get; set; } // PLUMBER, ELECTRICIAN, HVAC, SHEET_METAL, ROOFING, etc.

        // Is this person the permit holder for their trade on this job?
        public bool IsPermitHolder { get; set; } = false;

        // Status
        [StringLength(20)]
        public string Status { get; set; } = "ACTIVE"; // ACTIVE, INACTIVE, COMPLETED

        // Assignment dates
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Job Permit - Tracks permit applications and status for jobs
    /// </summary>
    public class JobPermit
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int JobId { get; set; }
        public Job? Job { get; set; }

        [Required]
        public int CompanyId { get; set; }

        [Required]
        [StringLength(50)]
        public string PermitType { get; set; } = string.Empty; // BUILDING, ELECTRICAL, PLUMBING, GAS, FIRE

        [StringLength(50)]
        public string Status { get; set; } = "PENDING"; // PENDING, PREPARING, SUBMITTED, UNDER_REVIEW, APPROVED, DENIED

        [StringLength(100)]
        public string? PermitNumber { get; set; } // Assigned by municipality after approval

        [StringLength(200)]
        public string? IssuingAuthority { get; set; } // "Boston Building Department", etc.

        // Application tracking
        public DateTime? ApplicationDate { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime? ExpirationDate { get; set; }

        // Fees
        [Column(TypeName = "decimal(10,2)")]
        public decimal? ApplicationFee { get; set; }
        public bool FeePaid { get; set; } = false;

        // Documents checklist
        public bool HasPlotPlan { get; set; } = false;
        public bool HasConstructionDrawings { get; set; } = false;
        public bool HasContractorLicense { get; set; } = false;
        public bool HasOwnerAuthorization { get; set; } = false;

        // Notes and details
        [StringLength(1000)]
        public string? Notes { get; set; }

        [StringLength(500)]
        public string? DenialReason { get; set; }

        // Who is handling this permit
        public int? AssignedUserId { get; set; }
        public User? AssignedUser { get; set; }

        // Code reference
        [StringLength(100)]
        public string? CodeReference { get; set; } // "780 CMR", "527 CMR", etc.

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// DTO for Job Options Wizard scope selections
    /// </summary>
    public class JobScopeSelectionDto
    {
        public int JobId { get; set; }
        public List<int> SelectedScopeItemIds { get; set; } = new();
        public string? Notes { get; set; }
    }

    /// <summary>
    /// DTO for scope category with items
    /// </summary>
    public class ScopeCategoryDto
    {
        public int Id { get; set; }
        public string CategoryCode { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<ScopeItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// DTO for individual scope item
    /// </summary>
    public class ScopeItemDto
    {
        public int Id { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? TradeType { get; set; }
        public bool RequiresLicensedTrade { get; set; }
        public string? Department { get; set; }
    }

    /// <summary>
    /// DTO for code rule with book info
    /// </summary>
    public class CodeRuleDto
    {
        public int Id { get; set; }
        public string RuleName { get; set; } = string.Empty;
        public string TriggerScope { get; set; } = string.Empty;
        public string? RequiredPermitType { get; set; }
        public string? RequiredInspectionType { get; set; }
        public string? CodeSection { get; set; }
        public string? Description { get; set; }
        public string Department { get; set; } = string.Empty;
        public string CodeBookName { get; set; } = string.Empty;
        public string CodeBookCode { get; set; } = string.Empty;
    }
}
