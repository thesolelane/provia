using System;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using JobTracker.Models;

namespace JobTracker.Services
{
    public interface INotificationService
    {
        Task<bool> SendWhatsAppNotification(string phoneNumber, string message);
    }

    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ILogger<NotificationService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> SendWhatsAppNotification(string phoneNumber, string message)
        {
            try
            {
                // Create a Python script for WhatsApp integration using Twilio
                string scriptPath = Path.Combine(Path.GetTempPath(), "send_whatsapp.py");
                string scriptContent = @"
import os
import sys
from twilio.rest import Client

# Get environment variables
account_sid = os.environ.get('TWILIO_ACCOUNT_SID')
auth_token = os.environ.get('TWILIO_AUTH_TOKEN')
twilio_phone = os.environ.get('TWILIO_PHONE_NUMBER')

if not account_sid or not auth_token or not twilio_phone:
    print('Missing required Twilio credentials')
    sys.exit(1)

# Get phone number and message from arguments
phone_number = sys.argv[1]
message = sys.argv[2]

try:
    # Initialize Twilio client
    client = Client(account_sid, auth_token)
    
    # Format WhatsApp number (add whatsapp: prefix)
    whatsapp_number = f'whatsapp:{phone_number}'
    twilio_whatsapp = f'whatsapp:{twilio_phone}'
    
    # Send message
    message = client.messages.create(
        from_=twilio_whatsapp,
        body=message,
        to=whatsapp_number
    )
    
    print(f'Message sent successfully: {message.sid}')
    sys.exit(0)
except Exception as e:
    print(f'Error sending WhatsApp message: {str(e)}')
    sys.exit(1)
";

                await File.WriteAllTextAsync(scriptPath, scriptContent);

                // Create process to run the Python script
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "python",
                        Arguments = $"{scriptPath} {phoneNumber} \"{message}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    _logger.LogError($"Failed to send WhatsApp notification: {error}");
                    return false;
                }

                _logger.LogInformation($"WhatsApp notification sent: {output}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WhatsApp notification");
                return false;
            }
        }
    }
}