using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreOne.DOMAIN.Models
{
    public class UserPermission
    {
        public int UserID { get; set; }
        public int ModuleID { get; set; }
        public int ActionID { get; set; }
        public int CreatedBy { get; set; }
    }
}
