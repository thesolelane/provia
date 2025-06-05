using OpenAI;
using System.Text.Json;
using JobTracker.Models;
using JobTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Services
{
    public class AIMessagingService : IAIMessagingService
    {
        private readonly OpenAI.OpenAIClient _openAiClient;
        private readonly ILogger<AIMessagingService> _logger;
        private readonly JobTrackerContext _context;

        public AIMessagingService(ILogger<AIMessagingService> logger, JobTrackerContext context)
        {
            _logger = logger;
            _context = context;
            
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (!string.IsNullOrEmpty(apiKey))
            {
                _openAiClient = new OpenAI.OpenAIClient(apiKey);
            }
        }

        public async Task<string> GenerateVerificationMessageAsync(string employeeName, string verificationCode)
        {
            if (_openAiClient == null)
            {
                return $"Hi {employeeName}, your Job Tracker verification code is: {verificationCode}. This code expires in 10 minutes.";
            }

            try
            {
                var prompt = $"Generate a professional but friendly SMS verification message for employee {employeeName}. Include verification code {verificationCode} and mention it expires in 10 minutes. Keep it under 160 characters. Company name is Job Tracker.";

                var response = await _openAiClient.Chat.Completions.CreateAsync(new OpenAI.Chat.ChatCompletionOptions
                {
                    Model = "gpt-4o", // the newest OpenAI model is "gpt-4o" which was released May 13, 2024. do not change this unless explicitly requested by the user
                    Messages = { new OpenAI.Chat.ChatMessage(OpenAI.Chat.ChatMessageRole.User, prompt) },
                    MaxTokens = 100
                });

                return response.Value.Content[0].Text ?? $"Hi {employeeName}, your Job Tracker verification code is: {verificationCode}. Expires in 10 minutes.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI verification message");
                return $"Hi {employeeName}, your Job Tracker verification code is: {verificationCode}. This code expires in 10 minutes.";
            }
        }

        public async Task<string> GenerateJobUpdateMessageAsync(Job job, string updateType)
        {
            if (_openAiClient == null)
            {
                return $"Job Update - {job.JobName}: {updateType}. Job #{job.JobNumber}";
            }

            try
            {
                var prompt = $"Generate a concise SMS message about job update. Job: {job.JobName} (#{job.JobNumber}), Update: {updateType}, Status: {job.Status}. Keep professional, under 160 characters.";

                var response = await _openAiClient.Chat.Completions.CreateAsync(new OpenAI.Chat.ChatCompletionOptions
                {
                    Model = "gpt-4o", // the newest OpenAI model is "gpt-4o" which was released May 13, 2024. do not change this unless explicitly requested by the user
                    Messages = { new OpenAI.Chat.ChatMessage(OpenAI.Chat.ChatMessageRole.User, prompt) },
                    MaxTokens = 100
                });

                return response.Value.Content[0].Text ?? $"Job Update - {job.JobName}: {updateType}. Job #{job.JobNumber}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI job update message");
                return $"Job Update - {job.JobName}: {updateType}. Job #{job.JobNumber}";
            }
        }

        public async Task<string> GenerateScheduleReminderAsync(string employeeName, Job job, DateTime scheduledTime)
        {
            if (_openAiClient == null)
            {
                return $"Hi {employeeName}, reminder: You're scheduled for {job.JobName} today at {scheduledTime:HH:mm}. Job #{job.JobNumber}";
            }

            try
            {
                var prompt = $"Generate a friendly SMS reminder for employee {employeeName} scheduled to work on {job.JobName} at {scheduledTime:HH:mm} today. Include job #{job.JobNumber}. Keep under 160 characters.";

                var response = await _openAiClient.Chat.Completions.CreateAsync(new OpenAI.Chat.ChatCompletionOptions
                {
                    Model = "gpt-4o", // the newest OpenAI model is "gpt-4o" which was released May 13, 2024. do not change this unless explicitly requested by the user
                    Messages = { new OpenAI.Chat.ChatMessage(OpenAI.Chat.ChatMessageRole.User, prompt) },
                    MaxTokens = 100
                });

                return response.Value.Content[0].Text ?? $"Hi {employeeName}, reminder: You're scheduled for {job.JobName} today at {scheduledTime:HH:mm}. Job #{job.JobNumber}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI schedule reminder");
                return $"Hi {employeeName}, reminder: You're scheduled for {job.JobName} today at {scheduledTime:HH:mm}. Job #{job.JobNumber}";
            }
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