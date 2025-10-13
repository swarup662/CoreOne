using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreOne.COMMON.Models
{
    public class Module
    {
        public int ModuleID { get; set; }
        public string ModuleName { get; set; }
        public bool IsModule { get; set; } // 1 = menu, 0 = actual module
        public bool ActiveFlag { get; set; }
        public int CreatedBy { get; set; }
    }
}
