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
    public class ContactsController : ControllerBase
    {
        private readonly JobTrackerContext _context;
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<ContactsController> _logger;

        public ContactsController(JobTrackerContext context, ITenantContext tenantContext, ILogger<ContactsController> logger)
        {
            _context = context;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        private string GetCurrentUserName()
        {
            return User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.FindFirst(ClaimTypes.Name)?.Value
                ?? "System";
        }

        private async Task LogActivity(int contactId, int companyId, string action, string? detail = null)
        {
            _context.ContactActivityLogs.Add(new ContactActivityLog
            {
                ContactId = contactId,
                CompanyId = companyId,
                Action = action,
                Detail = detail,
                UserName = GetCurrentUserName(),
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        private async Task<string> GenerateCustomerNumberAsync(int companyId)
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"C-{year}-";
            var last = await _context.Contacts
                .Where(c => c.CompanyId == companyId && c.CustomerNumber.StartsWith(prefix))
                .OrderByDescending(c => c.CustomerNumber)
                .Select(c => c.CustomerNumber)
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

        // GET: api/contacts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetContacts([FromQuery] string? q = null, [FromQuery] bool includeInactive = false)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var query = _context.Contacts
                    .Where(c => c.CompanyId == companyId);

                if (!includeInactive)
                    query = query.Where(c => c.IsActive);

                if (!string.IsNullOrWhiteSpace(q))
                {
                    var lower = q.ToLower();
                    query = query.Where(c =>
                        c.FullName.ToLower().Contains(lower) ||
                        (c.CustomerNumber != null && c.CustomerNumber.ToLower().Contains(lower)) ||
                        (c.Email != null && c.Email.ToLower().Contains(lower)) ||
                        (c.Phone != null && c.Phone.Contains(lower)) ||
                        (c.CompanyName != null && c.CompanyName.ToLower().Contains(lower)) ||
                        (c.City != null && c.City.ToLower().Contains(lower)));
                }

                var contacts = await query
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new
                    {
                        c.Id,
                        c.CustomerNumber,
                        c.FullName,
                        c.CompanyName,
                        c.ContactType,
                        c.Phone,
                        c.Email,
                        c.City,
                        c.State,
                        c.IsActive,
                        c.CreatedAt,
                        JobCount = _context.Jobs.Count(j => j.ContactId == c.Id)
                    })
                    .ToListAsync();

                return Ok(contacts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching contacts");
                return StatusCode(500, new { message = "Error fetching contacts" });
            }
        }

        // GET: api/contacts/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetContact(int id)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var contact = await _context.Contacts
                    .Where(c => c.Id == id && c.CompanyId == companyId)
                    .Select(c => new
                    {
                        c.Id,
                        c.CustomerNumber,
                        c.FullName,
                        c.CompanyName,
                        c.ContactType,
                        c.Phone,
                        c.Email,
                        c.Address,
                        c.City,
                        c.State,
                        c.ZipCode,
                        c.Notes,
                        c.IsActive,
                        c.CreatedAt,
                        c.UpdatedAt,
                        Jobs = _context.Jobs
                            .Where(j => j.ContactId == c.Id)
                            .Select(j => new { j.Id, j.JobNumber, j.Name, j.Status, j.Location, j.CreatedAt })
                            .ToList(),
                        ActivityLogs = _context.ContactActivityLogs
                            .Where(a => a.ContactId == c.Id)
                            .OrderByDescending(a => a.CreatedAt)
                            .Take(50)
                            .Select(a => new { a.Id, a.Action, a.Detail, a.UserName, a.CreatedAt })
                            .ToList(),
                        Documents = _context.ContactDocuments
                            .Where(d => d.ContactId == c.Id)
                            .OrderByDescending(d => d.CreatedAt)
                            .Select(d => new { d.Id, d.OriginalName, d.ContentType, d.FileSize, d.UploadedBy, d.CreatedAt })
                            .ToList()
                    })
                    .FirstOrDefaultAsync();

                if (contact == null)
                    return NotFound(new { message = "Contact not found" });

                return Ok(contact);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching contact {Id}", id);
                return StatusCode(500, new { message = "Error fetching contact" });
            }
        }

        // POST: api/contacts
        [HttpPost]
        public async Task<ActionResult<object>> CreateContact([FromBody] CreateContactRequest req)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var customerNumber = await GenerateCustomerNumberAsync(companyId);

                var contact = new Contact
                {
                    CompanyId = companyId,
                    CustomerNumber = customerNumber,
                    FullName = req.FullName.Trim(),
                    CompanyName = req.CompanyName?.Trim(),
                    ContactType = req.ContactType ?? "Residential",
                    Phone = req.Phone?.Trim(),
                    Email = req.Email?.Trim(),
                    Address = req.Address?.Trim(),
                    City = req.City?.Trim(),
                    State = req.State?.Trim(),
                    ZipCode = req.ZipCode?.Trim(),
                    Notes = req.Notes?.Trim(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Contacts.Add(contact);
                await _context.SaveChangesAsync();

                await LogActivity(contact.Id, companyId, "Created", $"Contact {customerNumber} — {contact.FullName}");

                _logger.LogInformation("Contact {CustomerNumber} created for company {CompanyId}", customerNumber, companyId);
                return CreatedAtAction(nameof(GetContact), new { id = contact.Id }, new { contact.Id, contact.CustomerNumber, contact.FullName });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contact");
                return StatusCode(500, new { message = "Error creating contact" });
            }
        }

        // PUT: api/contacts/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateContact(int id, [FromBody] UpdateContactRequest req)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var contact = await _context.Contacts.FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId);

                if (contact == null)
                    return NotFound(new { message = "Contact not found" });

                contact.FullName = req.FullName?.Trim() ?? contact.FullName;
                contact.CompanyName = req.CompanyName?.Trim() ?? contact.CompanyName;
                contact.ContactType = req.ContactType ?? contact.ContactType;
                contact.Phone = req.Phone?.Trim() ?? contact.Phone;
                contact.Email = req.Email?.Trim() ?? contact.Email;
                contact.Address = req.Address?.Trim() ?? contact.Address;
                contact.City = req.City?.Trim() ?? contact.City;
                contact.State = req.State?.Trim() ?? contact.State;
                contact.ZipCode = req.ZipCode?.Trim() ?? contact.ZipCode;
                contact.Notes = req.Notes?.Trim() ?? contact.Notes;
                contact.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await LogActivity(id, companyId, "Updated", $"Fields updated by {GetCurrentUserName()}");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contact {Id}", id);
                return StatusCode(500, new { message = "Error updating contact" });
            }
        }

        // DELETE: api/contacts/{id}  (soft delete)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeactivateContact(int id)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var contact = await _context.Contacts.FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId);

                if (contact == null)
                    return NotFound(new { message = "Contact not found" });

                contact.IsActive = false;
                contact.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                await LogActivity(id, companyId, "Deactivated", $"Deactivated by {GetCurrentUserName()}");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating contact {Id}", id);
                return StatusCode(500, new { message = "Error deactivating contact" });
            }
        }

        // POST: api/contacts/{id}/activity
        [HttpPost("{id}/activity")]
        public async Task<IActionResult> AddNote(int id, [FromBody] AddNoteRequest req)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var exists = await _context.Contacts.AnyAsync(c => c.Id == id && c.CompanyId == companyId);
                if (!exists)
                    return NotFound(new { message = "Contact not found" });

                await LogActivity(id, companyId, "Note", req.Note);
                return Ok(new { message = "Note added" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding note to contact {Id}", id);
                return StatusCode(500, new { message = "Error adding note" });
            }
        }

        // POST: api/contacts/{id}/link-job/{jobId}
        [HttpPost("{id}/link-job/{jobId}")]
        public async Task<IActionResult> LinkJob(int id, int jobId)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var contact = await _context.Contacts.FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId);
                if (contact == null) return NotFound(new { message = "Contact not found" });

                var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId && j.CompanyId == companyId);
                if (job == null) return NotFound(new { message = "Job not found" });

                job.ContactId = id;
                await _context.SaveChangesAsync();
                await LogActivity(id, companyId, "Job Linked", $"Linked to job {job.JobNumber} — {job.Name}");

                return Ok(new { message = "Job linked" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error linking job to contact");
                return StatusCode(500, new { message = "Error linking job" });
            }
        }
    }

    public class CreateContactRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string? ContactType { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateContactRequest : CreateContactRequest { }

    public class AddNoteRequest
    {
        public string Note { get; set; } = string.Empty;
    }
}
