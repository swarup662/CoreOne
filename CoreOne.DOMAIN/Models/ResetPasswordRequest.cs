using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreOne.DOMAIN.Models
{
    public class ResetPasswordRequest { public int? UserID { get; set; } public string NewPassword { get; set; } public string Token { get; set; } }
    public class ChangePasswordRequest { public int? UserID { get; set; } public string CurrentPassword { get; set; } public string NewPassword { get; set; } }

}
