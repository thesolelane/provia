using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobTracker.Data;
using JobTracker.Models;
using JobTracker.Services;
using System.Security.Claims;

namespace JobTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InvoicesController : ControllerBase
    {
        private readonly JobTrackerContext _context;
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<InvoicesController> _logger;

        public InvoicesController(JobTrackerContext context, ITenantContext tenantContext, ILogger<InvoicesController> logger)
        {
            _context = context;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        private string GetCurrentUserName() =>
            User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst(ClaimTypes.Name)?.Value ?? "System";

        private async Task<string> GenerateInvoiceNumberAsync(int companyId)
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"INV-{year}-";
            var last = await _context.Invoices
                .Where(i => i.CompanyId == companyId && i.InvoiceNumber.StartsWith(prefix))
                .OrderByDescending(i => i.InvoiceNumber)
                .Select(i => i.InvoiceNumber)
                .FirstOrDefaultAsync();

            int next = 1;
            if (last != null)
            {
                var parts = last.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastNum))
                    next = lastNum + 1;
            }
            return $"{prefix}{next:D4}";
        }

        private static (decimal subtotal, decimal taxAmount, decimal total, decimal balance) CalcTotals(
            IEnumerable<InvoiceLineItem> items, decimal taxRate, decimal amountPaid)
        {
            var subtotal = items.Sum(li => li.Total);
            var taxAmount = Math.Round(subtotal * taxRate, 2);
            var total = subtotal + taxAmount;
            var balance = total - amountPaid;
            return (subtotal, taxAmount, total, balance);
        }

        // GET: api/invoices
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetInvoices([FromQuery] string? status, [FromQuery] string? search)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var query = _context.Invoices.Where(i => i.CompanyId == companyId);

                if (!string.IsNullOrWhiteSpace(status) && status != "all")
                    query = query.Where(i => i.Status == status);

                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(i => i.InvoiceNumber.Contains(search) || (i.ClientName != null && i.ClientName.Contains(search)));

                var invoices = await query
                    .OrderByDescending(i => i.InvoiceDate)
                    .Select(i => new
                    {
                        i.Id, i.InvoiceNumber, i.ClientName, i.ClientEmail,
                        i.Status, i.InvoiceDate, i.DueDate,
                        i.Subtotal, i.TaxAmount, i.Total, i.AmountPaid, i.BalanceDue,
                        i.JobId, i.ContactId, i.CreatedAt,
                        LineItemCount = _context.InvoiceLineItems.Count(li => li.InvoiceId == i.Id)
                    })
                    .ToListAsync();

                return Ok(invoices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching invoices");
                return StatusCode(500, new { message = "Error fetching invoices" });
            }
        }

        // GET: api/invoices/summary
        [HttpGet("summary")]
        public async Task<ActionResult<object>> GetSummary()
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var now = DateTime.UtcNow;
                var monthStart = new DateTime(now.Year, now.Month, 1);

                var invoices = await _context.Invoices
                    .Where(i => i.CompanyId == companyId && i.Status != "void")
                    .ToListAsync();

                return Ok(new
                {
                    TotalOutstanding = invoices.Where(i => i.Status != "paid").Sum(i => i.BalanceDue),
                    PaidThisMonth = invoices.Where(i => i.Status == "paid" && i.UpdatedAt >= monthStart).Sum(i => i.Total),
                    OverdueCount = invoices.Count(i => i.Status != "paid" && i.Status != "void" && i.DueDate < now),
                    DraftCount = invoices.Count(i => i.Status == "draft"),
                    TotalInvoiced = invoices.Sum(i => i.Total)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching invoice summary");
                return StatusCode(500, new { message = "Error fetching summary" });
            }
        }

        // GET: api/invoices/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetInvoice(int id)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var invoice = await _context.Invoices
                    .Include(i => i.LineItems.OrderBy(li => li.SortOrder))
                    .Include(i => i.Payments.OrderByDescending(p => p.PaidAt))
                    .Where(i => i.Id == id && i.CompanyId == companyId)
                    .FirstOrDefaultAsync();

                if (invoice == null) return NotFound(new { message = "Invoice not found" });
                return Ok(invoice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching invoice {Id}", id);
                return StatusCode(500, new { message = "Error fetching invoice" });
            }
        }

        // POST: api/invoices
        [HttpPost]
        public async Task<ActionResult<object>> CreateInvoice([FromBody] CreateInvoiceRequest req)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var invoiceNumber = await GenerateInvoiceNumberAsync(companyId);

                var lineItems = (req.LineItems ?? new List<LineItemRequest>())
                    .Select((li, idx) => new InvoiceLineItem
                    {
                        Description = li.Description.Trim(),
                        Quantity = li.Quantity,
                        UnitPrice = li.UnitPrice,
                        Total = Math.Round(li.Quantity * li.UnitPrice, 2),
                        SortOrder = idx
                    }).ToList();

                var taxRate = req.TaxRate / 100m; // convert percent to decimal
                var (subtotal, taxAmount, total, balance) = CalcTotals(lineItems, taxRate, 0);

                var invoice = new Invoice
                {
                    CompanyId = companyId,
                    InvoiceNumber = invoiceNumber,
                    JobId = req.JobId,
                    ContactId = req.ContactId,
                    ClientName = req.ClientName?.Trim(),
                    ClientAddress = req.ClientAddress?.Trim(),
                    ClientEmail = req.ClientEmail?.Trim(),
                    ClientPhone = req.ClientPhone?.Trim(),
                    Status = "draft",
                    InvoiceDate = req.InvoiceDate ?? DateTime.UtcNow,
                    DueDate = req.DueDate ?? DateTime.UtcNow.AddDays(30),
                    TaxRate = taxRate,
                    Notes = req.Notes?.Trim(),
                    Terms = req.Terms?.Trim(),
                    Subtotal = subtotal,
                    TaxAmount = taxAmount,
                    Total = total,
                    AmountPaid = 0,
                    BalanceDue = balance,
                    LineItems = lineItems,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id },
                    new { invoice.Id, invoice.InvoiceNumber, invoice.Total });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice");
                return StatusCode(500, new { message = "Error creating invoice" });
            }
        }

        // PUT: api/invoices/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateInvoice(int id, [FromBody] CreateInvoiceRequest req)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var invoice = await _context.Invoices
                    .Include(i => i.LineItems)
                    .FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == companyId);

                if (invoice == null) return NotFound(new { message = "Invoice not found" });
                if (invoice.Status != "draft") return BadRequest(new { message = "Only draft invoices can be edited" });

                // Replace line items
                _context.InvoiceLineItems.RemoveRange(invoice.LineItems);

                var lineItems = (req.LineItems ?? new List<LineItemRequest>())
                    .Select((li, idx) => new InvoiceLineItem
                    {
                        InvoiceId = id,
                        Description = li.Description.Trim(),
                        Quantity = li.Quantity,
                        UnitPrice = li.UnitPrice,
                        Total = Math.Round(li.Quantity * li.UnitPrice, 2),
                        SortOrder = idx
                    }).ToList();

                var taxRate = req.TaxRate / 100m;
                var (subtotal, taxAmount, total, balance) = CalcTotals(lineItems, taxRate, invoice.AmountPaid);

                invoice.ClientName = req.ClientName?.Trim() ?? invoice.ClientName;
                invoice.ClientAddress = req.ClientAddress?.Trim() ?? invoice.ClientAddress;
                invoice.ClientEmail = req.ClientEmail?.Trim() ?? invoice.ClientEmail;
                invoice.ClientPhone = req.ClientPhone?.Trim() ?? invoice.ClientPhone;
                invoice.InvoiceDate = req.InvoiceDate ?? invoice.InvoiceDate;
                invoice.DueDate = req.DueDate ?? invoice.DueDate;
                invoice.TaxRate = taxRate;
                invoice.Notes = req.Notes?.Trim();
                invoice.Terms = req.Terms?.Trim();
                invoice.Subtotal = subtotal;
                invoice.TaxAmount = taxAmount;
                invoice.Total = total;
                invoice.BalanceDue = balance;
                invoice.LineItems = lineItems;
                invoice.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating invoice {Id}", id);
                return StatusCode(500, new { message = "Error updating invoice" });
            }
        }

        // POST: api/invoices/{id}/send
        [HttpPost("{id}/send")]
        public async Task<IActionResult> SendInvoice(int id)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == companyId);
                if (invoice == null) return NotFound(new { message = "Invoice not found" });
                if (invoice.Status == "void") return BadRequest(new { message = "Cannot send a voided invoice" });

                invoice.Status = "sent";
                invoice.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Invoice marked as sent" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending invoice {Id}", id);
                return StatusCode(500, new { message = "Error sending invoice" });
            }
        }

        // POST: api/invoices/{id}/payments
        [HttpPost("{id}/payments")]
        public async Task<IActionResult> RecordPayment(int id, [FromBody] RecordPaymentRequest req)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var invoice = await _context.Invoices
                    .Include(i => i.Payments)
                    .FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == companyId);

                if (invoice == null) return NotFound(new { message = "Invoice not found" });
                if (invoice.Status == "void") return BadRequest(new { message = "Cannot record payment on voided invoice" });
                if (req.Amount <= 0) return BadRequest(new { message = "Payment amount must be positive" });

                _context.InvoicePayments.Add(new InvoicePayment
                {
                    InvoiceId = id,
                    CompanyId = companyId,
                    Amount = req.Amount,
                    PaidAt = req.PaidAt ?? DateTime.UtcNow,
                    Method = req.Method ?? "check",
                    Reference = req.Reference?.Trim(),
                    Notes = req.Notes?.Trim(),
                    RecordedBy = GetCurrentUserName(),
                    CreatedAt = DateTime.UtcNow
                });

                invoice.AmountPaid += req.Amount;
                invoice.BalanceDue = invoice.Total - invoice.AmountPaid;
                invoice.Status = invoice.BalanceDue <= 0 ? "paid" : "partial";
                invoice.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok(new { message = "Payment recorded", balanceDue = invoice.BalanceDue, status = invoice.Status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording payment for invoice {Id}", id);
                return StatusCode(500, new { message = "Error recording payment" });
            }
        }

        // POST: api/invoices/{id}/void
        [HttpPost("{id}/void")]
        public async Task<IActionResult> VoidInvoice(int id)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == companyId);
                if (invoice == null) return NotFound(new { message = "Invoice not found" });
                if (invoice.Status == "paid") return BadRequest(new { message = "Cannot void a paid invoice" });

                invoice.Status = "void";
                invoice.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Invoice voided" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error voiding invoice {Id}", id);
                return StatusCode(500, new { message = "Error voiding invoice" });
            }
        }
    }

    public class LineItemRequest
    {
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
    }

    public class CreateInvoiceRequest
    {
        public int? JobId { get; set; }
        public int? ContactId { get; set; }
        public string? ClientName { get; set; }
        public string? ClientAddress { get; set; }
        public string? ClientEmail { get; set; }
        public string? ClientPhone { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal TaxRate { get; set; } = 0; // percent, e.g. 6.25
        public string? Notes { get; set; }
        public string? Terms { get; set; }
        public List<LineItemRequest>? LineItems { get; set; }
    }

    public class RecordPaymentRequest
    {
        public decimal Amount { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? Method { get; set; }
        public string? Reference { get; set; }
        public string? Notes { get; set; }
    }
}
