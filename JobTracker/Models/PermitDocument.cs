using System;
using System.ComponentModel.DataAnnotations;

namespace JobTracker.Models
{
    public class PermitFormTemplate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string PermitType { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string TemplateName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? Jurisdiction { get; set; }

        [StringLength(50)]
        public string? Version { get; set; }

        [Required]
        public string StoragePath { get; set; } = string.Empty;

        public string? FilePath { get; set; }

        [StringLength(255)]
        public string? OriginalFileName { get; set; }

        public long? FileSize { get; set; }

        public string? FieldSchema { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedByUserId { get; set; }
    }

    public class PropertyProfile
    {
        [Key]
        public int Id { get; set; }

        public int? JobId { get; set; }
        public Job? Job { get; set; }

        public int CompanyId { get; set; }

        [StringLength(200)]
        public string? PropertyAddress { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(10)]
        public string? ZipCode { get; set; }

        [StringLength(50)]
        public string? ParcelId { get; set; }

        [StringLength(50)]
        public string? MapLot { get; set; }

        [StringLength(200)]
        public string? OwnerName { get; set; }

        [StringLength(500)]
        public string? OwnerAddress { get; set; }

        [StringLength(50)]
        public string? ZoningCode { get; set; }

        [StringLength(100)]
        public string? ZoningDescription { get; set; }

        public decimal? LotAreaSqFt { get; set; }
        public decimal? BuildingSqFt { get; set; }
        public decimal? LandValue { get; set; }
        public decimal? BuildingValue { get; set; }
        public decimal? TotalAssessedValue { get; set; }

        [StringLength(50)]
        public string? UseCode { get; set; }

        [StringLength(100)]
        public string? UseDescription { get; set; }

        public int? YearBuilt { get; set; }
        public int? FiscalYear { get; set; }

        public string? GisDataJson { get; set; }

        public DateTime? GisFetchedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class PermitDocument
    {
        [Key]
        public int Id { get; set; }

        public int JobPermitId { get; set; }
        public JobPermit? JobPermit { get; set; }

        public int TemplateId { get; set; }
        public PermitFormTemplate? Template { get; set; }

        public int CompanyId { get; set; }

        [Required]
        public string StoragePath { get; set; } = string.Empty;

        [StringLength(100)]
        public string? FileHash { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "GENERATED";

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public int? GeneratedByUserId { get; set; }

        public string? FieldDataJson { get; set; }
    }

    public class FormFieldMapping
    {
        [Key]
        public int Id { get; set; }

        public int TemplateId { get; set; }
        public PermitFormTemplate? Template { get; set; }

        [Required]
        [StringLength(100)]
        public string PdfFieldName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? DataSource { get; set; }

        [StringLength(200)]
        public string? DataPath { get; set; }

        [StringLength(50)]
        public string FieldType { get; set; } = "TEXT";

        [StringLength(500)]
        public string? DefaultValue { get; set; }

        [StringLength(200)]
        public string? FormatExpression { get; set; }

        public bool IsRequired { get; set; } = false;

        public int DisplayOrder { get; set; } = 0;
    }

    public class MassGisPropertyData
    {
        public string? LocId { get; set; }
        public string? Town { get; set; }
        public string? SiteAddress { get; set; }
        public string? Owner { get; set; }
        public string? OwnerAddress { get; set; }
        public string? MapPar { get; set; }
        public string? UseCode { get; set; }
        public string? UseDescription { get; set; }
        public decimal? LotSize { get; set; }
        public decimal? LandValue { get; set; }
        public decimal? BuildingValue { get; set; }
        public decimal? TotalValue { get; set; }
        public int? YearBuilt { get; set; }
        public int? FiscalYear { get; set; }
        public string? ZoningCode { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class PermitFormFieldDto
    {
        public string FieldName { get; set; } = string.Empty;
        public string? Value { get; set; }
        public string DataSource { get; set; } = string.Empty;
        public bool IsAutoFilled { get; set; }
        public bool NeedsReview { get; set; }
    }

    public class GeneratePermitPdfRequest
    {
        public int JobPermitId { get; set; }
        public int TemplateId { get; set; }
        public Dictionary<string, string>? FieldOverrides { get; set; }
    }

    public class DocumentRequirement
    {
        [Key]
        public int Id { get; set; }

        public int? TemplateId { get; set; }
        public PermitFormTemplate? Template { get; set; }

        [StringLength(50)]
        public string? ScopeItemCode { get; set; }

        [StringLength(50)]
        public string? PermitType { get; set; }

        [Required]
        [StringLength(50)]
        public string DocumentPhase { get; set; } = string.Empty; // EXISTING, PROPOSED, BOTH, COMPLETION

        [StringLength(50)]
        public string? DocumentFormat { get; set; } // PHOTO, DRAWING, FORM, CERTIFICATE

        public bool IsRequired { get; set; } = true;

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public static class DocumentPhases
    {
        public const string Existing = "EXISTING";
        public const string Proposed = "PROPOSED";
        public const string Both = "BOTH";
        public const string Completion = "COMPLETION";
    }

    public static class DocumentFormats
    {
        public const string Photo = "PHOTO";
        public const string Drawing = "DRAWING";
        public const string Form = "FORM";
        public const string Certificate = "CERTIFICATE";
    }
}
