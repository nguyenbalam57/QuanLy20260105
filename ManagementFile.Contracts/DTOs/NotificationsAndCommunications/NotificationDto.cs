using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.NotificationsAndCommunications
{
    /// <summary>
    /// Notification DTO
    /// </summary>
    public class NotificationDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public NotificationType Type { get; set; }
        public string Url { get; set; } = "";
        public string Data { get; set; } = "";
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime? ScheduledAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = "";
        public int RecipientId { get; set; } 
        public string RecipientName { get; set; } = "";
        public int UserId { get; set; } 
        public string Content { get; set; } = "";
        public string RelatedEntityType { get; set; } = "";
        public int? RelatedEntityId { get; set; } 
        public string ActionUrl { get; set; } = "";
        public bool IsExpired { get; set; }
        public int CreatedBy { get; set; }
    }
}
