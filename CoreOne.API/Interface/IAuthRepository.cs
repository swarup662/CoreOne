using CoreOne.DOMAIN.Models;

namespace CoreOne.API.Interfaces
{
    public interface IAuthRepository
    {
        (bool Success, string Message, User User, string Token) Login(string userName, string password, string ipAddress);
        int Logout(int userID, string ipAddress);
        int LogHttpError(LogHttpErrorRequest request);
        int LogException(LogExceptionRequest request);




        PasswordValidationResponse ChangePassword(int? userId, string currentPwd, string newPwd);
        PasswordValidationResponse ForgotPassword(string email);
        PasswordValidationResponse ValidateResetToken(string token);
        PasswordValidationResponse ResetPassword(int? userId, string newPwd, string token);

    }
}
