namespace CoreOne.API.Interfaces
{
    public interface IEmailHelper
    {
        bool SendEmail(string provider, string toEmail, string subject, string body);
        Task<bool> SendEmailAsync(string provider, string toEmail, string subject, string body);
    }
}
