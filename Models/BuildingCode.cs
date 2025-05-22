using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTrackerApp.Models
{
    public class BuildingCode
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string CodeNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public SectionType? RelatedSectionType { get; set; }

        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [StringLength(20)]
        public string Version { get; set; } = "Current";

        public DateTime EffectiveDate { get; set; }

        public DateTime? ExpirationDate { get; set; }

        public string? RequirementDetails { get; set; }

        public string? ComplianceGuidance { get; set; }

        public string? ReferenceUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
    }
}
