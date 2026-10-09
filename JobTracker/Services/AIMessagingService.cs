using System.Text.Json;
using JobTracker.Models;
using JobTracker.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace JobTracker.Services
{
    public class AIMessagingService : IAIMessagingService
    {
        private readonly ILogger<AIMessagingService> _logger;
        private readonly JobTrackerContext _context;
        private readonly ITenantContext _tenantContext;
        private readonly bool _hasOpenAI;

        public AIMessagingService(
            ILogger<AIMessagingService> logger,
            JobTrackerContext context,
            ITenantContext tenantContext)
        {
            _logger = logger;
            _context = context;
            _tenantContext = tenantContext;
            
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            _hasOpenAI = !string.IsNullOrEmpty(apiKey);
        }

        public async Task<string> GenerateVerificationMessageAsync(string employeeName, string verificationCode)
        {
            RequireTenant();

            // Smart template-based message generation
            var templates = new[]
            {
                $"Hi {employeeName}! Your PROVIA verification code is {verificationCode}. Expires in 10 minutes.",
                $"{employeeName}, your verification code: {verificationCode}. Valid for 10 minutes - PROVIA",
                $"PROVIA: {employeeName}, use code {verificationCode} to verify. Expires in 10 minutes.",
                $"Hello {employeeName}, verification code {verificationCode} for PROVIA. Expires in 10 min."
            };

            // Select template based on name length to optimize SMS character count
            var firstName = employeeName.Split(' ')[0];
            var selectedTemplate = employeeName.Length > 15 
                ? templates[1].Replace(employeeName, firstName)
                : templates[0];

            await Task.CompletedTask; // Maintain async pattern
            return selectedTemplate;
        }

        public async Task<string> GenerateJobUpdateMessageAsync(Job job, string updateType)
        {
            var tenantJob = await LoadTenantJobAsync(job);

            var templates = new[]
            {
                $"Job Update: {tenantJob.Name} - {updateType}. Status: {tenantJob.Status}. Job #{tenantJob.JobNumber}",
                $"{tenantJob.Name} ({tenantJob.JobNumber}): {updateType}. Current status: {tenantJob.Status}",
                $"Update on Job #{tenantJob.JobNumber} - {tenantJob.Name}: {updateType}",
                $"{updateType} - {tenantJob.Name}. Job #{tenantJob.JobNumber} now {tenantJob.Status}"
            };

            // Select template based on total length to stay under SMS limits
            var baseMessage = templates[0];
            if (baseMessage.Length > 160)
            {
                baseMessage = templates[2]; // Shorter version
            }

            await Task.CompletedTask;
            return baseMessage;
        }

        public async Task<string> GenerateScheduleReminderAsync(string employeeName, Job job, DateTime scheduledTime)
        {
            var tenantJob = await LoadTenantJobAsync(job);
            var firstName = employeeName.Split(' ')[0];
            var timeFormatted = scheduledTime.ToString("h:mm tt");
            
            var templates = new[]
            {
                $"Hi {firstName}! Reminder: {tenantJob.Name} today at {timeFormatted}. Job #{tenantJob.JobNumber}",
                $"{firstName}, you're scheduled for {tenantJob.Name} at {timeFormatted} today. Job #{tenantJob.JobNumber}",
                $"Work reminder: {tenantJob.Name} - {timeFormatted} today, {firstName}. Job #{tenantJob.JobNumber}",
                $"{firstName}: {tenantJob.Name} today {timeFormatted}. Job #{tenantJob.JobNumber}"
            };

            var selectedTemplate = templates[0];
            if (selectedTemplate.Length > 160)
            {
                selectedTemplate = templates[3]; // Shortest version
            }

            await Task.CompletedTask;
            return selectedTemplate;
        }

        public async Task<string> ProcessIncomingQueryAsync(string query, int? jobId = null)
        {
            try
            {
                var companyId = RequireTenant();
                query = query.ToLowerInvariant();
                
                // Get relevant job data if jobId is provided
                string contextData = "";
                if (jobId.HasValue)
                {
                    var job = await _context.Jobs.AsNoTracking()
                        .FirstOrDefaultAsync(j => j.Id == jobId.Value && j.CompanyId == companyId);
                    if (job == null)
                        throw new CrossTenantReferenceException();

                    contextData = $"Job: {job.Name} (#{job.JobNumber}), Status: {job.Status}, Location: {job.Location}, Start: {job.StartDate:MM/dd/yyyy}";
                }

                // Smart pattern matching for common queries
                if (query.Contains("status") || query.Contains("progress"))
                {
                    if (!string.IsNullOrEmpty(contextData))
                    {
                        var job = await _context.Jobs.AsNoTracking()
                            .FirstAsync(j => j.Id == jobId!.Value && j.CompanyId == companyId);
                        return $"Job {job?.Name} (#{job?.JobNumber}) is currently {job?.Status}. Started {job?.StartDate:MM/dd/yyyy}.";
                    }
                    return "To check job status, please specify the job number (e.g., #1234).";
                }

                if (query.Contains("schedule") || query.Contains("when"))
                {
                    return "For schedule information, contact your supervisor or check the job board.";
                }

                if (query.Contains("building code") || query.Contains("regulation"))
                {
                    return "For Massachusetts building code questions, refer to the latest residential/commercial code documents or contact the building inspector.";
                }

                if (query.Contains("location") || query.Contains("address"))
                {
                    if (!string.IsNullOrEmpty(contextData))
                    {
                        var job = await _context.Jobs.AsNoTracking()
                            .FirstAsync(j => j.Id == jobId!.Value && j.CompanyId == companyId);
                        return $"Job location: {job?.Location}";
                    }
                    return "Please specify which job location you need.";
                }

                // Default helpful response
                return "I can help with job status, schedules, building codes, and locations. Please be specific about what you need or contact your supervisor.";
            }
            catch (CrossTenantReferenceException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing query");
                return "Please contact your supervisor for assistance.";
            }
        }

        public async Task<string> GenerateResponseToWhatsAppMessageAsync(string fromNumber, string messageContent)
        {
            var companyId = RequireTenant();

            // Extract job number from message content
            var jobNumberMatch = Regex.Match(messageContent, @"#(\d+)");
            int? jobId = null;
            
            if (jobNumberMatch.Success && int.TryParse(jobNumberMatch.Groups[1].Value, out int jobNumber))
            {
                var job = await _context.Jobs.AsNoTracking()
                    .FirstOrDefaultAsync(j => j.JobNumber == jobNumber.ToString() && j.CompanyId == companyId);

                if (job == null && await _context.Jobs.AsNoTracking()
                    .AnyAsync(j => j.JobNumber == jobNumber.ToString()))
                {
                    throw new CrossTenantReferenceException();
                }

                jobId = job?.Id;
            }
            
            return await ProcessIncomingQueryAsync(messageContent, jobId);
        }

        private int RequireTenant() => _tenantContext.GetCurrentCompanyId();

        private async Task<Job> LoadTenantJobAsync(Job requestedJob)
        {
            var companyId = RequireTenant();

            if (requestedJob == null || requestedJob.Id <= 0)
                throw new CrossTenantReferenceException();

            var tenantJob = await _context.Jobs.AsNoTracking()
                .FirstOrDefaultAsync(j => j.Id == requestedJob.Id && j.CompanyId == companyId);

            if (tenantJob == null)
                throw new CrossTenantReferenceException();

            return tenantJob;
        }
    }

    public sealed class CrossTenantReferenceException : InvalidOperationException
    {
        public CrossTenantReferenceException()
            : base("Referenced data does not belong to the authenticated company.")
        {
        }
    }
}