using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobTracker.Data;
using JobTracker.Models;
using JobTracker.Services;

namespace JobTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IssuesController : ControllerBase
    {
        private readonly JobTrackerContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<IssuesController> _logger;
        private readonly JobTracker.Services.ITenantContext _tenantContext;

        public IssuesController(JobTrackerContext context, IEmailService emailService, ILogger<IssuesController> logger, JobTracker.Services.ITenantContext tenantContext)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
            _tenantContext = tenantContext;
        }

        [HttpPost("report")]
        public async Task<IActionResult> SubmitIssueReport([FromBody] IssueReportRequest request)
        {
            try
            {
                var userId = 9; // Mike Johnson for testing

                // Get user information
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return BadRequest(new { success = false, message = "User not found" });
                }

                // Get job information if specified
                Job? job = null;
                if (request.JobId.HasValue)
                {
                    job = await _context.Jobs.FindAsync(request.JobId.Value);
                }

                // Create issue report record with company ID for tenant isolation
                var issueReport = new IssueReport
                {
                    CompanyId = user.CompanyId,
                    UserId = userId,
                    JobId = request.JobId,
                    IssueType = request.IssueType,
                    Priority = request.Priority,
                    Description = request.Description,
                    LocationNotes = request.LocationNotes,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    Accuracy = request.Accuracy,
                    Status = "Open",
                    SubmittedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.IssueReports.Add(issueReport);
                await _context.SaveChangesAsync();

                // Send notifications to administrators
                await SendIssueNotifications(issueReport, user, job);

                return Ok(new { 
                    success = true, 
                    message = "Issue report submitted successfully",
                    reportId = issueReport.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting issue report");
                return StatusCode(500, new { success = false, message = "Server error submitting issue report" });
            }
        }

        private async Task SendIssueNotifications(IssueReport report, User user, Job? job)
        {
            try
            {
                // Get all master admins (1510) and admins (1520)
                var adminUsers = await _context.Users
                    .Where(u => u.Role >= 1510)
                    .ToListAsync();

                if (!adminUsers.Any()) return;

                var changeOrderRequired = report.IssueType == "Unforeseen Condition";
                var subject = changeOrderRequired 
                    ? $"⚠️ CHANGE ORDER REQUIRED - Issue Report #{report.Id} - {report.Priority} Priority"
                    : $"🚨 Issue Report #{report.Id} - {report.Priority} Priority";
                
                // Create HTML email content
                var priorityColor = report.Priority switch
                {
                    "Critical" => "#dc3545",
                    "High" => "#fd7e14", 
                    "Medium" => "#0d6efd",
                    "Low" => "#6c757d",
                    _ => "#0d6efd"
                };

                var priorityIcon = report.Priority switch
                {
                    "Critical" => "🔴",
                    "High" => "🟠",
                    "Medium" => "🔵", 
                    "Low" => "⚪",
                    _ => "🔵"
                };

                var locationInfo = "";
                if (report.Latitude.HasValue && report.Longitude.HasValue)
                {
                    locationInfo = $@"
                        <div class=""value"">
                            <span class=""label"">📍 GPS Location:</span> 
                            {report.Latitude:F6}, {report.Longitude:F6}
                            {(report.Accuracy.HasValue ? $" (±{report.Accuracy:F0}m accuracy)" : "")}
                        </div>";
                }

                var changeOrderAlert = changeOrderRequired ? $@"
      <div class=""change-order-alert"">
        <div style=""background-color: #ffc107; color: #212529; padding: 15px; border-radius: 5px; margin: 20px 0; border-left: 5px solid #fd7e14;"">
          <strong>⚠️ CHANGE ORDER REQUIRED</strong><br>
          This unforeseen condition may require client approval and change order processing before work can continue.
        </div>
      </div>" : "";

                var htmlBody = $@"
<!DOCTYPE html>
<html>
  <head>
    <style>
      body {{
        font-family: Roboto, Arial, sans-serif;
        background-color: #f5f5f5;
        margin: 0;
        padding: 20px;
      }}
      .container {{
        background-color: #ffffff;
        padding: 20px;
        border-radius: 8px;
        max-width: 600px;
        margin: auto;
        box-shadow: 0 2px 4px rgba(0,0,0,0.1);
      }}
      .header {{
        background-color: {priorityColor};
        color: white;
        padding: 15px;
        border-radius: 8px 8px 0 0;
        margin: -20px -20px 20px -20px;
        text-align: center;
      }}
      .label {{
        font-weight: bold;
        color: #333;
      }}
      .value {{
        margin-bottom: 10px;
        padding: 8px 0;
        border-bottom: 1px solid #eee;
      }}
      .priority-badge {{
        background-color: {priorityColor};
        color: white;
        padding: 4px 12px;
        border-radius: 20px;
        font-weight: bold;
        display: inline-block;
      }}
      .description-box {{
        background-color: #f8f9fa;
        padding: 15px;
        border-radius: 5px;
        border-left: 4px solid {priorityColor};
        margin: 15px 0;
      }}
      .footer {{
        font-size: 12px;
        color: #777;
        margin-top: 20px;
        text-align: center;
        padding-top: 15px;
        border-top: 1px solid #eee;
      }}
    </style>
  </head>
  <body>
    <div class=""container"">
      <div class=""header"">
        <h2>{priorityIcon} Issue Report #{report.Id}</h2>
        <div class=""priority-badge"">{report.Priority} Priority</div>
      </div>
      
      {changeOrderAlert}
      
      <div class=""value""><span class=""label"">👷 Reported by:</span> {user.FirstName} {user.LastName}</div>
      <div class=""value""><span class=""label"">📧 Contact:</span> {user.Email}</div>
      {(string.IsNullOrEmpty(user.PhoneNumber) ? "" : $@"<div class=""value""><span class=""label"">📱 Phone:</span> {user.PhoneNumber}</div>")}
      <div class=""value""><span class=""label"">🏗️ Issue Type:</span> {report.IssueType}</div>
      <div class=""value""><span class=""label"">📅 Submitted:</span> {report.SubmittedAt:MM/dd/yyyy HH:mm} UTC</div>
      {(job != null ? $@"<div class=""value""><span class=""label"">🏢 Job Site:</span> {job.Name} - {job.Location}</div>" : "")}
      {locationInfo}
      {(string.IsNullOrEmpty(report.LocationNotes) ? "" : $@"<div class=""value""><span class=""label"">📍 Location Notes:</span> {System.Web.HttpUtility.HtmlEncode(report.LocationNotes)}</div>")}

      <div class=""description-box"">
        <div class=""label"">📝 Issue Description:</div>
        <div style=""margin-top: 8px; line-height: 1.5;"">{System.Web.HttpUtility.HtmlEncode(report.Description)}</div>
      </div>

      <div class=""footer"">
        This is an automated notification from the <strong>PROVIA</strong> system.<br>
        Report ID: #{report.Id} | Status: Open
        {(changeOrderRequired ? "<br><strong>Action Required:</strong> Review for change order processing" : "")}
      </div>
    </div>
  </body>
</html>";

                var plainTextBody = $@"
ISSUE REPORT #{report.Id} - {report.Priority} Priority

Reported by: {user.FirstName} {user.LastName}
Contact: {user.Email}
{(string.IsNullOrEmpty(user.PhoneNumber) ? "" : $"Phone: {user.PhoneNumber}\n")}
Issue Type: {report.IssueType}
Submitted: {report.SubmittedAt:MM/dd/yyyy HH:mm} UTC
{(job != null ? $"Job Site: {job.Name} - {job.Location}\n" : "")}
{(report.Latitude.HasValue && report.Longitude.HasValue ? $"GPS Location: {report.Latitude:F6}, {report.Longitude:F6}\n" : "")}
{(string.IsNullOrEmpty(report.LocationNotes) ? "" : $"Location Notes: {report.LocationNotes}\n")}

Issue Description:
{report.Description}

---
This is an automated notification from the PROVIA system.
Report ID: #{report.Id} | Status: Open
";

                // Send emails to all administrators
                foreach (var admin in adminUsers)
                {
                    if (!string.IsNullOrEmpty(admin.Email))
                    {
                        await _emailService.SendHtmlEmailAsync(admin.Email, subject, htmlBody, plainTextBody);
                    }
                }

                // Send SMS notifications using Twilio
                await SendSMSNotifications(adminUsers, report, user, job);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send issue report notifications");
            }
        }

        private async Task SendSMSNotifications(List<User> adminUsers, IssueReport report, User user, Job? job)
        {
            try
            {
                var priorityIcon = report.Priority switch
                {
                    "Critical" => "🔴",
                    "High" => "🟠", 
                    "Medium" => "🔵",
                    "Low" => "⚪",
                    _ => "🔵"
                };

                var smsMessage = $@"{priorityIcon} ISSUE REPORT #{report.Id}

Priority: {report.Priority}
Type: {report.IssueType}
From: {user.FirstName} {user.LastName}
{(job != null ? $"Site: {job.Name}" : "General Issue")}

{report.Description}

Submitted: {report.SubmittedAt:MM/dd HH:mm}";

                foreach (var admin in adminUsers)
                {
                    if (!string.IsNullOrEmpty(admin.PhoneNumber))
                    {
                        // This would use Twilio service - for now log the attempt
                        _logger.LogInformation($"SMS notification would be sent to {admin.PhoneNumber}: Issue Report #{report.Id}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS notifications for issue report");
            }
        }
    }

    public class IssueReportRequest
    {
        public string IssueType { get; set; } = "";
        public string Priority { get; set; } = "";
        public int? JobId { get; set; }
        public string Description { get; set; } = "";
        public string? LocationNotes { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? Accuracy { get; set; }
        public DateTime SubmittedAt { get; set; }
    }
}