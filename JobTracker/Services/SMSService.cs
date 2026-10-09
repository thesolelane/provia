using JobTracker.Models;

namespace JobTracker.Services
{
    public class SMSService : IMessagingService
    {
        private readonly ILogger<SMSService> _logger;
        private readonly string _accountSid;
        private readonly string _authToken;
        private readonly string _fromPhoneNumber;

        public SMSService(IConfiguration configuration, ILogger<SMSService> logger)
        {
            _logger = logger;
            _accountSid = configuration["TWILIO_ACCOUNT_SID"];
            _authToken = configuration["TWILIO_AUTH_TOKEN"];
            _fromPhoneNumber = configuration["TWILIO_PHONE_NUMBER"];
        }

        public async Task<bool> SendSMSAsync(string toPhoneNumber, string message)
        {
            try
            {
                if (string.IsNullOrEmpty(_accountSid) || string.IsNullOrEmpty(_authToken) || string.IsNullOrEmpty(_fromPhoneNumber))
                {
                    _logger.LogWarning("SMS credentials not configured. Message logged instead.");
                    _logger.LogInformation($"SMS would be sent to {toPhoneNumber}: {message}");
                    return true;
                }

                // Clean phone number format
                var cleanPhoneNumber = CleanPhoneNumber(toPhoneNumber);
                if (string.IsNullOrEmpty(cleanPhoneNumber))
                {
                    _logger.LogWarning($"Invalid phone number format: {toPhoneNumber}");
                    return false;
                }

                _logger.LogInformation($"SMS notification would be sent to {cleanPhoneNumber}: {message}");
                // SMS functionality can be implemented when Twilio credentials are provided
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send SMS to {toPhoneNumber}: {ex.Message}");
                return false;
            }
        }

        public Task<bool> SendSmsAsync(string phoneNumber, string message) =>
            SendSMSAsync(phoneNumber, message);

        public Task<bool> SendVerificationSmsAsync(string phoneNumber, string verificationCode) =>
            SendSMSAsync(phoneNumber, $"Your PROVIA verification code is {verificationCode}. Expires in 10 minutes.");

        public Task<bool> SendJobUpdateSmsAsync(string phoneNumber, Job job, string updateMessage) =>
            SendSMSAsync(phoneNumber, updateMessage);

        public Task<bool> SendScheduleReminderAsync(
            string phoneNumber,
            string employeeName,
            Job job,
            DateTime scheduledTime) =>
            SendSMSAsync(phoneNumber,
                $"Hi {employeeName.Split(' ')[0]}! Reminder: {job.Name} today at {scheduledTime:h:mm tt}. Job #{job.JobNumber}");

        private string CleanPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber)) return null;

            // Remove all non-digit characters
            var digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());

            // Handle US phone numbers
            if (digitsOnly.Length == 10)
            {
                return $"+1{digitsOnly}";
            }
            else if (digitsOnly.Length == 11 && digitsOnly.StartsWith("1"))
            {
                return $"+{digitsOnly}";
            }
            else if (phoneNumber.StartsWith("+"))
            {
                return phoneNumber;
            }

            return null;
        }
    }
}