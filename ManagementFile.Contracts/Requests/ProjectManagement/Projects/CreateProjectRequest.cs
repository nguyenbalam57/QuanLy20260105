using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.Projects
{
    /// <summary>
    /// CreateProjectRequest - Request để tạo mới project
    /// </summary>
    public class CreateProjectRequest : BaseProjectRequest
    {

        /// <summary>ID của Project Manager</summary>
        [Required(ErrorMessage = "Project Manager là bắt buộc")]
        public int ProjectManagerId { get; set; } 

        public int? ProjectParentId { get; set; }

        /// <summary>Ngày bắt đầu project</summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Validation logic cho CreateProjectRequest
        /// </summary>
        public bool IsValid(out List<string> errors)
        {
            errors = new List<string>();

            // Kiểm tra ngày
            if (StartDate.HasValue && PlannedEndDate.HasValue)
            {
                if (StartDate.Value >= PlannedEndDate.Value)
                {
                    errors.Add("Ngày bắt đầu phải nhỏ hơn ngày kết thúc dự kiến");
                }
            }

            return errors.Count == 0;
        }
    }
}
