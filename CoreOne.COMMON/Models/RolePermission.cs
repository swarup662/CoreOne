namespace CoreOne.COMMON.Models
{
 
    public class RolePermission
    {
        public int RoleID { get; set; }
        public int ModuleID { get; set; }
        public int ActionID { get; set; }
        public int CreatedBy { get; set; }
    }
}
