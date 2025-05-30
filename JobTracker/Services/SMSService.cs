using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace JobTracker.Services
{
    public class SMSService
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

            if (!string.IsNullOrEmpty(_accountSid) && !string.IsNullOrEmpty(_authToken))
            {
                TwilioClient.Init(_accountSid, _authToken);
            }
        }

        public async Task<bool> SendSMSAsync(string toPhoneNumber, string message)
        {
            try
            {
                if (string.IsNullOrEmpty(_accountSid) || string.IsNullOrEmpty(_authToken) || string.IsNullOrEmpty(_fromPhoneNumber))
                {
                    _logger.LogWarning("Twilio credentials not configured. SMS not sent.");
                    return false;
                }

                // Clean phone number format
                var cleanPhoneNumber = CleanPhoneNumber(toPhoneNumber);
                if (string.IsNullOrEmpty(cleanPhoneNumber))
                {
                    _logger.LogWarning($"Invalid phone number format: {toPhoneNumber}");
                    return false;
                }

                var messageResource = await MessageResource.CreateAsync(
                    body: message,
                    from: new Twilio.Types.PhoneNumber(_fromPhoneNumber),
                    to: new Twilio.Types.PhoneNumber(cleanPhoneNumber)
                );

                _logger.LogInformation($"SMS sent successfully to {cleanPhoneNumber}. SID: {messageResource.Sid}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send SMS to {toPhoneNumber}: {ex.Message}");
                return false;
            }
        }

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