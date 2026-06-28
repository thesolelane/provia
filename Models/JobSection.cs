using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTrackerApp.Models
{
    public class JobSection
    {
        public int Id { get; set; }

        [Required]
        public int JobId { get; set; }
        
        [ForeignKey("JobId")]
        public Job? Job { get; set; }

        [Required]
        public SectionType SectionType { get; set; }

        [Required]
        public SectionStatus Status { get; set; } = SectionStatus.NotStarted;

        [StringLength(200)]
        public string? Description { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? CompletionDate { get; set; }

        public int? ResponsibleEmployeeId { get; set; }
        
        [ForeignKey("ResponsibleEmployeeId")]
        public Employee? ResponsibleEmployee { get; set; }

        public bool IsSubcontracted { get; set; } = false;

        public int? SubcontractorId { get; set; }
        
        [ForeignKey("SubcontractorId")]
        public Subcontractor? Subcontractor { get; set; }

        [StringLength(100)]
        public string? ContractReference { get; set; }

        public DateTime? InspectionDate { get; set; }

        public DateTime? ReinspectionDate { get; set; }

        public string? InspectionNotes { get; set; }

        public string? InspectionResult { get; set; }

        public string? PermitNumber { get; set; }

        public DateTime? PermitIssueDate { get; set; }

        public DateTime? PermitExpirationDate { get; set; }

        public string? MaterialsRequired { get; set; }

        public bool MaterialsOrdered { get; set; } = false;

        public bool MaterialsReceived { get; set; } = false;

        public string? AdditionalNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;

        // Specific fields for different section types could be added here
        // or in child classes if using Table-Per-Hierarchy inheritance
        
        // Navigation properties
        public ICollection<SectionSubcontractor> SectionSubcontractors { get; set; } = new List<SectionSubcontractor>();
    }
}
