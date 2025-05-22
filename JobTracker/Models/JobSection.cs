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
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Additional navigational properties would go here
    }
}