using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ManagementFile.Contracts.Requests.NotificationsAndCommunications
{
    /// <summary>
    /// Request để tạo notification
    /// </summary>
    public class CreateNotificationRequest
    {
        [Required]
        public int UserId { get; set; } 

        [Required]
        public string Title { get; set; } = "";

        [Required]
        public string Content { get; set; } = "";

        public NotificationType Type { get; set; } = NotificationType.Info;

        public string RelatedEntityType { get; set; }

        public int RelatedEntityId { get; set; }

        public string ActionUrl { get; set; }

        public DateTime? ExpiresAt { get; set; }
    }
}
