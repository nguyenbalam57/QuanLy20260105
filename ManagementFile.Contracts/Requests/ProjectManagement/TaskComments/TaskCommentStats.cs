using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.TaskComments
{
    /// <summary>
    /// DTO cho thống kê TaskComment
    /// </summary>
    public class TaskCommentStats
    {
        public int TaskId { get; set; }
        public int TotalComments { get; set; }
        public int PendingComments { get; set; }
        public int ResolvedComments { get; set; }
        public int BlockingComments { get; set; }
        public int VerifiedComments { get; set; }

        public Dictionary<CommentType, int> CommentsByType { get; set; } = new Dictionary<CommentType, int>();
        public Dictionary<TaskStatuss, int> CommentsByStatus { get; set; } = new Dictionary<TaskStatuss, int>();
        public Dictionary<TaskPriority, int> CommentsByPriority { get; set; } = new Dictionary<TaskPriority, int>();

        public decimal TotalEstimatedTime { get; set; }
        public decimal TotalActualTime { get; set; }
        public int OverdueComments { get; set; }
    }
}
