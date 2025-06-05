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
        private readonly bool _hasOpenAI;

        public AIMessagingService(ILogger<AIMessagingService> logger, JobTrackerContext context)
        {
            _logger = logger;
            _context = context;
            
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            _hasOpenAI = !string.IsNullOrEmpty(apiKey);
        }

        public async Task<string> GenerateVerificationMessageAsync(string employeeName, string verificationCode)
        {
            // Smart template-based message generation
            var templates = new[]
            {
                $"Hi {employeeName}! Your Job Tracker verification code is {verificationCode}. Expires in 10 minutes.",
                $"{employeeName}, your verification code: {verificationCode}. Valid for 10 minutes - Job Tracker",
                $"Job Tracker: {employeeName}, use code {verificationCode} to verify. Expires in 10 minutes.",
                $"Hello {employeeName}, verification code {verificationCode} for Job Tracker. Expires in 10 min."
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
            var templates = new[]
            {
                $"Job Update: {job.Name} - {updateType}. Status: {job.Status}. Job #{job.JobNumber}",
                $"{job.Name} ({job.JobNumber}): {updateType}. Current status: {job.Status}",
                $"Update on Job #{job.JobNumber} - {job.Name}: {updateType}",
                $"{updateType} - {job.Name}. Job #{job.JobNumber} now {job.Status}"
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
            var firstName = employeeName.Split(' ')[0];
            var timeFormatted = scheduledTime.ToString("h:mm tt");
            
            var templates = new[]
            {
                $"Hi {firstName}! Reminder: {job.Name} today at {timeFormatted}. Job #{job.JobNumber}",
                $"{firstName}, you're scheduled for {job.Name} at {timeFormatted} today. Job #{job.JobNumber}",
                $"Work reminder: {job.Name} - {timeFormatted} today, {firstName}. Job #{job.JobNumber}",
                $"{firstName}: {job.Name} today {timeFormatted}. Job #{job.JobNumber}"
            };

            var selectedTemplate = templates[0];
            if (selectedTemplate.Length > 160)
            {
                selectedTemplate = templates[3]; // Shortest version
            }

            await Task.CompletedTask;
            return selectedTemplate;
        }

        public async Task<string> ProcessIncomingQueryAsync(string query, int? jobId = null, int? userId = null)
        {
            try
            {
                query = query.ToLowerInvariant();
                
                // Get relevant job data if jobId is provided
                string contextData = "";
                if (jobId.HasValue)
                {
                    var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId.Value);
                    if (job != null)
                    {
                        contextData = $"Job: {job.Name} (#{job.JobNumber}), Status: {job.Status}, Location: {job.Location}, Start: {job.StartDate:MM/dd/yyyy}";
                    }
                }

                // Smart pattern matching for common queries
                if (query.Contains("status") || query.Contains("progress"))
                {
                    if (!string.IsNullOrEmpty(contextData))
                    {
                        var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId.Value);
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
                        var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId.Value);
                        return $"Job location: {job?.Location}";
                    }
                    return "Please specify which job location you need.";
                }

                // Default helpful response
                return "I can help with job status, schedules, building codes, and locations. Please be specific about what you need or contact your supervisor.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing query");
                return "Please contact your supervisor for assistance.";
            }
        }

        public async Task<string> GenerateResponseToWhatsAppMessageAsync(string fromNumber, string messageContent)
        {
            // Extract job number from message content
            var jobNumberMatch = Regex.Match(messageContent, @"#(\d+)");
            int? jobId = null;
            
            if (jobNumberMatch.Success && int.TryParse(jobNumberMatch.Groups[1].Value, out int jobNumber))
            {
                var job = await _context.Jobs.FirstOrDefaultAsync(j => j.JobNumber == jobNumber.ToString());
                jobId = job?.Id;
            }

            // Try to identify user by phone number
            var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == fromNumber);
            
            return await ProcessIncomingQueryAsync(messageContent, jobId, user?.Id);
        }
    }
}