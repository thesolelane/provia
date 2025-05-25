using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace JobTracker.Models
{
    public class JobSection
    {
        public int Id { get; set; }

        [Required]
        public int JobId { get; set; }
        
        [JsonIgnore]
        public Job Job { get; set; } = null!;

        [Required]
        public int SectionType { get; set; }

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public int Status { get; set; } = 1; // 1 = Not Started (default)

        public DateTime? StartDate { get; set; }
        
        public DateTime? CompletionDate { get; set; }

        public bool IsSubcontracted { get; set; }
        
        public int? SubcontractorId { get; set; }
        
        [StringLength(100)]
        public string? ContractReference { get; set; }
        
        public int? ResponsibleEmployeeId { get; set; }
        
        public bool MaterialsOrdered { get; set; }
        
        public bool MaterialsDelivered { get; set; }
        
        public DateTime? InspectionDate { get; set; }
        
        public string? InspectionNotes { get; set; }
        
        // Detailed inspection tracking
        public bool RequiresInspection { get; set; } = false;
        
        public bool BuildingInspectionRequired { get; set; } = false;
        public bool BuildingInspectionCompleted { get; set; } = false;
        public DateTime? BuildingInspectionDate { get; set; }
        public bool BuildingInspectionPassed { get; set; } = false;
        public string? BuildingInspectionNotes { get; set; }
        
        public bool ElectricalInspectionRequired { get; set; } = false;
        public bool ElectricalInspectionCompleted { get; set; } = false;
        public DateTime? ElectricalInspectionDate { get; set; }
        public bool ElectricalInspectionPassed { get; set; } = false;
        public string? ElectricalInspectionNotes { get; set; }
        
        public bool PlumbingInspectionRequired { get; set; } = false;
        public bool PlumbingInspectionCompleted { get; set; } = false;
        public DateTime? PlumbingInspectionDate { get; set; }
        public bool PlumbingInspectionPassed { get; set; } = false;
        public string? PlumbingInspectionNotes { get; set; }
        
        // Additional notes field for section
        [StringLength(1000)]
        public string? Notes { get; set; }
        
        // Flag to track if section is collapsed in UI
        public bool IsCollapsed { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Collection of images for this section
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ICollection<JobImage> Images { get; set; } = new List<JobImage>();
    }
}