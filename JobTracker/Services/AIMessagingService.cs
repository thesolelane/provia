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
                $"Job Update: {job.JobName} - {updateType}. Status: {job.Status}. Job #{job.JobNumber}",
                $"{job.JobName} ({job.JobNumber}): {updateType}. Current status: {job.Status}",
                $"Update on Job #{job.JobNumber} - {job.JobName}: {updateType}",
                $"{updateType} - {job.JobName}. Job #{job.JobNumber} now {job.Status}"
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
                $"Hi {firstName}! Reminder: {job.JobName} today at {timeFormatted}. Job #{job.JobNumber}",
                $"{firstName}, you're scheduled for {job.JobName} at {timeFormatted} today. Job #{job.JobNumber}",
                $"Work reminder: {job.JobName} - {timeFormatted} today, {firstName}. Job #{job.JobNumber}",
                $"{firstName}: {job.JobName} today {timeFormatted}. Job #{job.JobNumber}"
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
            if (_openAiClient == null)
            {
                return "AI assistant is not available at the moment. Please contact your supervisor for assistance.";
            }

            try
            {
                // Get relevant job data if jobId is provided
                string contextData = "";
                if (jobId.HasValue)
                {
                    var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId.Value);
                    if (job != null)
                    {
                        contextData += $"Job: {job.JobName} (#{job.JobNumber}), Status: {job.Status}, Address: {job.Address}, Start: {job.StartDate:MM/dd/yyyy}";
                    }
                }

                var systemPrompt = @"You are a construction job tracking assistant. Help with job status, schedules, building codes, and general construction questions. 
                Keep responses concise and professional. If asked about specific job details you don't have access to, suggest contacting the supervisor.
                For building code questions, refer to Massachusetts building codes when relevant.";

                var userPrompt = string.IsNullOrEmpty(contextData) ? query : $"Context: {contextData}\n\nQuestion: {query}";

                var response = await _openAiClient.Chat.Completions.CreateAsync(new OpenAI.Chat.ChatCompletionOptions
                {
                    Model = "gpt-4o", // the newest OpenAI model is "gpt-4o" which was released May 13, 2024. do not change this unless explicitly requested by the user
                    Messages = { 
                        new OpenAI.Chat.ChatMessage(OpenAI.Chat.ChatMessageRole.System, systemPrompt),
                        new OpenAI.Chat.ChatMessage(OpenAI.Chat.ChatMessageRole.User, userPrompt)
                    },
                    MaxTokens = 300
                });

                return response.Value.Content[0].Text ?? "I'm unable to process that request right now. Please contact your supervisor.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing incoming query with AI");
                return "I'm experiencing technical difficulties. Please contact your supervisor for assistance.";
            }
        }

        public async Task<string> GenerateResponseToWhatsAppMessageAsync(string fromNumber, string messageContent)
        {
            // Extract job number or employee info from message content
            var jobNumberMatch = System.Text.RegularExpressions.Regex.Match(messageContent, @"#(\d+)");
            int? jobId = null;
            
            if (jobNumberMatch.Success && int.TryParse(jobNumberMatch.Groups[1].Value, out int jobNumber))
            {
                var job = await _context.Jobs.FirstOrDefaultAsync(j => j.JobNumber == jobNumber);
                jobId = job?.Id;
            }

            // Try to identify user by phone number
            var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == fromNumber);
            
            return await ProcessIncomingQueryAsync(messageContent, jobId, user?.Id);
        }
    }
}