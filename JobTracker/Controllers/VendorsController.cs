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
    public class VendorsController : ControllerBase
    {
        private readonly JobTrackerContext _context;
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<VendorsController> _logger;

        public VendorsController(JobTrackerContext context, ITenantContext tenantContext, ILogger<VendorsController> logger)
        {
            _context = context;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        private string GetCurrentUserName() =>
            User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst(ClaimTypes.Name)?.Value ?? "System";

        private async Task<string> GenerateVendorNumberAsync(int companyId)
        {
            var count = await _context.Vendors.CountAsync(v => v.CompanyId == companyId);
            return $"VEN-{(count + 1):D4}";
        }

        // GET: api/vendors
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetVendors(
            [FromQuery] string? category = null,
            [FromQuery] string? search = null,
            [FromQuery] bool? activeOnly = null)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var query = _context.Vendors.Where(v => v.CompanyId == companyId);

                if (activeOnly == true)
                    query = query.Where(v => v.IsActive);
                if (!string.IsNullOrWhiteSpace(category) && category != "all")
                    query = query.Where(v => v.Category == category);
                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(v => v.Name.Contains(search) ||
                        (v.ContactName != null && v.ContactName.Contains(search)) ||
                        v.VendorNumber.Contains(search));

                var vendors = await query
                    .OrderByDescending(v => v.IsPreferred)
                    .ThenBy(v => v.Name)
                    .Select(v => new
                    {
                        v.Id, v.VendorNumber, v.Name, v.Category, v.ContactName,
                        v.Email, v.Phone, v.City, v.State,
                        v.PaymentTerms, v.CreditLimit, v.IsActive, v.IsPreferred,
                        v.CreatedAt, v.UpdatedAt,
                        PurchaseCount = _context.VendorPurchases.Count(p => p.VendorId == v.Id),
                        TotalSpend = _context.VendorPurchases
                            .Where(p => p.VendorId == v.Id)
                            .Sum(p => (decimal?)p.Amount) ?? 0
                    })
                    .ToListAsync();

                return Ok(vendors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching vendors");
                return StatusCode(500, new { message = "Error fetching vendors" });
            }
        }

        // GET: api/vendors/summary
        [HttpGet("summary")]
        public async Task<ActionResult<object>> GetSummary()
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var vendors = await _context.Vendors.Where(v => v.CompanyId == companyId).ToListAsync();
                var purchases = await _context.VendorPurchases.Where(p => p.CompanyId == companyId).ToListAsync();

                return Ok(new
                {
                    TotalVendors   = vendors.Count,
                    ActiveVendors  = vendors.Count(v => v.IsActive),
                    PreferredCount = vendors.Count(v => v.IsPreferred),
                    TotalSpend     = purchases.Sum(p => p.Amount),
                    PendingOrders  = purchases.Count(p => p.Status == "pending" || p.Status == "ordered"),
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching vendor summary");
                return StatusCode(500, new { message = "Error fetching summary" });
            }
        }

        // GET: api/vendors/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetVendor(int id)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var vendor = await _context.Vendors
                    .Include(v => v.Purchases).ThenInclude(p => p.Job)
                    .FirstOrDefaultAsync(v => v.Id == id && v.CompanyId == companyId);

                if (vendor == null) return NotFound(new { message = "Vendor not found" });

                return Ok(new
                {
                    vendor.Id, vendor.VendorNumber, vendor.Name, vendor.Category,
                    vendor.ContactName, vendor.Email, vendor.Phone,
                    vendor.Address, vendor.City, vendor.State, vendor.ZipCode, vendor.Website,
                    vendor.AccountNumber, vendor.PaymentTerms, vendor.CreditLimit,
                    vendor.Notes, vendor.IsActive, vendor.IsPreferred,
                    vendor.CreatedBy, vendor.CreatedAt, vendor.UpdatedAt,
                    Purchases = vendor.Purchases
                        .OrderByDescending(p => p.CreatedAt)
                        .Select(p => new
                        {
                            p.Id, p.PurchaseOrderNumber, p.Description, p.Amount,
                            p.Status, p.OrderedAt, p.ReceivedAt, p.RecordedBy, p.CreatedAt,
                            p.JobId,
                            JobNumber = p.Job?.JobNumber,
                            JobName   = p.Job?.Name
                        }),
                    TotalSpend = vendor.Purchases.Sum(p => p.Amount),
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching vendor {Id}", id);
                return StatusCode(500, new { message = "Error fetching vendor" });
            }
        }

        // POST: api/vendors
        [HttpPost]
        public async Task<ActionResult<object>> CreateVendor([FromBody] CreateVendorRequest req)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var vendorNumber = await GenerateVendorNumberAsync(companyId);

                var vendor = new Vendor
                {
                    CompanyId    = companyId,
                    VendorNumber = vendorNumber,
                    Name         = req.Name.Trim(),
                    Category     = req.Category?.Trim(),
                    ContactName  = req.ContactName?.Trim(),
                    Email        = req.Email?.Trim(),
                    Phone        = req.Phone?.Trim(),
                    Address      = req.Address?.Trim(),
                    City         = req.City?.Trim(),
                    State        = req.State?.Trim(),
                    ZipCode      = req.ZipCode?.Trim(),
                    Website      = req.Website?.Trim(),
                    AccountNumber = req.AccountNumber?.Trim(),
                    PaymentTerms = req.PaymentTerms?.Trim(),
                    CreditLimit  = req.CreditLimit,
                    Notes        = req.Notes?.Trim(),
                    IsActive     = true,
                    IsPreferred  = req.IsPreferred,
                    CreatedBy    = GetCurrentUserName(),
                };

                _context.Vendors.Add(vendor);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetVendor), new { id = vendor.Id }, new { vendor.Id, vendor.VendorNumber, vendor.Name });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating vendor");
                return StatusCode(500, new { message = "Error creating vendor" });
            }
        }

        // PUT: api/vendors/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<object>> UpdateVendor(int id, [FromBody] UpdateVendorRequest req)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.Id == id && v.CompanyId == companyId);
                if (vendor == null) return NotFound(new { message = "Vendor not found" });

                vendor.Name         = req.Name?.Trim() ?? vendor.Name;
                vendor.Category     = req.Category?.Trim() ?? vendor.Category;
                vendor.ContactName  = req.ContactName?.Trim();
                vendor.Email        = req.Email?.Trim();
                vendor.Phone        = req.Phone?.Trim();
                vendor.Address      = req.Address?.Trim();
                vendor.City         = req.City?.Trim();
                vendor.State        = req.State?.Trim();
                vendor.ZipCode      = req.ZipCode?.Trim();
                vendor.Website      = req.Website?.Trim();
                vendor.AccountNumber = req.AccountNumber?.Trim();
                vendor.PaymentTerms = req.PaymentTerms?.Trim();
                vendor.CreditLimit  = req.CreditLimit;
                vendor.Notes        = req.Notes?.Trim();
                vendor.IsPreferred  = req.IsPreferred;
                vendor.UpdatedAt    = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok(new { vendor.Id, vendor.VendorNumber, vendor.Name });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vendor {Id}", id);
                return StatusCode(500, new { message = "Error updating vendor" });
            }
        }

        // POST: api/vendors/{id}/deactivate
        [HttpPost("{id}/deactivate")]
        public async Task<ActionResult> Deactivate(int id)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.Id == id && v.CompanyId == companyId);
                if (vendor == null) return NotFound(new { message = "Vendor not found" });
                vendor.IsActive  = false;
                vendor.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Vendor deactivated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating vendor {Id}", id);
                return StatusCode(500, new { message = "Error deactivating vendor" });
            }
        }

        // POST: api/vendors/{id}/activate
        [HttpPost("{id}/activate")]
        public async Task<ActionResult> Activate(int id)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.Id == id && v.CompanyId == companyId);
                if (vendor == null) return NotFound(new { message = "Vendor not found" });
                vendor.IsActive  = true;
                vendor.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Vendor activated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating vendor {Id}", id);
                return StatusCode(500, new { message = "Error activating vendor" });
            }
        }

        // POST: api/vendors/{id}/purchases
        [HttpPost("{id}/purchases")]
        public async Task<ActionResult<object>> AddPurchase(int id, [FromBody] CreatePurchaseRequest req)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.Id == id && v.CompanyId == companyId);
                if (vendor == null) return NotFound(new { message = "Vendor not found" });

                var purchase = new VendorPurchase
                {
                    VendorId            = id,
                    CompanyId           = companyId,
                    JobId               = req.JobId,
                    PurchaseOrderNumber = req.PurchaseOrderNumber?.Trim(),
                    Description         = req.Description?.Trim(),
                    Amount              = req.Amount,
                    Status              = req.Status ?? "pending",
                    OrderedAt           = req.OrderedAt,
                    ReceivedAt          = req.ReceivedAt,
                    RecordedBy          = GetCurrentUserName(),
                };

                _context.VendorPurchases.Add(purchase);
                await _context.SaveChangesAsync();
                return Ok(new { purchase.Id, purchase.Amount, purchase.Status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding purchase to vendor {Id}", id);
                return StatusCode(500, new { message = "Error adding purchase" });
            }
        }

        // DELETE: api/vendors/{id}/purchases/{purchaseId}
        [HttpDelete("{id}/purchases/{purchaseId}")]
        public async Task<ActionResult> DeletePurchase(int id, int purchaseId)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var purchase = await _context.VendorPurchases
                    .FirstOrDefaultAsync(p => p.Id == purchaseId && p.VendorId == id && p.CompanyId == companyId);
                if (purchase == null) return NotFound();
                _context.VendorPurchases.Remove(purchase);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Purchase deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting purchase {PurchaseId}", purchaseId);
                return StatusCode(500, new { message = "Error deleting purchase" });
            }
        }

        // DELETE: api/vendors/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteVendor(int id)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var vendor = await _context.Vendors
                    .Include(v => v.Purchases)
                    .FirstOrDefaultAsync(v => v.Id == id && v.CompanyId == companyId);
                if (vendor == null) return NotFound(new { message = "Vendor not found" });
                if (vendor.Purchases.Any())
                    return BadRequest(new { message = "Cannot delete a vendor with purchase history. Deactivate instead." });
                _context.Vendors.Remove(vendor);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Vendor deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting vendor {Id}", id);
                return StatusCode(500, new { message = "Error deleting vendor" });
            }
        }
    }

    // ── Request DTOs ──────────────────────────────────────────────
    public class CreateVendorRequest
    {
        public string Name          { get; set; } = string.Empty;
        public string? Category     { get; set; }
        public string? ContactName  { get; set; }
        public string? Email        { get; set; }
        public string? Phone        { get; set; }
        public string? Address      { get; set; }
        public string? City         { get; set; }
        public string? State        { get; set; }
        public string? ZipCode      { get; set; }
        public string? Website      { get; set; }
        public string? AccountNumber { get; set; }
        public string? PaymentTerms { get; set; }
        public decimal? CreditLimit { get; set; }
        public string? Notes        { get; set; }
        public bool IsPreferred     { get; set; }
    }

    public class UpdateVendorRequest : CreateVendorRequest { }

    public class CreatePurchaseRequest
    {
        public int? JobId                   { get; set; }
        public string? PurchaseOrderNumber  { get; set; }
        public string? Description          { get; set; }
        public decimal Amount               { get; set; }
        public string? Status               { get; set; }
        public DateTime? OrderedAt          { get; set; }
        public DateTime? ReceivedAt         { get; set; }
    }
}
