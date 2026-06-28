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

        [StringLength(200)]
        public string? ClientEmail { get; set; }

        [StringLength(300)]
        public string? ClientAddress { get; set; }

        [StringLength(30)]
        public string? ClientPhone { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }

        [StringLength(2000)]
        public string? Terms { get; set; }

        // Line items stored as JSON array: [{description, quantity, unitPrice, amount}]
        public string LineItemsJson { get; set; } = "[]";

        [Column(TypeName = "decimal(12,2)")]
        public decimal Subtotal { get; set; } = 0;

        // TaxRate stored as percentage (e.g. 6.25 = 6.25%)
        [Column(TypeName = "decimal(5,2)")]
        public decimal TaxRate { get; set; } = 0;

        [Column(TypeName = "decimal(12,2)")]
        public decimal TaxAmount { get; set; } = 0;

        [Column(TypeName = "decimal(12,2)")]
        public decimal Total { get; set; } = 0;

        [Column(TypeName = "decimal(12,2)")]
        public decimal AmountPaid { get; set; } = 0;

        [Column(TypeName = "decimal(12,2)")]
        public decimal BalanceDue { get; set; } = 0;

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "draft"; // draft | sent | partial | paid | overdue | void

        public DateTime? IssuedAt { get; set; }
        public DateTime? DueAt { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime? PaidAt { get; set; }

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<InvoicePayment> Payments { get; set; } = new List<InvoicePayment>();
    }

    public static class InvoiceStatuses
    {
        public const string Draft = "draft";
        public const string Sent = "sent";
        public const string Partial = "partial";
        public const string Paid = "paid";
        public const string Overdue = "overdue";
        public const string Void = "void";
    }
}
