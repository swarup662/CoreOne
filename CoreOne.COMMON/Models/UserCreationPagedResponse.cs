using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreOne.COMMON.Models
{
    public class UserCreationPagedResponse
    {
        public List<UserCreation> Items { get; set; } = new();
        public int TotalRecords { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string? SortColumn { get; set; }
        public string? SortDir { get; set; }
        public string? Search { get; set; }
        public string? SearchCol { get; set; }

        public int TotalPages => PageSize > 0
            ? (int)Math.Ceiling(TotalRecords / (double)PageSize)
            : 0;
    }

    public class UserCreationRequest
    {
        public int PageSize { get; set; } = 10;
        public int PageNumber { get; set; } = 1;
        public string? Search { get; set; }
        public string? SortColumn { get; set; }
        public string? SortDir { get; set; }
        public string? SearchCol { get; set; }
        public string? Status { get; set; }
    }

}
