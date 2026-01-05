using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace ManagementFile.Contracts.Requests.NotificationsAndCommunications
{
    /// <summary>
    /// Request để broadcast notification
    /// </summary>
    public class BroadcastNotificationRequest
    {
        [Required]
        public string Title { get; set; } = "";

        [Required]
        public string Content { get; set; } = "";

        public NotificationType Type { get; set; } = NotificationType.Info;

        public string ActionUrl { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public List<int> UserIds { get; set; }

        public UserRole? TargetRole { get; set; }

        public Department? TargetDepartment { get; set; }

        public bool SendToAllUsers { get; set; } = false;
    }
}
