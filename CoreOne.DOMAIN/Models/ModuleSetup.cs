using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreOne.DOMAIN.Models
{
    public class ModuleSetup
    {
        public int MenuModuleID { get; set; }
        public string Name { get; set; }
        public int? ParentID { get; set; }
        public bool IsModule { get; set; }
        public bool ActiveFlag { get; set; }
    }

    public class MenuModuleDto
    {
        public int MenuModuleID { get; set; }
        public string MenuName { get; set; }
        public string MenuSymbol { get; set; }
        public string Modules { get; set; } // comma-separated submenu names
        public int Sequence { get; set; }
        public int ActiveFlag { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class MenuModulePagedResponse
    {
        public IEnumerable<MenuModuleDto> Items { get; set; }
        public int TotalRecords { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string SortColumn { get; set; }
        public string SortDir { get; set; }
        public string Search { get; set; }
        public string SearchCol { get; set; }
    }
    public class ModuleItemSave
    {
        public int? ModuleID { get; set; } // existing module ID or null for insert
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public int Sequence { get; set; }
    }

    public class MenuWithModulesSave
    {
        public int? MenuModuleID { get; set; }   // Menu ID, null if new
        public string Name { get; set; } = string.Empty;
        public string? MenuSymbol { get; set; }
        public int Sequence { get; set; }
        public char RecType { get; set; }        // 'I' = Insert, 'U' = Update
        public int? CreatedBy { get; set; }

        public List<ModuleItemSave> Modules { get; set; } = new List<ModuleItemSave>();
    }



    public class MenuModuleEditModel
    {
        public int? MenuID { get; set; }        // Hidden field for edit
        public string MenuName { get; set; }
        public int MenuSeq { get; set; }
        public string MenuSymbol { get; set; }
        public List<ModuleItem> Modules { get; set; } = new List<ModuleItem>();
    }

    public class ModuleItem
    {
        public int ModuleID { get; set; }       // ✅ new field added
        public string Name { get; set; }
        public string Url { get; set; }
        public int Sequence { get; set; }
        public int ParentID { get; set; }
    }
    // Strongly-typed DTO
    public class ActionDropdownDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    public class SaveActionModel
    {
        public int ModuleID { get; set; }           // must match JSON property "ModuleID"
        public List<ActionDto>? Actions { get; set; } // must match JSON property "Actions"
        public int CreatedBy { get; set; }          // must match JSON property "CreatedBy"
    }



}
