using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTracker.Models
{
    public class LocationVerificationRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int LocationTrackerId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public DateTime RequestedAt { get; set; }

        [Required]
        public DateTime ExpectedResponseTime { get; set; }

        public DateTime? RespondedAt { get; set; }

        [Required]
        public VerificationStatus Status { get; set; }

        [Required]
        public LocationTrackerStatus RequiredStatus { get; set; }

        public double? ResponseLatitude { get; set; }

        public double? ResponseLongitude { get; set; }

        public double? ResponseAccuracy { get; set; }

        public double? DistanceFromRequired { get; set; }

        public bool? IsWithinGeofence { get; set; }

        public string? Notes { get; set; }

        // Navigation properties
        [ForeignKey("LocationTrackerId")]
        public virtual LocationTracker LocationTracker { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }

    public enum VerificationStatus
    {
        Pending = 1,
        Completed = 2,
        Failed = 3,
        Expired = 4
    }
}