using CoreOne.DOMAIN.Models;

namespace CoreOne.API.Interfaces
{
    public interface IAuthenticationRepository
    {
        (bool Success, string Message, User User, string Token) Login(string userName, string password, string ipAddress);
        int Logout(int userID, string ipAddress);
        int LogHttpError(LogHttpErrorRequest request);
        int LogException(LogExceptionRequest request);




        (bool Success, string Message) ChangePassword(int? userId, string currentPwd, string newPwd);
        (bool Success, string Message) ForgotPassword(string email);
        (bool Success, string Message, int UserID) ValidateResetToken(string token);
        (bool Success, string Message) ResetPassword(int? userId, string newPwd, string token);

    }
}
