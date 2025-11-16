using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreOne.DOMAIN.Models
{
    public class Action
    {
        public int ActionID { get; set; }
        public string ActionName { get; set; }
        public string Description { get; set; }
        public bool ActiveFlag { get; set; }
    }

    public class ActionDto
    {
        public int ActionID { get; set; }      // must match JSON property "ActionID"
        public string? ActionName { get; set; } // must match JSON property "ActionName"
        public int ModuleId { get; set; }      // optional
    }


    public class HasPermissionRequest
    {
        public int? UserID { get; set; }
        public int? CurrentApplicationID { get; set; }
        public int? CurrentCompanyID { get; set; }
        public int? CurrentRoleID { get; set; }
        public int MenuModuleID { get; set; }
        public int ActionID { get; set; }
    }







    public class ActionCreationDto
    {
        public int ActionID { get; set; }
        public string? ActionName { get; set; }
        public string? Description { get; set; }
        public int? ActiveFlag { get; set; }
        public int? CreatedBy { get; set; }
        public string? CreatedDate { get; set; }
    }





}
