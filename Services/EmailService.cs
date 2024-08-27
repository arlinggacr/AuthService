using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace AuthService.Services
{
    public class EmailService
    {
        private readonly SmtpClient _smtpClient;

        public EmailService(IConfiguration configuration)
        {
            var smtpConfig = configuration.GetSection("Smtp");

            _smtpClient = new SmtpClient(smtpConfig["Host"]!, int.Parse(smtpConfig["Port"]!))
            {
                Credentials = new NetworkCredential(
                    smtpConfig["Username"]!,
                    smtpConfig["Password"]!
                ),
                EnableSsl = bool.Parse(smtpConfig["EnableSsl"]!)
            };
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress("arlingga02@gmail.com"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            await _smtpClient.SendMailAsync(mailMessage);
        }
    }
}
