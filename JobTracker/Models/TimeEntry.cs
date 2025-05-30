using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTracker.Models
{
    public class TimeEntry
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        public int JobId { get; set; }
        public Job Job { get; set; } = null!;

        [Required]
        public DateTime ClockInTime { get; set; }

        public DateTime? ClockOutTime { get; set; }

        // GPS coordinates for clock-in location
        public double? ClockInLatitude { get; set; }
        public double? ClockInLongitude { get; set; }

        // GPS coordinates for clock-out location
        public double? ClockOutLatitude { get; set; }
        public double? ClockOutLongitude { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? TotalHours { get; set; }

        public bool IsActive { get; set; } = true;

        // Indicates if location was verified during clock-in/out
        public bool LocationVerified { get; set; } = false;

        // Two-stage verification system
        public bool IsPendingVerification { get; set; } = false;
        public bool VerificationPending { get; set; } = false;
        public DateTime? VerificationDeadline { get; set; }
        public bool VerificationFailed { get; set; } = false;
        public DateTime? AutoLogoutTime { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    public class ClockInRequest
    {
        public int JobId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsLocationVerified { get; set; }
    }

    public class VerifyLocationRequest
    {
        public int TimeEntryId { get; set; }
        public int JobId { get; set; }
    }
}