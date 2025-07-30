using System.Threading.Tasks;

namespace StackFlow.Utils
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}