using Microsoft.AspNetCore.Mvc;
using JobTracker.Services;
using JobTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly JobTrackerContext _context;

        public TestController(IEmailService emailService, JobTrackerContext context)
        {
            _emailService = emailService;
            _context = context;
        }

        [HttpPost("send-material-email")]
        public async Task<IActionResult> SendMaterialEmail()
        {
            try
            {
                var materialRun = await _context.MaterialRuns
                    .Include(m => m.User)
                    .Include(m => m.Job)
                    .OrderByDescending(m => m.StartTime)
                    .FirstOrDefaultAsync();

                if (materialRun == null)
                {
                    return BadRequest("No material run found");
                }

                var subject = $"Material Run Request - {materialRun.User.FirstName} {materialRun.User.LastName}";
                var body = $@"
Material Run Details:

Employee: {materialRun.User.FirstName} {materialRun.User.LastName}
Job Site: {materialRun.Job.Location}
Store Type: {materialRun.StoreType}
Start Time: {materialRun.StartTime:MM/dd/yyyy HH:mm}

Materials Needed:
{materialRun.Materials}

This is an automated notification from the Job Tracker system.
";

                var result = await _emailService.SendEmailAsync("erika.silva@preferredbuildersusa.com", subject, body);
                
                if (result)
                {
                    return Ok(new { success = true, message = "Email sent to admin Erika" });
                }
                else
                {
                    return BadRequest(new { success = false, message = "Failed to send email" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}