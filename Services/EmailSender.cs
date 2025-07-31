using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;

namespace PawVerse.Services
{
    // Class to hold SendGrid settings from appsettings.json
    public class AuthMessageSenderOptions
    {
        public string ApiKey { get; set; }
        public string FromEmail { get; set; }
        public string FromName { get; set; }
    }

    public class EmailSender : IEmailSender
    {
        private readonly ILogger<EmailSender> _logger;
        public readonly AuthMessageSenderOptions Options; //Set with Secret Manager.

        public EmailSender(IOptions<AuthMessageSenderOptions> optionsAccessor, ILogger<EmailSender> logger)
        {
            Options = optionsAccessor.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            if (string.IsNullOrEmpty(Options.ApiKey) || Options.ApiKey == "PASTE_YOUR_SENDGRID_API_KEY_HERE")
            {
                _logger.LogError("SendGrid ApiKey is not configured. Please paste it into appsettings.json.");
                // In a real app, you might want to throw an exception or handle this more gracefully.
                return; 
            }
            await Execute(Options.ApiKey, subject, message, toEmail);
        }

        private async Task Execute(string apiKey, string subject, string message, string toEmail)
        {
            var client = new SendGridClient(apiKey);
            var msg = new SendGridMessage()
            {
                From = new EmailAddress(Options.FromEmail, Options.FromName),
                Subject = subject,
                PlainTextContent = message, // SendGrid recommends providing both plain text and HTML content
                HtmlContent = message
            };
            msg.AddTo(new EmailAddress(toEmail));

            // Disable click tracking.
            // See https://sendgrid.com/docs/User_Guide/Settings/tracking.html
            msg.SetClickTracking(false, false);
            var response = await client.SendEmailAsync(msg);

            string responseBodyString = await response.Body.ReadAsStringAsync(); // Read body once to avoid issues
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email to {ToEmail} queued successfully! Status Code: {StatusCode}. Response Body: {Body}. Response Headers: {Headers}", 
                                     toEmail, response.StatusCode, responseBodyString, response.Headers.ToString());
            }
            else
            {
                _logger.LogError("Failed to send email to {ToEmail}. Status Code: {StatusCode}, Body: {Body}", 
                                 toEmail, response.StatusCode, responseBodyString);
            }
        }
    }
}
