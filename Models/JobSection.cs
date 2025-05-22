using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTracker.Models
{
    public class JobSection
    {
        [Key]
        public int SectionId { get; set; }

        [Required]
        public int JobId { get; set; }

        [Required]
        public SectionType Type { get; set; }

        [Required]
        public SectionStatus Status { get; set; }

        [Required]
        public bool IsSubcontracted { get; set; }

        public int? SubcontractorId { get; set; }

        [MaxLength(100)]
        public string ContractReference { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? ExpectedCompletionDate { get; set; }

        public DateTime? ActualCompletionDate { get; set; }

        public DateTime? InspectionDate { get; set; }

        public DateTime? ReinspectionDate { get; set; }

        [MaxLength(50)]
        public string InspectionResult { get; set; }

        [MaxLength(50)]
        public string PermitNumber { get; set; }

        [MaxLength(200)]
        public string ResponsibleParty { get; set; }

        [MaxLength(500)]
        public string MaterialsNeeded { get; set; }

        [MaxLength(500)]
        public string SpecialRequirements { get; set; }

        [MaxLength(1000)]
        public string Notes { get; set; }

        // Navigation properties
        [ForeignKey("JobId")]
        public virtual Job Job { get; set; }

        [ForeignKey("SubcontractorId")]
        public virtual Subcontractor Subcontractor { get; set; }

        // Auditing
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
    }

    public enum SectionType
    {
        Permit,
        Demolition,
        RoughPlumbing,
        RoughElectrical,
        FramingMechanical,
        Insulation,
        Sheetrock,
        PaintPrep,
        FinishInstall,
        FinishPaintFlooring,
        KitchenBathFixtures,
        Miscellaneous
    }

    public enum SectionStatus
    {
        NotStarted,
        InProgress,
        Completed,
        Failed,
        OnHold,
        WaitingForInspection,
        WaitingForMaterials
    }
}
