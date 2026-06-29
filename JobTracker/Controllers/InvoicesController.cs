using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobTracker.Data;
using JobTracker.Models;
using JobTracker.Services;
using System.Security.Claims;
using System.Text.Json;

namespace JobTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InvoicesController : ControllerBase
    {
        private readonly JobTrackerContext _context;
        private readonly ITenantContext _tenantContext;
        private readonly IEmailService _emailService;
        private readonly ILogger<InvoicesController> _logger;

        public InvoicesController(
            JobTrackerContext context,
            ITenantContext tenantContext,
            IEmailService emailService,
            ILogger<InvoicesController> logger)
        {
            _context = context;
            _tenantContext = tenantContext;
            _emailService = emailService;
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

        private static (decimal subtotal, decimal taxAmount, decimal total) Recalculate(string lineItemsJson, decimal taxRate)
        {
            decimal subtotal = 0;
            try
            {
                var items = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(lineItemsJson);
                if (items != null)
                    foreach (var item in items)
                        if (item.TryGetValue("amount", out var amt))
                            subtotal += amt.GetDecimal();
            }
            catch { }

            var taxAmount = Math.Round(subtotal * taxRate / 100, 2);
            var total = subtotal + taxAmount;
            return (subtotal, taxAmount, total);
        }

        // GET: api/invoices
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetInvoices(
            [FromQuery] string? status = null,
            [FromQuery] string? search = null,
            [FromQuery] int? jobId = null,
            [FromQuery] int? contactId = null)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var query = _context.Invoices.Where(i => i.CompanyId == companyId);

                if (!string.IsNullOrWhiteSpace(status) && status != "all")
                {
                    if (status == "overdue")
                    {
                        var cutoff = DateTime.UtcNow;
                        query = query.Where(i => i.Status == InvoiceStatuses.Sent
                            && i.DueAt.HasValue && i.DueAt.Value < cutoff);
                    }
                    else
                    {
                        query = query.Where(i => i.Status == status);
                    }
                }
                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(i => i.InvoiceNumber.Contains(search) || (i.ClientName != null && i.ClientName.Contains(search)));
                if (jobId.HasValue)
                    query = query.Where(i => i.JobId == jobId);
                if (contactId.HasValue)
                    query = query.Where(i => i.ContactId == contactId);

                var now = DateTime.UtcNow;

                var invoices = await query
                    .OrderByDescending(i => i.CreatedAt)
                    .Select(i => new
                    {
                        i.Id, i.InvoiceNumber, i.Status, i.ClientName, i.ClientEmail,
                        i.JobId, i.ContactId, i.Subtotal, i.TaxRate, i.TaxAmount, i.Total,
                        i.AmountPaid, i.BalanceDue, i.IssuedAt, i.DueAt, i.SentAt, i.PaidAt,
                        i.CreatedAt, i.UpdatedAt,
                        JobNumber = i.Job != null ? i.Job.JobNumber : null,
                        JobName = i.Job != null ? i.Job.Name : null,
                        ContactName = i.Contact != null ? i.Contact.FullName : null,
                        PaymentCount = _context.InvoicePayments.Count(p => p.InvoiceId == i.Id),
                        IsOverdue = i.Status != InvoiceStatuses.Paid && i.Status != InvoiceStatuses.Void
                                    && i.DueAt.HasValue && i.DueAt.Value < now
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
                    .Where(i => i.CompanyId == companyId && i.Status != InvoiceStatuses.Void)
                    .ToListAsync();

                return Ok(new
                {
                    // Outstanding = sent invoices (current + overdue) + partially-paid invoices
                    TotalOutstanding = invoices
                        .Where(i => i.Status == InvoiceStatuses.Sent || i.Status == InvoiceStatuses.Partial)
                        .Sum(i => i.BalanceDue),
                    PaidThisMonth = invoices.Where(i => i.Status == InvoiceStatuses.Paid && i.UpdatedAt >= monthStart).Sum(i => i.Total),
                    OverdueCount = invoices.Count(i => i.Status == InvoiceStatuses.Sent && i.DueAt.HasValue && i.DueAt.Value < now),
                    DraftCount = invoices.Count(i => i.Status == InvoiceStatuses.Draft),
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
                    .Include(i => i.Job)
                    .Include(i => i.Contact)
                    .Include(i => i.Payments)
                    .FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == companyId);

                if (invoice == null)
                    return NotFound(new { message = "Invoice not found" });

                return Ok(new
                {
                    invoice.Id, invoice.InvoiceNumber, invoice.Status,
                    invoice.ClientName, invoice.ClientEmail, invoice.ClientAddress, invoice.ClientPhone,
                    invoice.Notes, invoice.Terms, invoice.LineItemsJson,
                    invoice.Subtotal, invoice.TaxRate, invoice.TaxAmount, invoice.Total,
                    invoice.AmountPaid, invoice.BalanceDue,
                    invoice.IssuedAt, invoice.DueAt, invoice.SentAt, invoice.PaidAt,
                    invoice.JobId, invoice.ContactId,
                    invoice.CreatedAt, invoice.UpdatedAt,
                    JobNumber = invoice.Job?.JobNumber,
                    JobName = invoice.Job?.Name,
                    ContactName = invoice.Contact?.FullName,
                    Payments = invoice.Payments.OrderByDescending(p => p.PaidAt).Select(p => new
                    {
                        p.Id, p.Amount, p.Method, p.PaidAt, p.Reference, p.Notes, p.RecordedBy, p.CreatedAt
                    })
                });
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

                var lineItemsJson = req.LineItemsJson ?? "[]";
                var (subtotal, taxAmount, total) = Recalculate(lineItemsJson, req.TaxRate);

                // Pre-fill from job or contact if provided
                string? clientName = req.ClientName;
                string? clientEmail = req.ClientEmail;
                string? clientAddress = req.ClientAddress;

                if (req.JobId.HasValue && (string.IsNullOrEmpty(clientName) || string.IsNullOrEmpty(clientEmail)))
                {
                    var job = await _context.Jobs.FindAsync(req.JobId.Value);
                    if (job != null && job.CompanyId == companyId)
                    {
                        clientName ??= job.ClientName;
                        clientEmail ??= job.ClientEmail;
                        clientAddress ??= job.Location;
                    }
                }
                else if (req.ContactId.HasValue && (string.IsNullOrEmpty(clientName) || string.IsNullOrEmpty(clientEmail)))
                {
                    var contact = await _context.Contacts.FindAsync(req.ContactId.Value);
                    if (contact != null && contact.CompanyId == companyId)
                    {
                        clientName ??= contact.FullName;
                        clientEmail ??= contact.Email;
                        clientAddress ??= contact.Address;
                    }
                }

                var invoice = new Invoice
                {
                    CompanyId = companyId,
                    InvoiceNumber = invoiceNumber,
                    JobId = req.JobId,
                    ContactId = req.ContactId,
                    ClientName = clientName?.Trim(),
                    ClientEmail = clientEmail?.Trim(),
                    ClientAddress = clientAddress?.Trim(),
                    ClientPhone = req.ClientPhone?.Trim(),
                    Notes = req.Notes?.Trim(),
                    Terms = req.Terms?.Trim(),
                    LineItemsJson = lineItemsJson,
                    Subtotal = subtotal,
                    TaxRate = req.TaxRate,
                    TaxAmount = taxAmount,
                    Total = total,
                    AmountPaid = 0,
                    BalanceDue = total,
                    Status = InvoiceStatuses.Draft,
                    IssuedAt = req.IssuedAt ?? DateTime.UtcNow,
                    DueAt = req.DueAt,
                    CreatedBy = GetCurrentUserName(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, new { invoice.Id, invoice.InvoiceNumber, invoice.Status, invoice.Total });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice");
                return StatusCode(500, new { message = "Error creating invoice" });
            }
        }

        // PUT: api/invoices/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateInvoice(int id, [FromBody] UpdateInvoiceRequest req)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == companyId);
                if (invoice == null)
                    return NotFound(new { message = "Invoice not found" });

                if (invoice.Status == InvoiceStatuses.Void)
                    return BadRequest(new { message = "Cannot edit a voided invoice" });

                var lineItemsJson = req.LineItemsJson ?? invoice.LineItemsJson;
                var taxRate = req.TaxRate ?? invoice.TaxRate;
                var (subtotal, taxAmount, total) = Recalculate(lineItemsJson, taxRate);

                invoice.ClientName = req.ClientName?.Trim() ?? invoice.ClientName;
                invoice.ClientEmail = req.ClientEmail?.Trim() ?? invoice.ClientEmail;
                invoice.ClientAddress = req.ClientAddress?.Trim() ?? invoice.ClientAddress;
                invoice.ClientPhone = req.ClientPhone?.Trim() ?? invoice.ClientPhone;
                invoice.Notes = req.Notes?.Trim() ?? invoice.Notes;
                invoice.Terms = req.Terms?.Trim() ?? invoice.Terms;
                invoice.LineItemsJson = lineItemsJson;
                invoice.Subtotal = subtotal;
                invoice.TaxRate = taxRate;
                invoice.TaxAmount = taxAmount;
                invoice.Total = total;
                invoice.BalanceDue = total - invoice.AmountPaid;
                invoice.DueAt = req.DueAt ?? invoice.DueAt;
                invoice.IssuedAt = req.IssuedAt ?? invoice.IssuedAt;
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
        public async Task<ActionResult> SendInvoice(int id)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var invoice = await _context.Invoices
                    .Include(i => i.Job)
                    .FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == companyId);

                if (invoice == null)
                    return NotFound(new { message = "Invoice not found" });
                if (invoice.Status == InvoiceStatuses.Void)
                    return BadRequest(new { message = "Cannot send a voided invoice" });
                if (string.IsNullOrEmpty(invoice.ClientEmail))
                    return BadRequest(new { message = "No client email on this invoice" });

                var subject = $"Invoice {invoice.InvoiceNumber} from PROVIA";
                var dueStr = invoice.DueAt.HasValue ? invoice.DueAt.Value.ToString("MMMM d, yyyy") : "On receipt";
                var htmlBody = $@"
<div style='font-family:Arial,sans-serif;max-width:600px;margin:auto;padding:20px;border:1px solid #eee;'>
  <div style='background:#FF9500;padding:15px 20px;border-radius:4px 4px 0 0;'>
    <h2 style='color:#fff;margin:0;'>PROVIA</h2>
  </div>
  <div style='padding:20px;'>
    <h3>Invoice {invoice.InvoiceNumber}</h3>
    <p>Dear {invoice.ClientName ?? "Valued Client"},</p>
    <p>Please find your invoice details below.</p>
    <table style='width:100%;border-collapse:collapse;margin:20px 0;'>
      <tr style='background:#f5f5f5;'>
        <th style='text-align:left;padding:8px;border:1px solid #ddd;'>Invoice #</th>
        <td style='padding:8px;border:1px solid #ddd;'>{invoice.InvoiceNumber}</td>
      </tr>
      <tr>
        <th style='text-align:left;padding:8px;border:1px solid #ddd;'>Issue Date</th>
        <td style='padding:8px;border:1px solid #ddd;'>{(invoice.IssuedAt.HasValue ? invoice.IssuedAt.Value.ToString("MMMM d, yyyy") : "")}</td>
      </tr>
      <tr style='background:#f5f5f5;'>
        <th style='text-align:left;padding:8px;border:1px solid #ddd;'>Due Date</th>
        <td style='padding:8px;border:1px solid #ddd;'>{dueStr}</td>
      </tr>
      <tr>
        <th style='text-align:left;padding:8px;border:1px solid #ddd;'>Amount Due</th>
        <td style='padding:8px;border:1px solid #ddd;font-weight:bold;font-size:1.1em;'>${invoice.BalanceDue:F2}</td>
      </tr>
    </table>
    {(!string.IsNullOrEmpty(invoice.Notes) ? $"<p><strong>Notes:</strong> {invoice.Notes}</p>" : "")}
    <p style='color:#888;font-size:0.9em;'>Please contact us if you have any questions regarding this invoice.</p>
  </div>
  <div style='background:#f5f5f5;padding:12px 20px;border-radius:0 0 4px 4px;font-size:0.85em;color:#666;'>
    <p style='margin:0;'>PROVIA Construction Management Platform</p>
  </div>
</div>";

                var plainText = $"Invoice {invoice.InvoiceNumber} — Amount Due: ${invoice.BalanceDue:F2} — Due: {dueStr}";
                await _emailService.SendHtmlEmailAsync(invoice.ClientEmail, subject, htmlBody, plainText);

                invoice.Status = InvoiceStatuses.Sent;
                invoice.SentAt = DateTime.UtcNow;
                invoice.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Invoice sent successfully", invoiceNumber = invoice.InvoiceNumber });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending invoice {Id}", id);
                return StatusCode(500, new { message = "Error sending invoice" });
            }
        }

        // POST: api/invoices/{id}/payments
        [HttpPost("{id}/payments")]
        public async Task<ActionResult> AddPayment(int id, [FromBody] AddPaymentRequest req)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == companyId);
                if (invoice == null)
                    return NotFound(new { message = "Invoice not found" });
                if (invoice.Status == InvoiceStatuses.Void)
                    return BadRequest(new { message = "Cannot add payments to a voided invoice" });
                if (req.Amount <= 0)
                    return BadRequest(new { message = "Payment amount must be greater than zero" });

                var payment = new InvoicePayment
                {
                    InvoiceId = id,
                    CompanyId = companyId,
                    Amount = req.Amount,
                    Method = req.Method ?? "check",
                    PaidAt = req.PaidAt ?? DateTime.UtcNow,
                    Reference = req.Reference?.Trim(),
                    Notes = req.Notes?.Trim(),
                    RecordedBy = GetCurrentUserName(),
                    CreatedAt = DateTime.UtcNow
                };

                _context.InvoicePayments.Add(payment);

                invoice.AmountPaid += req.Amount;
                invoice.BalanceDue = invoice.Total - invoice.AmountPaid;
                if (invoice.BalanceDue <= 0)
                {
                    invoice.BalanceDue = 0;
                    invoice.Status = InvoiceStatuses.Paid;
                    invoice.PaidAt = DateTime.UtcNow;
                }
                else
                {
                    invoice.Status = InvoiceStatuses.Partial;
                }
                invoice.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok(new { message = "Payment recorded", amountPaid = invoice.AmountPaid, balanceDue = invoice.BalanceDue, status = invoice.Status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding payment to invoice {Id}", id);
                return StatusCode(500, new { message = "Error recording payment" });
            }
        }

        // DELETE: api/invoices/{id}/payments/{paymentId}
        [HttpDelete("{id}/payments/{paymentId}")]
        public async Task<ActionResult> DeletePayment(int id, int paymentId)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == companyId);
                if (invoice == null)
                    return NotFound(new { message = "Invoice not found" });

                var payment = await _context.InvoicePayments.FirstOrDefaultAsync(p => p.Id == paymentId && p.InvoiceId == id && p.CompanyId == companyId);
                if (payment == null)
                    return NotFound(new { message = "Payment not found" });

                _context.InvoicePayments.Remove(payment);
                invoice.AmountPaid = Math.Max(0, invoice.AmountPaid - payment.Amount);
                invoice.BalanceDue = invoice.Total - invoice.AmountPaid;
                if (invoice.Status == InvoiceStatuses.Paid && invoice.BalanceDue > 0)
                    invoice.Status = invoice.AmountPaid > 0 ? InvoiceStatuses.Partial : InvoiceStatuses.Sent;
                invoice.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok(new { message = "Payment removed", amountPaid = invoice.AmountPaid, balanceDue = invoice.BalanceDue });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting payment {PaymentId}", paymentId);
                return StatusCode(500, new { message = "Error removing payment" });
            }
        }

        // POST: api/invoices/{id}/void
        [HttpPost("{id}/void")]
        public async Task<ActionResult> VoidInvoice(int id)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == companyId);
                if (invoice == null)
                    return NotFound(new { message = "Invoice not found" });
                if (invoice.Status == InvoiceStatuses.Void)
                    return BadRequest(new { message = "Invoice is already voided" });

                invoice.Status = InvoiceStatuses.Void;
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

        // GET: api/invoices/{id}/print
        [HttpGet("{id}/print")]
        public async Task<ActionResult> PrintInvoice(int id)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var invoice = await _context.Invoices
                    .Include(i => i.Job)
                    .Include(i => i.Contact)
                    .Include(i => i.Payments)
                    .FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == companyId);

                if (invoice == null)
                    return NotFound(new { message = "Invoice not found" });

                var company = await _context.Companies.FindAsync(companyId);

                var lineItems = new List<Dictionary<string, JsonElement>>();
                try { lineItems = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(invoice.LineItemsJson) ?? new(); } catch { }

                var lineItemRows = string.Join("", lineItems.Select(item =>
                {
                    var desc = item.TryGetValue("description", out var d) ? d.GetString() ?? "" : "";
                    var qty = item.TryGetValue("quantity", out var q) ? q.GetDecimal() : 0;
                    var unitPrice = item.TryGetValue("unitPrice", out var u) ? u.GetDecimal() : 0;
                    var amount = item.TryGetValue("amount", out var a) ? a.GetDecimal() : 0;
                    return $"<tr><td>{desc}</td><td style='text-align:center'>{qty}</td><td style='text-align:right'>${unitPrice:F2}</td><td style='text-align:right'>${amount:F2}</td></tr>";
                }));

                var dueStr = invoice.DueAt.HasValue ? invoice.DueAt.Value.ToString("MMMM d, yyyy") : "Upon receipt";
                var issuedStr = invoice.IssuedAt.HasValue ? invoice.IssuedAt.Value.ToString("MMMM d, yyyy") : DateTime.UtcNow.ToString("MMMM d, yyyy");

                var html = $@"<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'>
