using System.Data;

namespace CoreOne.API.Interface
{
    public interface IApplicationsRepository
    {
        DataTable GetAllApplications();
    }

}
