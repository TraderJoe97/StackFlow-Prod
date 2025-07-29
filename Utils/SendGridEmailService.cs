using SendGrid;
using SendGrid.Helpers.Mail;
using StackFlow.Services;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration; // Added for IConfiguration
using System; // Added for Exception

namespace StackFlow.Utils // Assuming this is where your IEmailService is located
{
    public class SendGridEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public SendGridEmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
        {
            var apiKey = _configuration["SendGrid:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                // Log an error or throw an exception if the API key is not configured
                Console.WriteLine("SendGrid API key is not configured.");
                // Depending on your application's requirements, you might throw an exception
                // throw new InvalidOperationException("SendGrid API key is not configured.");
                return; 
            }

            var client = new SendGridClient(apiKey);
            // Replace with your verified sender email address in SendGrid
            var from = new EmailAddress("yourverifiedemail@yourdomain.com", "Your App Name"); 
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlContent);

            try
            {
                var response = await client.SendEmailAsync(msg);
                // You can check the response.StatusCode for success or failure
                if (!response.IsSuccessStatusCode)
                {
                     // Log an error if the email sending failed
                    Console.WriteLine($"Error sending email via SendGrid. Status Code: {response.StatusCode}");
                    // Optionally, read the response body for more details
                    // var responseBody = await response.Body.ReadAsStringAsync();
                    // Console.WriteLine($"Response Body: {responseBody}");
                }
            }
            catch (Exception ex)
            {
                 // Log any exceptions that occur during email sending
                Console.WriteLine($"An error occurred while sending email via SendGrid: {ex.Message}");
            }
        }
    }
}