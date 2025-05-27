using System.ComponentModel.DataAnnotations;

namespace JobTracker.Models
{
    public class PendingClockIn
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        public int JobId { get; set; }
        public Job Job { get; set; } = null!;

        // Initial GPS location when clock-in was initiated
        [Required]
        public double InitialLatitude { get; set; }

        [Required]
        public double InitialLongitude { get; set; }

        // Timestamps for the verification process
        [Required]
        public DateTime InitiatedAt { get; set; }

        [Required]
        public DateTime ExpiresAt { get; set; }

        // Status tracking
        public bool IsActive { get; set; } = true;

        // Reference to completed time entry if successful
        public int? CompletedTimeEntryId { get; set; }
        public TimeEntry? CompletedTimeEntry { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}