using CoreOne.DOMAIN.Models;

namespace CoreOne.API.Interface
{
    public interface IActionCreationRepository
    {
        Task<List<ActionCreationDto>> GetActions();
        ActionCreationDto? GetActionById(int actionId);
        int SaveAction(string recType, int? ActionID, string ActionName, string Description, int userId);
    }
}
