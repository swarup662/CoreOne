using CoreOne.API.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace CoreOne.API.Helpers
{
    public class EmailHelper : IEmailHelper
    {
        private readonly IConfiguration _configuration;

        public EmailHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private (string Host, int Port, string Email, string Password) GetMailSettings(string provider)
        {
            var section = _configuration.GetSection($"MailSettings:{provider}");
            if (!section.Exists())
                throw new Exception($"Mail provider '{provider}' not found in configuration.");

            string host = section["Host"];
            int port = int.Parse(section["Port"]);
            string email = section["Email"];
            string password = section["Password"];

            return (host, port, email, password);
        }

        public bool SendEmail(string provider, string toEmail, string subject, string body)
        {
            try
            {
                var (host, port, email, password) = GetMailSettings(provider);

                using var smtp = new SmtpClient
                {
                    Host = host,
                    Port = port,
                    EnableSsl = true,
                    Credentials = new NetworkCredential(email, password)
                };

                var mail = new MailMessage
                {
                    From = new MailAddress(email, "CoreOne System"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mail.To.Add(toEmail);

                smtp.Send(mail);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SendEmailAsync(string provider, string toEmail, string subject, string body)
        {
            try
            {
                var (host, port, email, password) = GetMailSettings(provider);

                using var smtp = new SmtpClient
                {
                    Host = host,
                    Port = port,
                    EnableSsl = true,
                    Credentials = new NetworkCredential(email, password)
                };

                var mail = new MailMessage
                {
                    From = new MailAddress(email, "CoreOne System"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mail.To.Add(toEmail);

                await smtp.SendMailAsync(mail);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
