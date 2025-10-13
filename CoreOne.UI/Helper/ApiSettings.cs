namespace CoreOne.UI.Helper
{
    public class ApiSettings
    {
        public string BaseUrlAuth { get; set; }
        public string BaseUrlUser { get; set; }
        public string BaseUrlPermission { get; set; }

        public string BaseUrlRole { get; set; }

        public string BaseUrlRolePermission { get; set; }

        public string BaseUrlUserCreation { get; set; }
        public string BaseUrlMenuModule { get; set; }

    }

    public class SettingsService
    {
        private readonly IConfiguration _configuration;
        public ApiSettings ApiSettings { get; }

        public SettingsService(IConfiguration configuration)
        {
            _configuration = configuration;
            ApiSettings = _configuration.GetSection("ApiLinks").Get<ApiSettings>();
        }
    }

}
