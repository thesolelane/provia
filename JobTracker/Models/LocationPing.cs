using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTracker.Models
{
    public class LocationPing
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int LocationTrackerId { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        [Required]
        public double Accuracy { get; set; }

        public double DistanceFromJobSite { get; set; }

        [Required]
        public DateTime PingTime { get; set; }

        [Required]
        public LocationTrackerStatus Status { get; set; }

        public bool IsViolation { get; set; }

        public string? Notes { get; set; }

        // Navigation properties
        [ForeignKey("LocationTrackerId")]
        public virtual LocationTracker LocationTracker { get; set; }
    }
}