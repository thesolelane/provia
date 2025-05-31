using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTracker.Models
{
    public class IssueReport
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        public int? JobId { get; set; }

        [Required]
        [StringLength(100)]
        public string IssueType { get; set; } = "";

        [Required]
        [StringLength(50)]
        public string Priority { get; set; } = "";

        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = "";

        [StringLength(500)]
        public string? LocationNotes { get; set; }

        [Column(TypeName = "decimal(10,8)")]
        public double? Latitude { get; set; }

        [Column(TypeName = "decimal(11,8)")]
        public double? Longitude { get; set; }

        public double? Accuracy { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Open";

        public DateTime SubmittedAt { get; set; }

        public DateTime? ResolvedAt { get; set; }

        public int? ResolvedByUserId { get; set; }

        [StringLength(1000)]
        public string? ResolutionNotes { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("JobId")]
        public virtual Job? Job { get; set; }

        [ForeignKey("ResolvedByUserId")]
        public virtual User? ResolvedByUser { get; set; }
    }
}