using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreOne.COMMON.Models
{
    public class LoginModel
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    public class LoginViewModel
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
