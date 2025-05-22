using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace JobTracker.Models
{
    public class Job
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Location { get; set; } = string.Empty;

        [StringLength(50)]
        public string JobNumber { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? TargetCompletionDate { get; set; }

        public DateTime? ActualCompletionDate { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        [StringLength(100)]
        public string ClientName { get; set; } = string.Empty;

        [StringLength(100)]
        public string ClientEmail { get; set; } = string.Empty;

        [StringLength(20)]
        public string ClientPhone { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Budget { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ActualCost { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ICollection<JobSection> Sections { get; set; } = new List<JobSection>();
    }
}