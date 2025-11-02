using CoreOne.API.Infrastructure.Data;
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
        private readonly DBContext _dbHelper;


        public EmailHelper(IConfiguration configuration, DBContext dbHelper)
        {
            _configuration = configuration;
            _dbHelper = dbHelper;
        }

        private (string Host, int Port, string Email, string Password) GetMailSettings(int MailTypeId)
        {
            var parameters = new Dictionary<string, object>
            {
                {"@MailTypeId ", MailTypeId},
               
            };

            var dt = _dbHelper.ExecuteSP_ReturnDataTable("[sp_Auth_GetMailSettings]", parameters);

            if (dt.Rows.Count == 0)
                throw new Exception($"Mail settings for provider ':{MailTypeId}' not found in database.");

            var row = dt.Rows[0];

            string host = row["Host"].ToString();
            int port = Convert.ToInt32(row["Port"]);
            string email = "";
            string password = "";

            return (host, port, email, password);
        }




        public async Task<bool> SendEmailAsyncToIndividual(int MailTypeId, string toEmail, string subject, string body,string FromMail, string FromMailPassword)
        {
            try
            {
                var (host, port, email, password) = GetMailSettings(MailTypeId);

                using var smtp = new SmtpClient
                {
                    Host = host,
                    Port = port,
                    EnableSsl = true,
                    Credentials = new NetworkCredential(FromMail, FromMailPassword)
                };

                var mail = new MailMessage
                {
                    From = new MailAddress(FromMail, "CoreOne System"),
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

        // ✅ Classes for strong binding
        public class MailSettings
        {
            public MailProvider Gmail { get; set; }
            public MailProvider Yahoo { get; set; }
            public MailProvider Outlook { get; set; }
        }

        public class MailProvider
        {
            public string Host { get; set; }
            public string Port { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
        }
    }

}
