using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace StackFlow.Utils
{
    public class MailgunEmailService : IEmailService
    {
        private readonly string _apiKey;
        private readonly string _domain;
        private readonly string _fromEmail;

        public MailgunEmailService(string apiKey, string domain, string fromEmail)
        {
            _apiKey = apiKey;
            _domain = domain;
            _fromEmail = fromEmail;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes($"api:{_apiKey}")));

                var form = new Dictionary<string, string>
                {
                    {"from", _fromEmail},
                    {"to", toEmail},
                    {"subject", subject},
                    {"html", body}
                };

                var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.mailgun.net/v3/{_domain}/messages")
                {
                    Content = new FormUrlEncodedContent(form)
                };

                var response = await httpClient.SendAsync(request);

                // You might want to add more robust error handling here
                response.EnsureSuccessStatusCode();
            }
        }
    }
}