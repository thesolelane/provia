using System.ComponentModel.DataAnnotations;

namespace JobTracker.Models
{
    public class DeleteSectionRequest
    {
        [StringLength(255)]
        public string? DeletedBy { get; set; }
        
        [StringLength(500)]
        public string? Reason { get; set; }
    }
}