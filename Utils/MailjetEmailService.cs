using Mailjet.Client;
using Mailjet.Client.TransactionalEmails;
using StackFlow.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration; // Added for IConfiguration
using System; // Added for Exception

namespace StackFlow.Utils // Assuming this is where your IEmailService is located
{
    public class MailjetEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public MailjetEmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
        {
            var apiKey = _configuration["MailjetSettings:ApiKey"];
            var apiSecret = _configuration["MailjetSettings:SecretKey"];

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            {
                // Log an error or throw an exception if the API keys are not configured
                Console.WriteLine("Mailjet API key or Secret key is not configured.");
                // throw new InvalidOperationException("Mailjet API key or Secret key is not configured.");
                return;
            }

            var client = new MailjetClient(apiKey, apiSecret);

            var email = new TransactionalEmail
            {
                From = new SendContact("yourverifiedemail@yourdomain.com", "Your App Name"), // Replace with your verified sender email and name
                Subject = subject,
                HtmlPart = htmlContent,
                To = new List<SendContact> { new SendContact(toEmail) }
            };

            try
            {
                var response = await client.SendTransactionalEmailsAsync(new List<TransactionalEmail> { email });

                // Check the response for success or failure
                if (response.Messages != null && response.Messages.Count > 0)
                {
                    if (response.Messages[0].Status != "success")
                    {
                         // Log an error if the email sending failed
                        Console.WriteLine($"Error sending email via Mailjet. Status: {response.Messages[0].Status}, Error: {response.Messages[0].Errors?.FirstOrDefault()?.ErrorMessage}");
                    }
                }
                else
                {
                     // Log an error if the response messages are unexpected
                     Console.WriteLine("Mailjet email sending response did not contain expected messages.");
                }
            }
            catch (Exception ex)
            {
                 // Log any exceptions that occur during email sending
                Console.WriteLine($"An error occurred while sending email via Mailjet: {ex.Message}");
            }
        }
    }
}