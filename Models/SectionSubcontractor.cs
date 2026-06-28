using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTrackerApp.Models
{
    public class SectionSubcontractor
    {
        public int Id { get; set; }

        [Required]
        public int SectionId { get; set; }
        
        [ForeignKey("SectionId")]
        public JobSection? Section { get; set; }

        [Required]
        public int SubcontractorId { get; set; }
        
        [ForeignKey("SubcontractorId")]
        public Subcontractor? Subcontractor { get; set; }

        [StringLength(100)]
        public string? ContractReference { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? ExpectedCompletionDate { get; set; }

        public DateTime? ActualCompletionDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ContractAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
    }
}
