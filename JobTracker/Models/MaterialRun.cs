using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTracker.Models
{
    public class MaterialRun
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        [Required]
        public int UserId { get; set; }

        [Required]
        public int JobId { get; set; }

        [Required]
        public int TimeEntryId { get; set; }

        [Required]
        [StringLength(100)]
        public string StoreType { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Materials { get; set; } = string.Empty;

        [Required]
        public double StartLatitude { get; set; }

        [Required]
        public double StartLongitude { get; set; }

        public double? EndLatitude { get; set; }

        public double? EndLongitude { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public bool IsActive { get; set; } = true;

        public bool LocationVerified { get; set; } = false;

        public bool VerificationFailed { get; set; } = false;

        public DateTime? VerificationDeadline { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("JobId")]
        public virtual Job Job { get; set; } = null!;

        [ForeignKey("TimeEntryId")]
        public virtual TimeEntry TimeEntry { get; set; } = null!;
    }
}