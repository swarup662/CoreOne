using CoreOne.DOMAIN.Models;

namespace CoreOne.API.Interfaces
{
    public interface IAuthRepository
    {
        (bool Success, string Message, User User, string Token, List<UserAccessViewModel> AccessList) Login(string userName, string password, string ipAddress, string userAgent);
        (bool Ok, string Message, string RedirectUrl) CreateCacheKeyAndGetRedirectUrl(int userId, int companyId, int appId, int roleId, string? sourceIp, string urlType = "domain");
        (bool Ok, string Message, string Token) ExchangeCacheKeyForToken(string cacheKey, int appId, string? callerIp, string userAgent);
        int Logout(int userID, string ipAddress, string userAgent);
        int LogHttpError(LogHttpErrorRequest request);
        int LogException(LogExceptionRequest request);




        PasswordValidationResponse ChangePassword(int? userId, string currentPwd, string newPwd);
        PasswordValidationResponse ForgotPassword(string email);
        PasswordValidationResponse ValidateResetToken(string token);
        PasswordValidationResponse ResetPassword(int? userId, string newPwd, string token);

    }
}
