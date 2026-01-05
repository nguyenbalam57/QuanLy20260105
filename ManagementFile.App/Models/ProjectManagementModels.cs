using ManagementFile.App.Models.Projects;
using ManagementFile.Contracts.DTOs.ProjectManagement;
using ManagementFile.Contracts.DTOs.UserManagement;
using ManagementFile.Contracts.Enums;
using ManagementFile.Contracts.Enums.Extensions;
using ManagementFile.Contracts.Requests.ProjectManagement.TaskComments;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Windows.Media;

namespace ManagementFile.App.Models
{


    #region Dashboard Models

    /// <summary>
    /// Project Dashboard Model
    /// </summary>
    public class ProjectDashboardModel : INotifyPropertyChanged
    {
        public int ProjectId { get; set; } = -1;
        public string ProjectName { get; set; } = "";
        public ProjectStatus Status { get; set; }
        public decimal CompletionPercentage { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int TotalMembers { get; set; }
        public int TotalFiles { get; set; }
        public decimal TotalHours { get; set; }
        public List<ProjectTaskModel> RecentTasks { get; set; } = new List<ProjectTaskModel>();

        /// <summary>
        /// Completion percentage text
        /// </summary>
        public string CompletionPercentageText
        {
            get
            {
                return $"{CompletionPercentage:F1}%";
            }
        }

        /// <summary>
        /// Tasks completion ratio
        /// </summary>
        public string TasksCompletionText
        {
            get
            {
                return $"{CompletedTasks}/{TotalTasks}";
            }
        }

        /// <summary>
        /// Progress color based on completion percentage
        /// </summary>
        public Brush ProgressColor
        {
            get
            {
                if (CompletionPercentage >= 90) return new SolidColorBrush(Color.FromRgb(39, 174, 96)); // Green
                if (CompletionPercentage >= 70) return new SolidColorBrush(Color.FromRgb(52, 152, 219)); // Blue
                if (CompletionPercentage >= 40) return new SolidColorBrush(Color.FromRgb(243, 156, 18)); // Orange
                return new SolidColorBrush(Color.FromRgb(231, 76, 60)); // Red
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }


    #endregion

    #region Service Layer DTOs

    /// <summary>
    /// Project Dashboard DTO
    /// </summary>
    public class ProjectDashboardDto
    {
        public int ProjectId { get; set; } 
        public string ProjectName { get; set; } = "";
        public ProjectStatus Status { get; set; }
        public decimal CompletionPercentage { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int TotalMembers { get; set; }
        public int TotalFiles { get; set; }
        public decimal TotalHours { get; set; }
        public List<ProjectTaskDto> RecentTasks { get; set; } = new List<ProjectTaskDto>();
    }



    /// <summary>
    /// Model for pausing project
    /// </summary>
    public class PauseProjectModel
    {
        public string Reason { get; set; } = "";
    }

    /// <summary>
    /// Model for updating progress
    /// </summary>
    public class UpdateProgressModel
    {
        public decimal Percentage { get; set; }
    }

    /// <summary>
    /// Paginated result wrapper for service layer
    /// </summary>
    public class ProjectManagementPagedResult<T>
    {
        public List<T> Data { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }

        /// <summary>
        /// Pagination metadata object
        /// Tương thích với API responses
        /// </summary>
        public PaginationMetadata Pagination
        {
            get
            {
                return new PaginationMetadata
                {
                    TotalCount = TotalCount,
                    PageNumber = PageNumber,
                    PageSize = PageSize,
                    TotalPages = TotalPages,
                    HasNextPage = HasNextPage,
                    HasPreviousPage = HasPreviousPage
                };
            }
        }
    }

    /// <summary>
    /// Pagination metadata class
    /// </summary>
    public class PaginationMetadata
    {
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    #endregion


    #region Paging and Common Models

    /// <summary>
    /// Generic paged result model for UI
    /// </summary>
    public class PagedResultModel<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }

        // UI Helper Properties
        public string PagingInfo => $"Page {PageNumber} of {TotalPages} ({TotalCount} total items)";
        public string ResultRange => 
            $"{((PageNumber - 1) * PageSize) + 1}-{Math.Min(PageNumber * PageSize, TotalCount)} of {TotalCount}";
    }

    #endregion

    #region Notification Models

    /// <summary>
    /// Model for notification
    /// </summary>
    public class NotificationModel : INotifyPropertyChanged
    {
        private bool _isRead;

        public int Id { get; set; } 
        public int UserId { get; set; } 
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public string Type { get; set; } = "";
        
        public bool IsRead
        {
            get => _isRead;
            set => SetProperty(ref _isRead, value);
        }

        public DateTime? ReadAt { get; set; }
        public string RelatedEntityType { get; set; } = "";
        public string RelatedEntityId { get; set; } = "";
        public string ActionUrl { get; set; } = "";
        public DateTime? ExpiresAt { get; set; }
        public bool IsExpired { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = "";

        // UI Helper Properties
        public string TimeAgo => GetTimeAgo(CreatedAt);
        public string TypeDisplay => Type;

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;
            
            if (timeSpan.Days > 0)
                return $"{timeSpan.Days} day{(timeSpan.Days > 1 ? "s" : "")} ago";
            if (timeSpan.Hours > 0)
                return $"{timeSpan.Hours} hour{(timeSpan.Hours > 1 ? "s" : "")} ago";
            if (timeSpan.Minutes > 0)
                return $"{timeSpan.Minutes} minute{(timeSpan.Minutes > 1 ? "s" : "")} ago";
            
            return "Just now";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    #endregion
}