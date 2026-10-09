using JobTracker.Models;

namespace JobTracker.Services
{
    public interface IMessagingService
    {
        Task<bool> SendSmsAsync(string phoneNumber, string message);
        Task<bool> SendVerificationSmsAsync(string phoneNumber, string verificationCode);
        Task<bool> SendJobUpdateSmsAsync(string phoneNumber, Job job, string updateMessage);
        Task<bool> SendScheduleReminderAsync(string phoneNumber, string employeeName, Job job, DateTime scheduledTime);
    }

    public interface IWhatsAppService
    {
        Task<bool> SendWhatsAppMessageAsync(string phoneNumber, string message);
        Task<string> ProcessIncomingMessageAsync(string fromNumber, string messageContent);
        Task<bool> SetupWebhookAsync(string webhookUrl);
    }

    public interface IAIMessagingService
    {
        Task<string> GenerateVerificationMessageAsync(string employeeName, string verificationCode);
        Task<string> GenerateJobUpdateMessageAsync(Job job, string updateType);
        Task<string> GenerateScheduleReminderAsync(string employeeName, Job job, DateTime scheduledTime);
        Task<string> ProcessIncomingQueryAsync(string query, int? jobId = null);
        Task<string> GenerateResponseToWhatsAppMessageAsync(string fromNumber, string messageContent);
    }

    public class MessageTemplate
    {
        public string Type { get; set; } = string.Empty;
        public string Template { get; set; } = string.Empty;
        public Dictionary<string, string> Variables { get; set; } = new();
    }

    public class WhatsAppMessage
    {
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string MessageId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}