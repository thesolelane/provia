using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTracker.Models
{
    public class Vendor
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CompanyId { get; set; }
        public Company? Company { get; set; }

        [Required]
        [StringLength(50)]
        public string VendorNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Category { get; set; } // lumber, electrical, plumbing, tools, concrete, general, etc.

        [StringLength(200)]
        public string? ContactName { get; set; }

        [StringLength(200)]
        public string? Email { get; set; }

        [StringLength(30)]
        public string? Phone { get; set; }

        [StringLength(300)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(50)]
        public string? State { get; set; }

        [StringLength(20)]
        public string? ZipCode { get; set; }

        [StringLength(200)]
        public string? Website { get; set; }

        [StringLength(100)]
        public string? AccountNumber { get; set; }

        [StringLength(30)]
        public string? PaymentTerms { get; set; } // net30, net60, cod, prepaid

        [Column(TypeName = "decimal(12,2)")]
        public decimal? CreditLimit { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsPreferred { get; set; } = false;

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<VendorPurchase> Purchases { get; set; } = new List<VendorPurchase>();
    }

    public class VendorPurchase
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int VendorId { get; set; }
        public Vendor? Vendor { get; set; }

        [Required]
        public int CompanyId { get; set; }

        public int? JobId { get; set; }
        public Job? Job { get; set; }

        [StringLength(100)]
        public string? PurchaseOrderNumber { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal Amount { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "pending"; // pending | ordered | received | invoiced | paid

        public DateTime? OrderedAt { get; set; }
        public DateTime? ReceivedAt { get; set; }

        [StringLength(100)]
        public string? RecordedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public static class VendorCategories
    {
        public const string Lumber       = "lumber";
        public const string Electrical   = "electrical";
        public const string Plumbing     = "plumbing";
        public const string Concrete     = "concrete";
        public const string Roofing      = "roofing";
        public const string Tools        = "tools";
        public const string Safety       = "safety";
        public const string General      = "general";
        public const string Equipment    = "equipment";
        public const string Finishes     = "finishes";

        public static readonly string[] All = {
            Lumber, Electrical, Plumbing, Concrete, Roofing,
            Tools, Safety, Equipment, Finishes, General
        };
    }
}
