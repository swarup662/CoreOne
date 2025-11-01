namespace CoreOne.API.Interfaces
{
    public interface IEmailHelper
    {
       
        Task<bool> SendEmailAsyncToIndividual(int MailTypeId, string toEmail, string subject, string body, string FromMail, string FromMailPassword);
    }
}