<title>Invoice {invoice.InvoiceNumber}</title>
<style>
  body {{ font-family: Arial, sans-serif; color: #333; margin: 0; padding: 30px; font-size: 14px; }}
  .header {{ display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 40px; }}
  .logo {{ font-size: 28px; font-weight: bold; color: #FF9500; }}
  .parties {{ display: flex; justify-content: space-between; margin-bottom: 30px; }}
  .party h4 {{ margin: 0 0 6px; color: #888; font-size: 12px; text-transform: uppercase; letter-spacing: 1px; }}
  .party p {{ margin: 2px 0; }}
  .meta {{ display: flex; gap: 40px; background: #f8f8f8; padding: 15px 20px; border-radius: 6px; margin-bottom: 30px; }}
  .meta div {{ flex: 1; }}
  .meta label {{ font-size: 11px; color: #888; text-transform: uppercase; letter-spacing: 1px; display: block; margin-bottom: 4px; }}
  .meta span {{ font-weight: bold; }}
  table {{ width: 100%; border-collapse: collapse; margin-bottom: 20px; }}
  thead tr {{ background: #2F5A7E; color: #fff; }}
  thead th {{ padding: 10px 12px; text-align: left; font-size: 13px; }}
  tbody tr:nth-child(even) {{ background: #f8f8f8; }}
  tbody td {{ padding: 9px 12px; border-bottom: 1px solid #eee; }}
  .totals {{ margin-left: auto; width: 300px; }}
  .totals table td {{ padding: 6px 12px; }}
  .totals .grand {{ background: #2F5A7E; color: #fff; font-weight: bold; font-size: 15px; }}
  .notes {{ margin-top: 20px; padding: 15px; background: #fffbf0; border-left: 4px solid #FF9500; border-radius: 4px; }}
  .footer {{ margin-top: 50px; text-align: center; color: #aaa; font-size: 12px; }}
  @media print {{ body {{ padding: 15px; }} button {{ display: none; }} }}
</style>
</head>
<body>
<div class='header'>
  <div>
    <div class='logo'>PROVIA</div>
    <div style='color:#888;font-size:12px;margin-top:4px;'>{company?.CompanyName ?? "PROVIA Platform"}</div>
  </div>
  <div style='text-align:right;'>
    <div style='font-size:24px;color:#2F5A7E;font-weight:bold;'>INVOICE</div>
    <div style='font-size:16px;color:#888;margin-top:4px;'>{invoice.InvoiceNumber}</div>
    <div style='margin-top:8px;'>
      <span style='background:{GetStatusColor(invoice.Status)};color:#fff;padding:4px 12px;border-radius:20px;font-size:12px;font-weight:bold;text-transform:uppercase;'>{invoice.Status}</span>
    </div>
  </div>
</div>

<div class='parties'>
  <div class='party'>
    <h4>Bill To</h4>
    <p><strong>{invoice.ClientName ?? "—"}</strong></p>
    {(string.IsNullOrEmpty(invoice.ClientEmail) ? "" : $"<p>{invoice.ClientEmail}</p>")}
    {(string.IsNullOrEmpty(invoice.ClientPhone) ? "" : $"<p>{invoice.ClientPhone}</p>")}
    {(string.IsNullOrEmpty(invoice.ClientAddress) ? "" : $"<p>{invoice.ClientAddress}</p>")}
  </div>
  {(invoice.Job != null ? $"<div class='party'><h4>Job Reference</h4><p><strong>{invoice.Job.JobNumber}</strong></p><p>{invoice.Job.Name}</p></div>" : "")}
</div>

<div class='meta'>
  <div><label>Issue Date</label><span>{issuedStr}</span></div>
  <div><label>Due Date</label><span>{dueStr}</span></div>
  <div><label>Amount Due</label><span style='color:#FF9500;font-size:16px;'>${invoice.BalanceDue:F2}</span></div>
</div>

<table>
  <thead><tr><th>Description</th><th style='text-align:center;width:80px;'>Qty</th><th style='text-align:right;width:120px;'>Unit Price</th><th style='text-align:right;width:120px;'>Amount</th></tr></thead>
  <tbody>{lineItemRows}</tbody>
</table>

<div class='totals'>
  <table>
    <tr><td>Subtotal</td><td style='text-align:right;'>${invoice.Subtotal:F2}</td></tr>
    {(invoice.TaxRate > 0 ? $"<tr><td>Tax ({invoice.TaxRate}%)</td><td style='text-align:right;'>${invoice.TaxAmount:F2}</td></tr>" : "")}
    {(invoice.AmountPaid > 0 ? $"<tr><td>Amount Paid</td><td style='text-align:right;color:green;'>-${invoice.AmountPaid:F2}</td></tr>" : "")}
    <tr class='grand'><td>Balance Due</td><td style='text-align:right;'>${invoice.BalanceDue:F2}</td></tr>
  </table>
</div>

{(string.IsNullOrEmpty(invoice.Notes) ? "" : $"<div class='notes'><strong>Notes:</strong> {invoice.Notes}</div>")}
{(string.IsNullOrEmpty(invoice.Terms) ? "" : $"<div class='notes' style='margin-top:10px;'><strong>Terms:</strong> {invoice.Terms}</div>")}

<div class='footer'>
  <p>Thank you for your business · PROVIA Construction Management Platform</p>
</div>

<script>window.onload = function() {{ window.print(); }}</script>
</body>
</html>";

                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating print view for invoice {Id}", id);
                return StatusCode(500, new { message = "Error generating print view" });
            }
        }

        private static string GetStatusColor(string status) => status switch
        {
            "paid" => "#198754",
            "partial" => "#fd7e14",
            "sent" => "#0d6efd",
            "overdue" => "#dc3545",
            "void" => "#6c757d",
            _ => "#aaa"
        };

        // DELETE: api/invoices/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteInvoice(int id)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == companyId);
                if (invoice == null)
                    return NotFound(new { message = "Invoice not found" });
                if (invoice.Status != InvoiceStatuses.Draft && invoice.Status != InvoiceStatuses.Void)
                    return BadRequest(new { message = "Only draft or voided invoices can be deleted" });

                _context.Invoices.Remove(invoice);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Invoice deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting invoice {Id}", id);
                return StatusCode(500, new { message = "Error deleting invoice" });
            }
        }
    }

    // Request models
    public class CreateInvoiceRequest
    {
        public int? JobId { get; set; }
        public int? ContactId { get; set; }
        public string? ClientName { get; set; }
        public string? ClientEmail { get; set; }
        public string? ClientAddress { get; set; }
        public string? ClientPhone { get; set; }
        public string? Notes { get; set; }
        public string? Terms { get; set; }
        public string? LineItemsJson { get; set; }
        public decimal TaxRate { get; set; } = 0;
        public DateTime? IssuedAt { get; set; }
        public DateTime? DueAt { get; set; }
    }

    public class UpdateInvoiceRequest
    {
        public string? ClientName { get; set; }
        public string? ClientEmail { get; set; }
        public string? ClientAddress { get; set; }
        public string? ClientPhone { get; set; }
        public string? Notes { get; set; }
        public string? Terms { get; set; }
        public string? LineItemsJson { get; set; }
        public decimal? TaxRate { get; set; }
        public DateTime? IssuedAt { get; set; }
        public DateTime? DueAt { get; set; }
    }

    public class AddPaymentRequest
    {
        public decimal Amount { get; set; }
        public string? Method { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? Reference { get; set; }
        public string? Notes { get; set; }
    }
}
