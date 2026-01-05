using ManagementFile.App.Models.Users;
using ManagementFile.App.Services;
using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagementFile.App.Models.Projects.PermissionProjects
{
    public class PermissionProject
    {
        /// <summary>
        /// kiểm tra user hiện tại có phải admin hoặc project manager không
        /// </summary>
        /// <returns></returns>
        public static bool HasPermissionManagerProject()
        {
            if(App.GetCurrentUserModel().IsAdmin || App.GetCurrentUserModel().IsProjectManager)
                return true;

            return false;
        }

        /// <summary>
        /// Kiểm tra user hiện tại có phải là quản lý của dự án không
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns></returns>
        public static async Task<bool> HasPermissionManagerProjectOfProject(int projectId)
        {
            if(HasPermissionManagerProject() == true)
                return true;

            var currentUser = App.GetCurrentUserModel();

            var projectApiUser = App.GetRequiredService<ProjectApiService>();

            var userManager = await projectApiUser.GetProjectMemberByIdAsync(projectId, currentUser.Id);
            var project = await projectApiUser.GetProjectByIdAsync(projectId);

            if (userManager != null && 
                currentUser != null && 
                project != null &&
                userManager.ProjectRole == UserRole.Manager && // kiểm tra có phải là quản lý dự án member không
                project.ProjectManagerId == currentUser.Id && // kiểm tra có phải là người quản lý dự án chính không
                project.CreatedBy == currentUser.Id // kiểm tra xem có phải là người tạo dự án không
                )
                return true;

            return false;
        }

        /// <summary>
        /// Kiểm tra xem user hiện tại có phải là leader của dự án không
        /// được quyền thêm project task để giao việc cho thành viên
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns></returns>
        public static async Task<bool> HasPermissionTeamLeaderProjectOfProject(int projectId)
        {
            var boolUser = await HasPermissionManagerProjectOfProject(projectId);

            if (boolUser == true)
                return true;

            var currentUser = App.GetCurrentUserModel();
            var userLeader = await App.GetRequiredService<ProjectApiService>().GetProjectMemberByIdAsync(projectId, currentUser.Id);

            if (userLeader != null && currentUser != null && userLeader.ProjectRole == UserRole.TeamLead)
                return true;

            return false;
        }

        /// <summary>
        /// kiểm tra xem user hiện tại có phải là người được thực hiện projectask không
        /// Có thể thay đổi những nội dung mà người thực hiện projectask có thể làm
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="projectTaskId"></param>
        /// <returns></returns>
        public static async Task<bool> HasPermissionAssignedProjectTask(int projectId, int projectTaskId)
        {
            // Kiểm tra team leader trước
            var isTeamLeader = await HasPermissionTeamLeaderProjectOfProject(projectId);
            if (isTeamLeader)
                return true;

            var currentUser = App.GetCurrentUserModel();
            if (currentUser == null)
                return false;

            var projectTask = await App.GetRequiredService<ProjectApiService>()
                .GetTaskByIdAsync(projectId, projectTaskId);

            if (projectTask == null)
                return false;

            // Kiểm tra user có trong danh sách assigned không
            // Xử lý cả 2 trường hợp: AssignedToId (single) và AssignedToIds (multiple)
            bool isAssignedToSingle = projectTask.AssignedToId.HasValue &&
                                       currentUser.Id == projectTask.AssignedToId.Value;

            bool isAssignedToMultiple = projectTask.AssignedToIds != null &&
                                        projectTask.AssignedToIds.Contains(currentUser.Id);

            return isAssignedToSingle || isAssignedToMultiple;
        }

        /// <summary>
        /// kiểm tra xem user hiện tại có phải là người báo cáo projectTask không
        /// Để thực hiện chỉnh sửa nội dung liên quan đến người báo cáo
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="projectTaskId"></param>
        /// <returns></returns>
        public static async Task<bool> HasPermissionReportProjectTask(int projectId, int projectTaskId)
        {
            if (await HasPermissionTeamLeaderProjectOfProject(projectId) == true)
                return true;

            var currentUser = App.GetCurrentUserModel();
            var projectTask = await App.GetRequiredService<ProjectApiService>().GetTaskByIdAsync(projectId, projectTaskId);

            // Bổ sung quyền trong Tag để có thể truy cập được

            if (currentUser != null &&
                projectTask != null &&
                currentUser.Id == projectTask.ReporterId)
                return true;


            return false;
        }

        /// <summary>
        /// Kiểm tra user hiện tại có phải là người thực hiện taskComment không
        /// Chỉ thực hiện những quyền thuộc trong phạm vi của người đó
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="projectTaskId"></param>
        /// <param name="taskCommentId"></param>
        /// <returns></returns>
        public static async Task<bool> HasPermissionAssignedTaskComment(int projectId, int projectTaskId, int taskCommentId)
        {
            if (await HasPermissionTeamLeaderProjectOfProject(projectId))
                return true;

            var currentUser = App.GetCurrentUserModel();
            var taskComment = await App.GetRequiredService<TaskCommentService>().GetTaskCommentByIdAsync(projectId, projectTaskId, taskCommentId);
            if (currentUser != null && taskComment != null && currentUser.Id == taskComment.AssignedToId)
                return true;

            return false;
        }

        /// <summary>
        /// Kiểm tra xem user hiện tại có phải là người dược xác nhận taskcomment không
        /// Chỉ thực hiện những quyền thuộc phạm vi của người đó
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="projectTaskId"></param>
        /// <param name="taskCommentId"></param>
        /// <returns></returns>
        public static async Task<bool> HasPermssionReviewTaskComment(int projectId, int projectTaskId, int taskCommentId)
        {
            if (await HasPermissionTeamLeaderProjectOfProject(projectId))
                return true;

            var currentUser = App.GetCurrentUserModel();
            var taskComment = await App.GetRequiredService<TaskCommentService>().GetTaskCommentByIdAsync(projectId, projectTaskId, taskCommentId);
            if (currentUser != null && taskComment != null && currentUser.Id == taskComment.ReviewerId)
                return true;

            return false;
        }



        public static async Task<bool> HasPermissionViewProject(int projectId)
        {
            if (await HasPermissionTeamLeaderProjectOfProject(projectId))
                return true;
            var currentUser = App.GetCurrentUserModel();
            var projectMembers = await App.GetRequiredService<ProjectApiService>().GetProjectMembersAsync(projectId);
            if (currentUser != null && projectMembers != null && projectMembers.Any(o => o.UserId == currentUser.Id && o.IsActive))
                return true;
            return false;
        }

       


    }
}
