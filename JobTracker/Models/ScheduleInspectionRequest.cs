using System.ComponentModel.DataAnnotations;

namespace JobTracker.Models
{
    public class ScheduleInspectionRequest
    {
        [Required]
        public string InspectionType { get; set; } = string.Empty;
        
        [Required]
        public DateTime InspectionDate { get; set; }
        
        public string? Notes { get; set; }
    }

    public class InspectionResultRequest
    {
        [Required]
        public string InspectionType { get; set; } = string.Empty;
        
        [Required]
        public bool Passed { get; set; }
        
        public DateTime? InspectionDate { get; set; }
        
        public string? Notes { get; set; }
    }
}