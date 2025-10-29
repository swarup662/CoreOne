using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreOne.DOMAIN.Models
{
    public class MailSetting
    {
        public int MailSettingID { get; set; }
        public string Provider { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public bool EnableSSL { get; set; }
        public bool Active { get; set; }
    }

}
