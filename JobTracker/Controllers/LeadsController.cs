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
    public class LeadsController : ControllerBase
    {
        private readonly JobTrackerContext _context;
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<LeadsController> _logger;

        public LeadsController(JobTrackerContext context, ITenantContext tenantContext, ILogger<LeadsController> logger)
        {
            _context = context;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        private string GetCurrentUserName() =>
            User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst(ClaimTypes.Name)?.Value ?? "System";

        private async Task<string> GenerateLeadNumberAsync(int companyId)
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"L-{year}-";
            var last = await _context.Leads
                .Where(l => l.CompanyId == companyId && l.LeadNumber.StartsWith(prefix))
                .OrderByDescending(l => l.LeadNumber)
                .Select(l => l.LeadNumber)
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

        // GET: api/leads
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetLeads([FromQuery] bool includeArchived = false)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var query = _context.Leads.Where(l => l.CompanyId == companyId);

                if (!includeArchived)
                    query = query.Where(l => !l.IsArchived);

                var leads = await query
                    .OrderBy(l => l.StageEnteredAt)
                    .Select(l => new
                    {
                        l.Id, l.LeadNumber, l.CallerName, l.CallerPhone, l.CallerEmail,
                        l.Source, l.Stage, l.StageEnteredAt, l.JobAddress, l.JobCity,
                        l.JobType, l.JobScope, l.AppointmentAt, l.IsArchived, l.ArchiveReason,
                        l.ContactId, l.JobId, l.CreatedAt,
                        DaysInStage = (int)(DateTime.UtcNow - l.StageEnteredAt).TotalDays,
                        NoteCount = _context.LeadNotes.Count(n => n.LeadId == l.Id)
                    })
                    .ToListAsync();

                return Ok(leads);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching leads");
                return StatusCode(500, new { message = "Error fetching leads" });
            }
        }

        // GET: api/leads/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetLead(int id)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var lead = await _context.Leads
                    .Where(l => l.Id == id && l.CompanyId == companyId)
                    .Select(l => new
                    {
                        l.Id, l.LeadNumber, l.CallerName, l.CallerPhone, l.CallerEmail,
                        l.Source, l.Stage, l.StageEnteredAt, l.JobAddress, l.JobCity,
                        l.JobType, l.JobScope, l.AppointmentAt, l.IsArchived, l.ArchiveReason,
                        l.ContactId, l.JobId, l.CreatedAt, l.UpdatedAt,
                        DaysInStage = (int)(DateTime.UtcNow - l.StageEnteredAt).TotalDays,
                        Notes = _context.LeadNotes
                            .Where(n => n.LeadId == l.Id)
                            .OrderByDescending(n => n.CreatedAt)
                            .Select(n => new { n.Id, n.Body, n.UserName, n.CreatedAt })
                            .ToList()
                    })
                    .FirstOrDefaultAsync();

                if (lead == null) return NotFound(new { message = "Lead not found" });
                return Ok(lead);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching lead {Id}", id);
                return StatusCode(500, new { message = "Error fetching lead" });
            }
        }

        // POST: api/leads
        [HttpPost]
        public async Task<ActionResult<object>> CreateLead([FromBody] CreateLeadRequest req)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var leadNumber = await GenerateLeadNumberAsync(companyId);

                var lead = new Lead
                {
                    CompanyId = companyId,
                    LeadNumber = leadNumber,
                    CallerName = req.CallerName.Trim(),
                    CallerPhone = req.CallerPhone?.Trim(),
                    CallerEmail = req.CallerEmail?.Trim(),
                    Source = req.Source?.Trim() ?? "Direct",
                    Stage = "incoming",
                    StageEnteredAt = DateTime.UtcNow,
                    JobAddress = req.JobAddress?.Trim(),
                    JobCity = req.JobCity?.Trim(),
                    JobType = req.JobType?.Trim(),
                    JobScope = req.JobScope?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Leads.Add(lead);
                await _context.SaveChangesAsync();

                if (!string.IsNullOrWhiteSpace(req.InitialNote))
                {
                    _context.LeadNotes.Add(new LeadNote
                    {
                        LeadId = lead.Id,
                        CompanyId = companyId,
                        Body = req.InitialNote.Trim(),
                        UserName = GetCurrentUserName(),
                        CreatedAt = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();
                }

                return CreatedAtAction(nameof(GetLead), new { id = lead.Id },
                    new { lead.Id, lead.LeadNumber, lead.CallerName });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating lead");
                return StatusCode(500, new { message = "Error creating lead" });
            }
        }

        // PUT: api/leads/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLead(int id, [FromBody] UpdateLeadRequest req)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var lead = await _context.Leads.FirstOrDefaultAsync(l => l.Id == id && l.CompanyId == companyId);
                if (lead == null) return NotFound(new { message = "Lead not found" });

                lead.CallerName = req.CallerName?.Trim() ?? lead.CallerName;
                lead.CallerPhone = req.CallerPhone?.Trim() ?? lead.CallerPhone;
                lead.CallerEmail = req.CallerEmail?.Trim() ?? lead.CallerEmail;
                lead.Source = req.Source?.Trim() ?? lead.Source;
                lead.JobAddress = req.JobAddress?.Trim() ?? lead.JobAddress;
                lead.JobCity = req.JobCity?.Trim() ?? lead.JobCity;
                lead.JobType = req.JobType?.Trim() ?? lead.JobType;
                lead.JobScope = req.JobScope?.Trim() ?? lead.JobScope;
                lead.AppointmentAt = req.AppointmentAt ?? lead.AppointmentAt;
                lead.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating lead {Id}", id);
                return StatusCode(500, new { message = "Error updating lead" });
            }
        }

        // POST: api/leads/{id}/stage
        [HttpPost("{id}/stage")]
        public async Task<IActionResult> AdvanceStage(int id, [FromBody] StageChangeRequest req)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var lead = await _context.Leads.FirstOrDefaultAsync(l => l.Id == id && l.CompanyId == companyId);
                if (lead == null) return NotFound(new { message = "Lead not found" });

                if (!LeadStages.All.Contains(req.Stage))
                    return BadRequest(new { message = "Invalid stage" });

                var prevStage = lead.Stage;
                lead.Stage = req.Stage;
                lead.StageEnteredAt = DateTime.UtcNow;
                lead.UpdatedAt = DateTime.UtcNow;

                if (req.AppointmentAt.HasValue)
                    lead.AppointmentAt = req.AppointmentAt;

                await _context.SaveChangesAsync();

                // Auto-note on stage change
                _context.LeadNotes.Add(new LeadNote
                {
                    LeadId = id,
                    CompanyId = companyId,
                    Body = $"Stage changed: {LeadStages.Labels.GetValueOrDefault(prevStage, prevStage)} → {LeadStages.Labels.GetValueOrDefault(req.Stage, req.Stage)}",
                    UserName = GetCurrentUserName(),
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                return Ok(new { message = "Stage updated", stage = req.Stage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error advancing stage for lead {Id}", id);
                return StatusCode(500, new { message = "Error updating stage" });
            }
        }

        // POST: api/leads/{id}/notes
        [HttpPost("{id}/notes")]
        public async Task<IActionResult> AddNote(int id, [FromBody] AddLeadNoteRequest req)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var exists = await _context.Leads.AnyAsync(l => l.Id == id && l.CompanyId == companyId);
                if (!exists) return NotFound(new { message = "Lead not found" });

                _context.LeadNotes.Add(new LeadNote
                {
                    LeadId = id,
                    CompanyId = companyId,
                    Body = req.Body.Trim(),
                    UserName = GetCurrentUserName(),
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
                return Ok(new { message = "Note added" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding note to lead {Id}", id);
                return StatusCode(500, new { message = "Error adding note" });
            }
        }

        // POST: api/leads/{id}/archive
        [HttpPost("{id}/archive")]
        public async Task<IActionResult> Archive(int id, [FromBody] ArchiveLeadRequest req)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var lead = await _context.Leads.FirstOrDefaultAsync(l => l.Id == id && l.CompanyId == companyId);
                if (lead == null) return NotFound(new { message = "Lead not found" });

                lead.IsArchived = true;
                lead.ArchiveReason = req.Reason?.Trim();
                lead.UpdatedAt = DateTime.UtcNow;

                _context.LeadNotes.Add(new LeadNote
                {
                    LeadId = id,
                    CompanyId = companyId,
                    Body = $"Lead archived — Reason: {req.Reason ?? "Not specified"}",
                    UserName = GetCurrentUserName(),
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                return Ok(new { message = "Lead archived" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving lead {Id}", id);
                return StatusCode(500, new { message = "Error archiving lead" });
            }
        }

        // POST: api/leads/{id}/graduate
        [HttpPost("{id}/graduate")]
        public async Task<IActionResult> GraduateToContact(int id)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var lead = await _context.Leads.FirstOrDefaultAsync(l => l.Id == id && l.CompanyId == companyId);
                if (lead == null) return NotFound(new { message = "Lead not found" });
                if (lead.ContactId.HasValue) return BadRequest(new { message = "Lead already graduated to a contact" });

                // Auto-generate customer number
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
                    if (parts.Length == 3 && int.TryParse(parts[2], out int lastNum)) next = lastNum + 1;
                }
                var customerNumber = $"{prefix}{next:D4}";

                var contact = new Contact
                {
                    CompanyId = companyId,
                    CustomerNumber = customerNumber,
                    FullName = lead.CallerName,
                    Phone = lead.CallerPhone,
                    Email = lead.CallerEmail,
                    City = lead.JobCity,
                    ContactType = lead.JobType == "Commercial" ? "Commercial" : "Residential",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Contacts.Add(contact);
                await _context.SaveChangesAsync();

                lead.ContactId = contact.Id;
                lead.Stage = "signed";
                lead.StageEnteredAt = DateTime.UtcNow;
                lead.UpdatedAt = DateTime.UtcNow;

                _context.LeadNotes.Add(new LeadNote
                {
                    LeadId = id,
                    CompanyId = companyId,
                    Body = $"Graduated to Contact {customerNumber}",
                    UserName = GetCurrentUserName(),
                    CreatedAt = DateTime.UtcNow
                });

                _context.ContactActivityLogs.Add(new ContactActivityLog
                {
                    ContactId = contact.Id,
                    CompanyId = companyId,
                    Action = "Created from Lead",
                    Detail = $"Graduated from lead {lead.LeadNumber}",
                    UserName = GetCurrentUserName(),
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                return Ok(new { message = "Graduated to contact", contactId = contact.Id, customerNumber });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error graduating lead {Id}", id);
                return StatusCode(500, new { message = "Error graduating lead" });
            }
        }
    }

    public class CreateLeadRequest
    {
        public string CallerName { get; set; } = string.Empty;
        public string? CallerPhone { get; set; }
        public string? CallerEmail { get; set; }
        public string? Source { get; set; }
        public string? JobAddress { get; set; }
        public string? JobCity { get; set; }
        public string? JobType { get; set; }
        public string? JobScope { get; set; }
        public string? InitialNote { get; set; }
    }

    public class UpdateLeadRequest
    {
        public string? CallerName { get; set; }
        public string? CallerPhone { get; set; }
        public string? CallerEmail { get; set; }
        public string? Source { get; set; }
        public string? JobAddress { get; set; }
        public string? JobCity { get; set; }
        public string? JobType { get; set; }
        public string? JobScope { get; set; }
        public DateTime? AppointmentAt { get; set; }
    }

    public class StageChangeRequest
    {
        public string Stage { get; set; } = string.Empty;
        public DateTime? AppointmentAt { get; set; }
    }

    public class AddLeadNoteRequest
    {
        public string Body { get; set; } = string.Empty;
    }

    public class ArchiveLeadRequest
    {
        public string? Reason { get; set; }
    }
}
