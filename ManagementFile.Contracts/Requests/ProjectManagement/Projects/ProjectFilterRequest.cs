using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.Projects
{
    public class ProjectFilterRequest
    {
        public string SearchTerm { get; set; }
        public ProjectStatus? Status { get; set; }
        public int? ProjectManagerId { get; set; }
        public int? ProjectParentId { get; set; }
        public bool? IsActive { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SortBy { get; set; } = "CreatedAt";
        public string SortDirection { get; set; } = "desc";
    }

}
