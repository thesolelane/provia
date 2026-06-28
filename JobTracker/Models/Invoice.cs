using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTracker.Models
{
    public class Invoice
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CompanyId { get; set; }
        public Company? Company { get; set; }

        [Required]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        public int? JobId { get; set; }
        public Job? Job { get; set; }

        public int? ContactId { get; set; }
        public Contact? Contact { get; set; }

        // Billed-to info (snapshot at time of invoice creation)
        [StringLength(200)]
        public string? ClientName { get; set; }

        [StringLength(300)]
        public string? ClientAddress { get; set; }

        [StringLength(200)]
        public string? ClientEmail { get; set; }

        [StringLength(30)]
        public string? ClientPhone { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "draft"; // draft/sent/partial/paid/overdue/void

        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
        public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(30);

        [Column(TypeName = "decimal(12,2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(5,4)")]
        public decimal TaxRate { get; set; } = 0; // e.g. 0.0625 = 6.25%

        [Column(TypeName = "decimal(12,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal Total { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal AmountPaid { get; set; } = 0;

        [Column(TypeName = "decimal(12,2)")]
        public decimal BalanceDue { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }

        [StringLength(2000)]
        public string? Terms { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();
        public ICollection<InvoicePayment> Payments { get; set; } = new List<InvoicePayment>();
    }

    public class InvoiceLineItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int InvoiceId { get; set; }
        public Invoice? Invoice { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal Quantity { get; set; } = 1;

        [Column(TypeName = "decimal(12,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal Total { get; set; }

        public int SortOrder { get; set; } = 0;
    }

    public class InvoicePayment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int InvoiceId { get; set; }
        public Invoice? Invoice { get; set; }

        [Required]
        public int CompanyId { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal Amount { get; set; }

        public DateTime PaidAt { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string Method { get; set; } = "check"; // check/cash/card/bank_transfer/other

        [StringLength(200)]
        public string? Reference { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        [StringLength(200)]
        public string? RecordedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
