namespace CoreOne.UI.Helper
{
    public class ApiSettingsHelper
    {
        public string BaseUrlAuthentication { get; set; }
        public string BaseUrlUser { get; set; }
        public string BaseUrlPermission { get; set; }

        public string BaseUrlRoleCreation { get; set; }

        public string BaseUrlRolePermission { get; set; }

        public string BaseUrlUserCreation { get; set; }
        public string BaseUrlModuleSetup { get; set; }
        public string BaseUrlActionCreation { get; set; }

    }

    public class SettingsService
    {
        private readonly IConfiguration _configuration;
        public ApiSettingsHelper ApiSettings { get; }

        public SettingsService(IConfiguration configuration)
        {
            _configuration = configuration;
            ApiSettings = _configuration.GetSection("ApiLinks").Get<ApiSettingsHelper>();
        }
    }

}
