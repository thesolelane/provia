using System.ComponentModel.DataAnnotations;

namespace JobTracker.Models
{
    public class LunchBreak
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        public int JobId { get; set; }
        public Job Job { get; set; } = null!;

        [Required]
        public int TimeEntryId { get; set; }
        public TimeEntry TimeEntry { get; set; } = null!;

        // GPS coordinates for lunch break start location
        [Required]
        public double StartLatitude { get; set; }

        [Required]
        public double StartLongitude { get; set; }

        // GPS coordinates for lunch break end location
        public double? EndLatitude { get; set; }
        public double? EndLongitude { get; set; }

        // Timing
        [Required]
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public DateTime VerificationDeadline { get; set; }

        // Status tracking
        public bool IsActive { get; set; } = true;
        public bool LocationVerified { get; set; } = false;
        public bool VerificationFailed { get; set; } = false;

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}