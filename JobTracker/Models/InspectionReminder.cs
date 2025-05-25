using System.ComponentModel.DataAnnotations;

namespace JobTracker.Models
{
    public class InspectionReminder
    {
        public int Id { get; set; }
        
        [Required]
        public int JobSectionId { get; set; }
        public JobSection JobSection { get; set; } = null!;
        
        [Required]
        [StringLength(50)]
        public string InspectionType { get; set; } = string.Empty; // "Building", "Electrical", "Plumbing"
        
        [Required]
        [StringLength(100)]
        public string SectionName { get; set; } = string.Empty; // "Framing", "Electrical Rough-in", etc.
        
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        public DateTime? DueDate { get; set; }
        
        public bool IsCompleted { get; set; } = false;
        
        public DateTime? CompletedDate { get; set; }
        
        public bool IsUrgent { get; set; } = false; // Mark as urgent after 3+ days
        
        [StringLength(20)]
        public string Priority { get; set; } = "Normal"; // "Normal", "High", "Urgent"
    }
}