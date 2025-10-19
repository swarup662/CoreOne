using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreOne.DOMAIN.Models
{
    public class MenuItemModel
    {
        public int MenuModuleID { get; set; }
        public string Name { get; set; }
        public string Url { get; set; } // Add URL column in DB for modules
        public List<MenuItemModel> SubMenu { get; set; } = new List<MenuItemModel>();
        public int Sequence { get; set; }
    }

    public class MenuItem
    {
        public int MenuID { get; set; }
        public string MenuName { get; set; }
        public string Url { get; set; }
        public int MenuSequence { get; set; }

        public int? ParentID { get; set; }
        public string ParentName { get; set; }
        public int? ParentSequence { get; set; }
        public string MenuSymbol { get; set; } = "";
    }

}
