using ManagementFile.Contracts.DTOs.ProjectManagement;
using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.Requests.ProjectManagement.TaskComments
{
    /// <summary>
    /// Response wrapper cho TaskComment operations
    /// </summary>
    public class TaskCommentResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public TaskCommentDto Data { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    /// <summary>
    /// Response cho danh sách TaskComment với pagination
    /// </summary>
    public class TaskCommentsPagedResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public List<TaskCommentDto> Data { get; set; } = new List<TaskCommentDto>();
        public PaginationMetadata Pagination { get; set; } = new PaginationMetadata();
        public List<string> Errors { get; set; } = new List<string>();
    }

    /// <summary>
    /// Metadata cho pagination
    /// </summary>
    public class PaginationMetadata
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public long TotalCount { get; set; }
        public bool HasPrevious { get; set; }
        public bool HasNext { get; set; }
    }
}
