using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTracker.Models
{
    public class LocationTracker
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TimeEntryId { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public int JobId { get; set; }

        [Required]
        public double InitialLatitude { get; set; }
        
        [Required]
        public double InitialLongitude { get; set; }
        
        public double CurrentLatitude { get; set; }
        
        public double CurrentLongitude { get; set; }
        
        [Required]
        public LocationTrackerStatus Status { get; set; }
        
        public DateTime? StatusChangedAt { get; set; }
        
        public DateTime NextLocationCheckAt { get; set; }
        
        public DateTime? MaterialRunStartedAt { get; set; }
        
        public DateTime? LastLocationUpdate { get; set; }
        
        public DateTime? LastViolationAt { get; set; }
        
        public int ConsecutiveViolations { get; set; } = 0;
        
        public double? DistanceFromJobSite { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime? AutoLoggedOutAt { get; set; }
        
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        [ForeignKey("TimeEntryId")]
        public virtual TimeEntry TimeEntry { get; set; }
        
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
        
        [ForeignKey("JobId")]
        public virtual Job Job { get; set; }
        
        public virtual ICollection<LocationPing> LocationPings { get; set; } = new List<LocationPing>();
        
        public virtual ICollection<LocationVerificationRequest> VerificationRequests { get; set; } = new List<LocationVerificationRequest>();
    }

    public enum LocationTrackerStatus
    {
        ClockedIn = 1,
        LunchBreak = 2,
        MaterialRun = 3,
        ClockedOut = 4
    }
}