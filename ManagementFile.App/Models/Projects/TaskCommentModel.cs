using ManagementFile.App.Models.Projects;
using ManagementFile.App.ViewModels;
using ManagementFile.Contracts.DTOs.ProjectManagement;
using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ManagementFile.App.Models
{
    /// <summary>
    /// Model cho Task Comment hiển thị trong UI
    /// Enhanced version với đầy đủ tính năng hierarchy và lazy loading
    /// </summary>
    public class TaskCommentModel : BaseViewModel
    {
        #region Private Fields

        private int _id;
        private int _taskId;
        private string _content = "";
        private CommentType _commentType = CommentType.General;
        private TaskStatuss _commentStatus = TaskStatuss.Todo;
        private TaskPriority _priority = TaskPriority.Low;
        private DateTime _createdAt = DateTime.Now;
        private string _createdByName = "";
        private int _createdById;
        private string _createdByAvatar = "";
        private DateTime? _updatedAt;
        private string _updatedByName = "";
        private int? _updatedById;
        private bool _isSystemComment;
        private bool _isEdited;
        private long _version = 1;

        // Parent/Reply support
        private int? _parentCommentId;
        private ObservableCollection<TaskCommentModel> _replies = new ObservableCollection<TaskCommentModel>();

        // Issue Details
        private string _issueTitle = "";
        private string _suggestedFix = "";
        private string _relatedModule = "";

        // Assignment
        private int? _reviewerId;
        private string _reviewerName = "";
        private int? _assignedToId;
        private string _assignedToName = "";

        // Resolution Tracking
        private DateTime? _resolvedAt;
        private int? _resolvedBy;
        private string _resolvedByName = "";
        private string _resolutionNotes = "";
        private string _resolutionCommitId = "";
        private DateTime? _verifiedAt;
        private int? _verifiedBy;
        private string _verifiedByName = "";
        private string _verificationNotes = "";
        private bool _isVerified;

        // Time Tracking
        private decimal _estimatedFixTime;
        private decimal _actualFixTime;
        private DateTime? _dueDate;

        // Flags
        private bool _isBlocking;
        private bool _requiresDiscussion;
        private bool _isAgreed;
        private int? _agreedBy;
        private string _agreedByName = "";
        private DateTime? _agreedAt;

        // Collections
        private ObservableCollection<string> _attachments = new ObservableCollection<string>();
        private ObservableCollection<string> _mentionedUsers = new ObservableCollection<string>();
        private ObservableCollection<string> _relatedFiles = new ObservableCollection<string>();
        private ObservableCollection<string> _relatedScreenshots = new ObservableCollection<string>();
        private ObservableCollection<string> _relatedDocuments = new ObservableCollection<string>();
        private ObservableCollection<string> _tags = new ObservableCollection<string>();

        // UI State
        private bool _isSelected;
        private bool _isExpanded;
        private bool _isHighlighted;
        private bool _showActions = true;

        // NEW: Hierarchy and Lazy Loading Fields
        private int _hierarchyLevel = 0;
        private bool _isLoadingReplies = false;
        private int _totalReplyCount = 0; // From server

        #endregion

        #region Constructor

        public TaskCommentModel()
        {
            InitializeCommands();
            SetupCollectionChangeHandlers();
        }

        #endregion

        #region Basic Properties

        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string IdText => Id > 0 ? Id.ToString() : ""; 

        public int TaskId
        {
            get => _taskId;
            set => SetProperty(ref _taskId, value);
        }

        public string Content
        {
            get => _content;
            set
            {
                if (SetProperty(ref _content, value))
                {
                    OnPropertyChanged(nameof(ContentSummary));
                    OnPropertyChanged(nameof(HasContent));
                }
            }
        }

        public CommentType CommentType
        {
            get => _commentType;
            set
            {
                if (SetProperty(ref _commentType, value))
                {
                    OnPropertyChanged(nameof(CommentTypeDisplayName));
                    OnPropertyChanged(nameof(CommentTypeIcon));
                    OnPropertyChanged(nameof(CommentTypeBadgeColor));
                    OnPropertyChanged(nameof(CommentTypeBadgeText));
                }
            }
        }

        public TaskStatuss CommentStatus
        {
            get => _commentStatus;
            set
            {
                if (SetProperty(ref _commentStatus, value))
                {
                    OnPropertyChanged(nameof(StatusDisplayName));
                    OnPropertyChanged(nameof(StatusIcon));
                    OnPropertyChanged(nameof(StatusColor));
                    OnPropertyChanged(nameof(IsResolved));
                }
            }
        }

        public TaskPriority Priority
        {
            get => _priority;
            set
            {
                if (SetProperty(ref _priority, value))
                {
                    OnPropertyChanged(nameof(PriorityDisplayName));
                    OnPropertyChanged(nameof(PriorityIcon));
                    OnPropertyChanged(nameof(PriorityColor));
                }
            }
        }

        public long Version
        {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        #endregion

        #region Date/Time Properties

        public DateTime CreatedAt
        {
            get => _createdAt;
            set
            {
                if (SetProperty(ref _createdAt, value))
                {
                    OnPropertyChanged(nameof(RelativeTimeText));
                    OnPropertyChanged(nameof(FormattedCreatedAt));
                }
            }
        }

        public DateTime? UpdatedAt
        {
            get => _updatedAt;
            set
            {
                if (SetProperty(ref _updatedAt, value))
                {
                    IsEdited = value.HasValue;
                    OnPropertyChanged(nameof(FormattedUpdatedAt));
                    OnPropertyChanged(nameof(HasBeenUpdated));
                }
            }
        }

        public DateTime? ResolvedAt
        {
            get => _resolvedAt;
            set
            {
                if (SetProperty(ref _resolvedAt, value))
                {
                    OnPropertyChanged(nameof(IsResolved));
                    OnPropertyChanged(nameof(FormattedResolvedAt));
                }
            }
        }

        public DateTime? VerifiedAt
        {
            get => _verifiedAt;
            set
            {
                if (SetProperty(ref _verifiedAt, value))
                {
                    OnPropertyChanged(nameof(FormattedVerifiedAt));
                }
            }
        }

        public DateTime? DueDate
        {
            get => _dueDate;
            set
            {
                if (SetProperty(ref _dueDate, value))
                {
                    OnPropertyChanged(nameof(FormattedDueDate));
                    OnPropertyChanged(nameof(IsOverdue));
                    OnPropertyChanged(nameof(DaysUntilDue));
                    OnPropertyChanged(nameof(HasDueDate));
                }
            }
        }

        public DateTime? AgreedAt
        {
            get => _agreedAt;
            set
            {
                if (SetProperty(ref _agreedAt, value))
                {
                    OnPropertyChanged(nameof(FormattedAgreedAt));
                }
            }
        }

        #endregion

        #region User Properties

        public string CreatedByName
        {
            get => _createdByName;
            set
            {
                if (SetProperty(ref _createdByName, value))
                {
                    UpdateCreatedByAvatar();
                }
            }
        }

        public int CreatedById
        {
            get => _createdById;
            set => SetProperty(ref _createdById, value);
        }

        public string CreatedByAvatar
        {
            get => _createdByAvatar;
            set => SetProperty(ref _createdByAvatar, value);
        }

        public string UpdatedByName
        {
            get => _updatedByName;
            set => SetProperty(ref _updatedByName, value);
        }

        public int? UpdatedById
        {
            get => _updatedById;
            set => SetProperty(ref _updatedById, value);
        }

        public int? ReviewerId
        {
            get => _reviewerId;
            set => SetProperty(ref _reviewerId, value);
        }

        public string ReviewerName
        {
            get => _reviewerName;
            set
            {
                if (SetProperty(ref _reviewerName, value))
                {
                    OnPropertyChanged(nameof(HasReviewer));
                }
            }
        }

        public int? AssignedToId
        {
            get => _assignedToId;
            set => SetProperty(ref _assignedToId, value);
        }

        public string AssignedToName
        {
            get => _assignedToName;
            set
            {
                if (SetProperty(ref _assignedToName, value))
                {
                    OnPropertyChanged(nameof(HasAssignee));
                }
            }
        }

        public int? ResolvedBy
        {
            get => _resolvedBy;
            set => SetProperty(ref _resolvedBy, value);
        }

        public string ResolvedByName
        {
            get => _resolvedByName;
            set => SetProperty(ref _resolvedByName, value);
        }

        public int? VerifiedBy
        {
            get => _verifiedBy;
            set => SetProperty(ref _verifiedBy, value);
        }

        public string VerifiedByName
        {
            get => _verifiedByName;
            set => SetProperty(ref _verifiedByName, value);
        }

        public int? AgreedBy
        {
            get => _agreedBy;
            set => SetProperty(ref _agreedBy, value);
        }

        public string AgreedByName
        {
            get => _agreedByName;
            set => SetProperty(ref _agreedByName, value);
        }

        #endregion

        #region Issue Details Properties

        public string IssueTitle
        {
            get => _issueTitle;
            set
            {
                if (SetProperty(ref _issueTitle, value))
                {
                    OnPropertyChanged(nameof(HasIssueTitle));
                }
            }
        }

        public string SuggestedFix
        {
            get => _suggestedFix;
            set
            {
                if (SetProperty(ref _suggestedFix, value))
                {
                    OnPropertyChanged(nameof(HasSuggestedFix));
                }
            }
        }

        public string RelatedModule
        {
            get => _relatedModule;
            set
            {
                if (SetProperty(ref _relatedModule, value))
                {
                    OnPropertyChanged(nameof(HasRelatedModule));
                }
            }
        }

        public string ResolutionNotes
        {
            get => _resolutionNotes;
            set
            {
                if (SetProperty(ref _resolutionNotes, value))
                {
                    OnPropertyChanged(nameof(HasResolutionNotes));
                }
            }
        }

        public string ResolutionCommitId
        {
            get => _resolutionCommitId;
            set
            {
                if (SetProperty(ref _resolutionCommitId, value))
                {
                    OnPropertyChanged(nameof(HasResolutionCommitId));
                }
            }
        }

        public string VerificationNotes
        {
            get => _verificationNotes;
            set
            {
                if (SetProperty(ref _verificationNotes, value))
                {
                    OnPropertyChanged(nameof(HasVerificationNotes));
                }
            }
        }

        #endregion

        #region Time Tracking Properties

        public decimal EstimatedFixTime
        {
            get => _estimatedFixTime;
            set
            {
                if (SetProperty(ref _estimatedFixTime, value))
                {
                    OnPropertyChanged(nameof(FormattedEstimatedTime));
                    OnPropertyChanged(nameof(HasEstimatedTime));
                }
            }
        }

        public decimal ActualFixTime
        {
            get => _actualFixTime;
            set
            {
                if (SetProperty(ref _actualFixTime, value))
                {
                    OnPropertyChanged(nameof(FormattedActualTime));
                    OnPropertyChanged(nameof(HasActualTime));
                    OnPropertyChanged(nameof(TimeVariance));
                }
            }
        }

        #endregion

        #region Flag Properties

        public bool IsSystemComment
        {
            get => _isSystemComment;
            set => SetProperty(ref _isSystemComment, value);
        }

        public bool IsEdited
        {
            get => _isEdited;
            set => SetProperty(ref _isEdited, value);
        }

        public bool IsVerified
        {
            get => _isVerified;
            set => SetProperty(ref _isVerified, value);
        }

        public bool IsAgreed
        {
            get => _isAgreed;
            set => SetProperty(ref _isAgreed, value);
        }

        public bool IsBlocking
        {
            get => _isBlocking;
            set
            {
                if (SetProperty(ref _isBlocking, value))
                {
                    OnPropertyChanged(nameof(BlockingIndicator));
                }
            }
        }

        public bool RequiresDiscussion
        {
            get => _requiresDiscussion;
            set
            {
                if (SetProperty(ref _requiresDiscussion, value))
                {
                    OnPropertyChanged(nameof(DiscussionIndicator));
                }
            }
        }

        #endregion

        #region Parent/Reply Properties

        public int? ParentCommentId
        {
            get => _parentCommentId;
            set
            {
                if (SetProperty(ref _parentCommentId, value))
                {
                    OnPropertyChanged(nameof(IsReply));
                }
            }
        }

        public ObservableCollection<TaskCommentModel> Replies
        {
            get => _replies;
            set
            {
                if (SetProperty(ref _replies, value))
                {
                    OnPropertyChanged(nameof(HasReplies));
                    OnPropertyChanged(nameof(ReplyCount));
                    OnPropertyChanged(nameof(LoadedReplyCount));
                    OnPropertyChanged(nameof(ShowLoadMoreReplies));
                }
            }
        }

        #endregion

        #region Collection Properties

        public ObservableCollection<string> Attachments
        {
            get => _attachments;
            set
            {
                if (SetProperty(ref _attachments, value))
                {
                    OnPropertyChanged(nameof(HasAttachments));
                    OnPropertyChanged(nameof(AttachmentCount));
                }
            }
        }

        public ObservableCollection<string> MentionedUsers
        {
            get => _mentionedUsers;
            set
            {
                if (SetProperty(ref _mentionedUsers, value))
                {
                    OnPropertyChanged(nameof(HasMentions));
                    OnPropertyChanged(nameof(MentionCount));
                }
            }
        }

        public ObservableCollection<string> RelatedFiles
        {
            get => _relatedFiles;
            set
            {
                if (SetProperty(ref _relatedFiles, value))
                {
                    OnPropertyChanged(nameof(HasRelatedFiles));
                }
            }
        }

        public ObservableCollection<string> RelatedScreenshots
        {
            get => _relatedScreenshots;
            set
            {
                if (SetProperty(ref _relatedScreenshots, value))
                {
                    OnPropertyChanged(nameof(HasRelatedScreenshots));
                }
            }
        }

        public ObservableCollection<string> RelatedDocuments
        {
            get => _relatedDocuments;
            set
            {
                if (SetProperty(ref _relatedDocuments, value))
                {
                    OnPropertyChanged(nameof(HasRelatedDocuments));
                }
            }
        }

        public ObservableCollection<string> Tags
        {
            get => _tags;
            set
            {
                if (SetProperty(ref _tags, value))
                {
                    OnPropertyChanged(nameof(HasTags));
                    OnPropertyChanged(nameof(TagCount));
                }
            }
        }

        #endregion

        #region UI State Properties

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (SetProperty(ref _isExpanded, value))
                {
                    OnPropertyChanged(nameof(ExpandCollapseIcon));
                    OnPropertyChanged(nameof(ExpandCollapseColor));
                    OnPropertyChanged(nameof(ExpandCollapseTooltip));
                }
            }
        }

        public bool IsHighlighted
        {
            get => _isHighlighted;
            set => SetProperty(ref _isHighlighted, value);
        }

        public bool ShowActions
        {
            get => _showActions;
            set => SetProperty(ref _showActions, value);
        }

        #endregion

        #region Hierarchy and Lazy Loading Properties

        /// <summary>
        /// Cấp độ phân cấp (0 = root, 1 = reply level 1, etc.)
        /// </summary>
        public int HierarchyLevel
        {
            get => _hierarchyLevel;
            set
            {
                if (SetProperty(ref _hierarchyLevel, value))
                {
                    OnPropertyChanged(nameof(IndentWidth));
                    OnPropertyChanged(nameof(IndentMargin));
                    OnPropertyChanged(nameof(LevelBadgeColor));
                    OnPropertyChanged(nameof(LevelIcon));
                    OnPropertyChanged(nameof(LevelTooltip));
                    OnPropertyChanged(nameof(LevelBackground));
                    OnPropertyChanged(nameof(HierarchyBorderBrush));
                    OnPropertyChanged(nameof(HierarchyBorderThickness));
                }
            }
        }

        /// <summary>
        /// Đang load replies
        /// </summary>
        public bool IsLoadingReplies
        {
            get => _isLoadingReplies;
            set
            {
                if (SetProperty(ref _isLoadingReplies, value))
                {
                    OnPropertyChanged(nameof(ExpandCollapseIcon));
                    OnPropertyChanged(nameof(ExpandCollapseColor));
                    OnPropertyChanged(nameof(ExpandCollapseTooltip));
                    OnPropertyChanged(nameof(ShowLoadMoreReplies));
                }
            }
        }

        /// <summary>
        /// Tổng số replies từ server (có thể > số replies đã load)
        /// </summary>
        public int TotalReplyCount
        {
            get => _totalReplyCount;
            set
            {
                if (SetProperty(ref _totalReplyCount, value))
                {
                    OnPropertyChanged(nameof(ReplyCount));
                    OnPropertyChanged(nameof(HasReplies));
                    OnPropertyChanged(nameof(CanExpand));
                    OnPropertyChanged(nameof(ShowLoadMoreReplies));
                    OnPropertyChanged(nameof(LoadMoreRepliesButtonText));
                    OnPropertyChanged(nameof(LoadMoreRepliesTooltip));
                    OnPropertyChanged(nameof(ReplyCountTooltip));
                }
            }
        }

        /// <summary>
        /// Indent width based on hierarchy level (20px per level)
        /// </summary>
        public double IndentWidth => HierarchyLevel * 20;

        /// <summary>
        /// Indent margin cho hierarchical display
        /// </summary>
        public Thickness IndentMargin => new Thickness(HierarchyLevel * 24, 0, 0, 0);

        /// <summary>
        /// Can this comment be expanded
        /// </summary>
        public bool CanExpand => HasReplies || TotalReplyCount > 0;

        /// <summary>
        /// Số lượng replies đã load
        /// </summary>
        public int LoadedReplyCount => Replies?.Count ?? 0;

        /// <summary>
        /// Expand/Collapse icon
        /// </summary>
        public string ExpandCollapseIcon
        {
            get
            {
                if (!CanExpand) return "";
                if (IsLoadingReplies) return "⏳";
                return IsExpanded ? "▼" : "▶";
            }
        }

        /// <summary>
        /// Expand/Collapse button color
        /// </summary>
        public string ExpandCollapseColor
        {
            get
            {
                if (IsLoadingReplies) return "#2196F3";
                return IsExpanded ? "#28A745" : "#6C757D";
            }
        }

        /// <summary>
        /// Expand/Collapse tooltip
        /// </summary>
        public string ExpandCollapseTooltip
        {
            get
            {
                if (IsLoadingReplies) return "Đang tải replies...";
                if (!CanExpand) return "";

                var totalReplies = TotalReplyCount > 0 ? TotalReplyCount : ReplyCount;

                if (IsExpanded)
                    return $"Click để thu gọn {totalReplies} replies";
                else
                    return $"Click để xem {totalReplies} replies";
            }
        }

        /// <summary>
        /// Level badge color based on hierarchy level
        /// </summary>
        public string LevelBadgeColor
        {
            get
            {
                switch(HierarchyLevel)
                {
                    case 0:
                        return "#007BFF"; // Root - Blue
                        case 1:
                        return "#28A745"; // Level 1 - Green
                        case 2:
                        return "#FFC107"; // Level 2 - Yellow
                        case 3:
                        return "#FF9800"; // Level 3 - Orange
                        default:
                        return "#6C757D";  // Level 4+ - Gray
                }
                
            }
        }

        /// <summary>
        /// Level icon based on hierarchy level
        /// </summary>
        public string LevelIcon
        {
            get
            {
                switch(HierarchyLevel)
                {
                    case 0:
                        return "💬"; // Root comment
                        case 1:
                        return "↩️"; // First level reply
                        case 2:
                        return "↪️"; // Second level reply
                        case 3:
                        return "⤷";  // Third level reply
                        default:
                        return "↳";   // Deeper levels
                }
                
            }
        }

        /// <summary>
        /// Level tooltip
        /// </summary>
        public string LevelTooltip
        {
            get
            {
                switch(HierarchyLevel)
                {
                    case 0:
                        return "Comment gốc";
                    case 1:
                        return "Reply cấp 1";
                    case 2:
                        return "Reply cấp 2";
                    case 3:
                        return "Reply cấp 3";
                    default:
                        return $"Reply cấp {HierarchyLevel}";
                }

            }
        }

        /// <summary>
        /// Reply count tooltip
        /// </summary>
        public string ReplyCountTooltip
        {
            get
            {
                var totalReplies = TotalReplyCount > 0 ? TotalReplyCount : ReplyCount;
                if (totalReplies == 0) return "";

                var loadedCount = LoadedReplyCount;

                if (loadedCount >= totalReplies)
                    return $"Tất cả {totalReplies} replies đã được tải";
                else if (loadedCount > 0)
                    return $"Đã tải {loadedCount}/{totalReplies} replies. Click ▶ để tải thêm";
                else
                    return $"Có {totalReplies} replies. Click ▶ để xem";
            }
        }

        /// <summary>
        /// Show "Load More Replies" button
        /// </summary>
        public bool ShowLoadMoreReplies
        {
            get
            {
                var totalReplies = TotalReplyCount > 0 ? TotalReplyCount : ReplyCount;
                var loadedCount = LoadedReplyCount;
                return IsExpanded && totalReplies > loadedCount && !IsLoadingReplies;
            }
        }

        /// <summary>
        /// Load More Replies button text
        /// </summary>
        public string LoadMoreRepliesButtonText
        {
            get
            {
                var totalReplies = TotalReplyCount > 0 ? TotalReplyCount : ReplyCount;
                var loadedCount = LoadedReplyCount;
                var remaining = totalReplies - loadedCount;
                return $"📥 Load {Math.Min(remaining, 20)} more replies ({remaining} remaining)";
            }
        }

        /// <summary>
        /// Load More Replies tooltip
        /// </summary>
        public string LoadMoreRepliesTooltip
        {
            get
            {
                var totalReplies = TotalReplyCount > 0 ? TotalReplyCount : ReplyCount;
                var loadedCount = LoadedReplyCount;
                var remaining = totalReplies - loadedCount;
                return $"Đã tải {loadedCount}/{totalReplies} replies. Click để tải thêm {Math.Min(remaining, 20)} replies.";
            }
        }

        /// <summary>
        /// Background color based on level
        /// </summary>
        public Brush LevelBackground
        {
            get
            {
                switch(HierarchyLevel)
                {
                    case 0:
                        return new SolidColorBrush(Colors.White);
                    case 1:
                        return new SolidColorBrush(Color.FromRgb(0xF0, 0xF8, 0xFF));
                    case 2:
                        return new SolidColorBrush(Color.FromRgb(0xFF, 0xFA, 0xE6));
                    case 3:
                        return new SolidColorBrush(Color.FromRgb(0xFF, 0xF5, 0xEE));
                    default:
                        return new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5));
                }

            }
        }

        /// <summary>
        /// Border brush for hierarchy visual
        /// </summary>
        public Brush HierarchyBorderBrush
        {
            get
            {
                if (!IsReply)
                    return new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xE8));

                switch(HierarchyLevel)
                {
                    case 1:
                        return new SolidColorBrush(Color.FromRgb(0x00, 0x7B, 0xFF));
                    case 2:
                        return new SolidColorBrush(Color.FromRgb(0x28, 0xA7, 0x45));
                    case 3:
                        return new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07));
                    default:
                        return new SolidColorBrush(Color.FromRgb(0x6C, 0x75, 0x7D));
                }

            }
        }

        /// <summary>
        /// Border thickness for hierarchy visual
        /// </summary>
        public Thickness HierarchyBorderThickness
        {
            get
            {
                if (IsReply)
                    return new Thickness(2, 0, 0, 0); // Left border for replies
                return new Thickness(0, 0, 0, 1); // Bottom border for root
            }
        }

        /// <summary>
        /// Visibility cho expand button
        /// </summary>
        public Visibility ExpandButtonVisibility => CanExpand ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Hierarchy indicator text
        /// </summary>
        public string HierarchyIndicator
        {
            get
            {
                if (HierarchyLevel == 0) return "";
                if (HierarchyLevel == 1) return "├─";
                if (HierarchyLevel == 2) return "│ ├─";
                return "│ │ ├─";
            }
        }

        #endregion

        #region Computed Properties

        public bool HasContent => !string.IsNullOrWhiteSpace(Content);

        public string ContentSummary
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Content))
                    return "";

                const int maxLength = 100;
                return Content.Length <= maxLength ? Content : Content.Substring(0, maxLength - 3) + "...";
            }
        }

        public bool HasAttachments => Attachments?.Count > 0;
        public int AttachmentCount => Attachments?.Count ?? 0;
        public bool HasMentions => MentionedUsers?.Count > 0;
        public int MentionCount => MentionedUsers?.Count ?? 0;
        public bool HasTags => Tags?.Count > 0;
        public int TagCount => Tags?.Count ?? 0;
        public bool IsReply => ParentCommentId.HasValue && ParentCommentId > 0;

        /// <summary>
        /// Comment có replies không (check cả TotalReplyCount và Replies.Count)
        /// </summary>
        public bool HasReplies => TotalReplyCount > 0 || (Replies?.Count ?? 0) > 0;

        /// <summary>
        /// Số lượng replies (ưu tiên TotalReplyCount từ server)
        /// </summary>
        public int ReplyCount => TotalReplyCount > 0 ? TotalReplyCount : (Replies?.Count ?? 0);

        public bool IsResolved => CommentStatus == TaskStatuss.Completed && ResolvedAt.HasValue;
        public bool IsOverdue => DueDate.HasValue && DateTime.Now > DueDate.Value && !IsResolved;
        public int DaysUntilDue => DueDate.HasValue ? (int)(DueDate.Value - DateTime.Now).TotalDays : 0;
        public bool HasBeenUpdated => UpdatedAt.HasValue;
        public bool HasReviewer => !string.IsNullOrWhiteSpace(ReviewerName);
        public bool HasAssignee => !string.IsNullOrWhiteSpace(AssignedToName);
        public bool HasIssueTitle => !string.IsNullOrWhiteSpace(IssueTitle);
        public bool HasSuggestedFix => !string.IsNullOrWhiteSpace(SuggestedFix);
        public bool HasRelatedModule => !string.IsNullOrWhiteSpace(RelatedModule);
        public bool HasResolutionNotes => !string.IsNullOrWhiteSpace(ResolutionNotes);
        public bool HasResolutionCommitId => !string.IsNullOrWhiteSpace(ResolutionCommitId);
        public bool HasVerificationNotes => !string.IsNullOrWhiteSpace(VerificationNotes);
        public bool HasRelatedFiles => RelatedFiles?.Count > 0;
        public bool HasRelatedScreenshots => RelatedScreenshots?.Count > 0;
        public bool HasRelatedDocuments => RelatedDocuments?.Count > 0;
        public bool HasEstimatedTime => EstimatedFixTime > 0;
        public bool HasActualTime => ActualFixTime > 0;
        public decimal TimeVariance => ActualFixTime - EstimatedFixTime;
        public bool HasDueDate => DueDate.HasValue;
        public bool IsRootComment => !ParentCommentId.HasValue;

        public string PriorityText => Priority.GetDisplayName();
        public string PriorityBadgeColor => Priority.GetHexColor();
        public string DueDateText => DueDate?.ToString("dd/MM/yyyy") ?? "";
        public string DueDateColor => DueDate.HasValue && DueDate < DateTime.Now ? "#E74C3C" : "#28A745";

        public bool CanEdit { get; set; } = true;
        public bool CanDelete { get; set; } = true;

        #endregion

        #region Display Properties

        public string RelativeTimeText
        {
            get
            {
                var timeSpan = DateTime.Now - CreatedAt;
                var totalMinutes = timeSpan.TotalMinutes;
                var totalHours = timeSpan.TotalHours;
                var totalDays = timeSpan.TotalDays;

                if (totalMinutes < 1) return "vừa xong";
                if (totalMinutes < 60) return $"{(int)totalMinutes} phút trước";
                if (totalHours < 24) return $"{(int)totalHours} giờ trước";
                if (totalDays < 7) return $"{(int)totalDays} ngày trước";
                if (totalDays < 30) return $"{(int)(totalDays / 7)} tuần trước";
                if (totalDays < 365) return $"{(int)(totalDays / 30)} tháng trước";
                return CreatedAt.ToString("dd/MM/yyyy");
            }
        }

        public string CommentTypeDisplayName => CommentType.GetDisplayName();
        public string CommentTypeIcon => CommentType.GetIcon();
        public string CommentTypeBadgeText => $"{CommentTypeIcon} {CommentTypeDisplayName}";
        public string CommentTypeBadgeColor => CommentType.GetHexColor();
        public string StatusDisplayName => CommentStatus.GetDisplayName();
        public string StatusIcon => CommentStatus.GetIcon();
        public string StatusColor => CommentStatus.GetHexColor();
        public string PriorityDisplayName => Priority.GetDisplayName();
        public string PriorityIcon => Priority.GetIcon();
        public string PriorityColor => Priority.GetHexColor();
        public string FormattedCreatedAt => CreatedAt.ToString("dd/MM/yyyy HH:mm");
        public string FormattedUpdatedAt => UpdatedAt?.ToString("dd/MM/yyyy HH:mm") ?? "";
        public string FormattedResolvedAt => ResolvedAt?.ToString("dd/MM/yyyy HH:mm") ?? "";
        public string FormattedVerifiedAt => VerifiedAt?.ToString("dd/MM/yyyy HH:mm") ?? "";
        public string FormattedDueDate => DueDate?.ToString("dd/MM/yyyy") ?? "";
        public string FormattedAgreedAt => AgreedAt?.ToString("dd/MM/yyyy HH:mm") ?? "";
        public string FormattedEstimatedTime => EstimatedFixTime > 0 ? $"{EstimatedFixTime:F1}h" : "";
        public string FormattedActualTime => ActualFixTime > 0 ? $"{ActualFixTime:F1}h" : "";
        public string BlockingIndicator => IsBlocking ? "🚫 Blocking" : "";
        public string DiscussionIndicator => RequiresDiscussion ? "💬 Needs Discussion" : "";

        #endregion

        #region Commands

        public ICommand EditCommand { get; private set; }
        public ICommand DeleteCommand { get; private set; }
        public ICommand ReplyCommand { get; private set; }
        public ICommand ResolveCommand { get; private set; }
        public ICommand VerifyCommand { get; private set; }
        public ICommand AgreeCommand { get; private set; }
        public ICommand AssignCommand { get; private set; }
        public ICommand ToggleExpandCommand { get; private set; }

        private void InitializeCommands()
        {
            EditCommand = new RelayCommand(_ => OnEditRequested?.Invoke(this));
            DeleteCommand = new RelayCommand(_ => OnDeleteRequested?.Invoke(this));
            ReplyCommand = new RelayCommand(_ => OnReplyRequested?.Invoke(this));
            ResolveCommand = new RelayCommand(_ => OnResolveRequested?.Invoke(this));
            VerifyCommand = new RelayCommand(_ => OnVerifyRequested?.Invoke(this));
            AgreeCommand = new RelayCommand(_ => OnAgreeRequested?.Invoke(this));
            AssignCommand = new RelayCommand(_ => OnAssignRequested?.Invoke(this));
            ToggleExpandCommand = new RelayCommand(_ => IsExpanded = !IsExpanded);
        }

        #endregion

        #region Events

        public event Action<TaskCommentModel> OnEditRequested;
        public event Action<TaskCommentModel> OnDeleteRequested;
        public event Action<TaskCommentModel> OnReplyRequested;
        public event Action<TaskCommentModel> OnResolveRequested;
        public event Action<TaskCommentModel> OnVerifyRequested;
        public event Action<TaskCommentModel> OnAgreeRequested;
        public event Action<TaskCommentModel> OnAssignRequested;

        #endregion

        #region Helper Methods

        private void UpdateCreatedByAvatar()
        {
            if (string.IsNullOrEmpty(CreatedByName))
            {
                CreatedByAvatar = "?";
                return;
            }

            var names = CreatedByName.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (names.Length >= 2)
            {
                CreatedByAvatar = $"{names[0][0]}{names[names.Length - 1][0]}".ToUpper();
            }
            else if (names.Length == 1)
            {
                var name = names[0];
                CreatedByAvatar = name.Length >= 2 ? name.Substring(0, 2).ToUpper() : name.ToUpper();
            }
            else
            {
                CreatedByAvatar = "?";
            }
        }

        private void SetupCollectionChangeHandlers()
        {
            Attachments.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasAttachments));
            MentionedUsers.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasMentions));
            RelatedFiles.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasRelatedFiles));
            RelatedScreenshots.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasRelatedScreenshots));
            RelatedDocuments.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasRelatedDocuments));
            Tags.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasTags));
            Replies.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(HasReplies));
                OnPropertyChanged(nameof(LoadedReplyCount));
                OnPropertyChanged(nameof(ShowLoadMoreReplies));
                OnPropertyChanged(nameof(LoadMoreRepliesButtonText));
                OnPropertyChanged(nameof(LoadMoreRepliesTooltip));
                OnPropertyChanged(nameof(ReplyCountTooltip));
            };
        }

        public TaskCommentModel Clone()
        {
            var clone = new TaskCommentModel
            {
                Id = Id,
                TaskId = TaskId,
                Content = Content,
                CommentType = CommentType,
                CommentStatus = CommentStatus,
                Priority = Priority,
                CreatedAt = CreatedAt,
                CreatedByName = CreatedByName,
                CreatedById = CreatedById,
                CreatedByAvatar = CreatedByAvatar,
                UpdatedAt = UpdatedAt,
                UpdatedByName = UpdatedByName,
                UpdatedById = UpdatedById,
                IsSystemComment = IsSystemComment,
                IsEdited = IsEdited,
                Version = Version,
                ParentCommentId = ParentCommentId,
                IssueTitle = IssueTitle,
                SuggestedFix = SuggestedFix,
                RelatedModule = RelatedModule,
                ReviewerId = ReviewerId,
                ReviewerName = ReviewerName,
                AssignedToId = AssignedToId,
                AssignedToName = AssignedToName,
                ResolvedAt = ResolvedAt,
                ResolvedBy = ResolvedBy,
                ResolvedByName = ResolvedByName,
                ResolutionNotes = ResolutionNotes,
                ResolutionCommitId = ResolutionCommitId,
                VerifiedAt = VerifiedAt,
                VerifiedBy = VerifiedBy,
                VerifiedByName = VerifiedByName,
                VerificationNotes = VerificationNotes,
                IsVerified = IsVerified,
                EstimatedFixTime = EstimatedFixTime,
                ActualFixTime = ActualFixTime,
                DueDate = DueDate,
                IsBlocking = IsBlocking,
                RequiresDiscussion = RequiresDiscussion,
                IsAgreed = IsAgreed,
                AgreedBy = AgreedBy,
                AgreedByName = AgreedByName,
                AgreedAt = AgreedAt,
                HierarchyLevel = HierarchyLevel,
                TotalReplyCount = TotalReplyCount
            };

            foreach (var attachment in Attachments ?? new ObservableCollection<string>())
                clone.Attachments.Add(attachment);
            foreach (var mention in MentionedUsers ?? new ObservableCollection<string>())
                clone.MentionedUsers.Add(mention);
            foreach (var file in RelatedFiles ?? new ObservableCollection<string>())
                clone.RelatedFiles.Add(file);
            foreach (var screenshot in RelatedScreenshots ?? new ObservableCollection<string>())
                clone.RelatedScreenshots.Add(screenshot);
            foreach (var document in RelatedDocuments ?? new ObservableCollection<string>())
                clone.RelatedDocuments.Add(document);
            foreach (var tag in Tags ?? new ObservableCollection<string>())
                clone.Tags.Add(tag);

            return clone;
        }

        public void Highlight()
        {
            IsHighlighted = true;
            System.Threading.Tasks.Task.Delay(3000).ContinueWith(_ =>
            {
                App.Current.Dispatcher.Invoke(() => IsHighlighted = false);
            });
        }

        #endregion

        #region Static Factory Methods

        public static TaskCommentModel CreateTaskCreatedComment(int taskId, string taskTitle, string createdByName, int createdById)
        {
            return new TaskCommentModel
            {
                TaskId = taskId,
                Content = $"Task '{taskTitle}' đã được tạo.",
                CommentType = CommentType.StatusUpdate,
                CreatedByName = createdByName,
                CreatedById = createdById,
                IsSystemComment = true,
                CommentStatus = TaskStatuss.Completed
            };
        }

        public static TaskCommentModel CreateStatusChangeComment(int taskId, string oldStatus, string newStatus, string changedByName, int changedById)
        {
            return new TaskCommentModel
            {
                TaskId = taskId,
                Content = $"Trạng thái task đã thay đổi từ '{oldStatus}' sang '{newStatus}'.",
                CommentType = CommentType.StatusUpdate,
                CreatedByName = changedByName,
                CreatedById = changedById,
                IsSystemComment = true,
                CommentStatus = TaskStatuss.Completed
            };
        }

        public static TaskCommentModel CreateAssignmentComment(int taskId, string assignedToName, string assignedByName, int assignedById)
        {
            return new TaskCommentModel
            {
                TaskId = taskId,
                Content = $"Task đã được assign cho {assignedToName}.",
                CommentType = CommentType.StatusUpdate,
                CreatedByName = assignedByName,
                CreatedById = assignedById,
                IsSystemComment = true,
                CommentStatus = TaskStatuss.Completed
            };
        }

        public static TaskCommentModel FromDto(TaskCommentDto dto)
        {
            if (dto == null) return null;

            var model = new TaskCommentModel
            {
                Id = dto.Id,
                TaskId = dto.TaskId,
                Content = dto.Content,
                CommentType = dto.CommentType,
                CommentStatus = dto.CommentStatus,
                Priority = dto.Priority,
                CreatedAt = dto.CreatedAt,
                CreatedById = dto.CreatedBy,
                CreatedByName = dto.CreatedByName,
                UpdatedAt = dto.UpdatedAt,
                UpdatedById = dto.UpdatedBy,
                UpdatedByName = dto.UpdatedByName,
                Version = dto.Version,
                ParentCommentId = dto.ParentCommentId,
                IssueTitle = dto.IssueTitle,
                SuggestedFix = dto.SuggestedFix,
                RelatedModule = dto.RelatedModule,
                ReviewerId = dto.ReviewerId,
                ReviewerName = dto.ReviewerName,
                AssignedToId = dto.AssignedToId,
                AssignedToName = dto.AssignedToName,
                ResolvedAt = dto.ResolvedAt,
                ResolvedBy = dto.ResolvedBy,
                ResolvedByName = dto.ResolvedByName,
                ResolutionNotes = dto.ResolutionNotes,
                ResolutionCommitId = dto.ResolutionCommitId,
                VerifiedAt = dto.VerifiedAt,
                VerifiedBy = dto.VerifiedBy,
                VerifiedByName = dto.VerifiedByName,
                VerificationNotes = dto.VerificationNotes,
                IsVerified = dto.IsVerified,
                EstimatedFixTime = dto.EstimatedFixTime,
                ActualFixTime = dto.ActualFixTime,
                DueDate = dto.DueDate,
                IsBlocking = dto.IsBlocking,
                RequiresDiscussion = dto.RequiresDiscussion,
                IsAgreed = dto.IsAgreed,
                AgreedBy = dto.AgreedBy,
                AgreedByName = dto.AgreedByName,
                AgreedAt = dto.AgreedAt,
                // NEW: Set TotalReplyCount from DTO
                TotalReplyCount = dto.TotalReplyCount
            };

            if (dto.Attachments != null)
                foreach (var item in dto.Attachments)
                    model.Attachments.Add(item);
            if (dto.MentionedUsers != null)
                foreach (var item in dto.MentionedUsers)
                    model.MentionedUsers.Add(item);
            if (dto.RelatedFiles != null)
                foreach (var item in dto.RelatedFiles)
                    model.RelatedFiles.Add(item);
            if (dto.RelatedScreenshots != null)
                foreach (var item in dto.RelatedScreenshots)
                    model.RelatedScreenshots.Add(item);
            if (dto.RelatedDocuments != null)
                foreach (var item in dto.RelatedDocuments)
                    model.RelatedDocuments.Add(item);
            if (dto.Tags != null)
                foreach (var item in dto.Tags)
                    model.Tags.Add(item);

            // Load replies recursively if available
            if (dto.Replies != null)
                foreach (var reply in dto.Replies)
                {
                    var replyModel = FromDto(reply);
                    if (replyModel != null)
                        model.Replies.Add(replyModel);
                }

            return model;
        }

        public static TaskCommentDto FromModel(TaskCommentModel model)
        {
            if (model == null) return null;

            var dto = new TaskCommentDto
            {
                Id = model.Id,
                TaskId = model.TaskId,
                Content = model.Content,
                CommentType = model.CommentType,
                CommentStatus = model.CommentStatus,
                Priority = model.Priority,
                CreatedAt = model.CreatedAt,
                CreatedBy = model.CreatedById,
                CreatedByName = model.CreatedByName,
                UpdatedAt = model.UpdatedAt,
                UpdatedBy = model.UpdatedById,
                UpdatedByName = model.UpdatedByName,
                Version = model.Version,
                ParentCommentId = model.ParentCommentId,
                IssueTitle = model.IssueTitle,
                SuggestedFix = model.SuggestedFix,
                RelatedModule = model.RelatedModule,
                ReviewerId = model.ReviewerId,
                ReviewerName = model.ReviewerName,
                AssignedToId = model.AssignedToId,
                AssignedToName = model.AssignedToName,
                ResolvedAt = model.ResolvedAt,
                ResolvedBy = model.ResolvedBy,
                ResolvedByName = model.ResolvedByName,
                ResolutionNotes = model.ResolutionNotes,
                ResolutionCommitId = model.ResolutionCommitId,
                VerifiedAt = model.VerifiedAt,
                VerifiedBy = model.VerifiedBy,
                VerifiedByName = model.VerifiedByName,
                VerificationNotes = model.VerificationNotes,
                IsVerified = model.IsVerified,
                EstimatedFixTime = model.EstimatedFixTime,
                ActualFixTime = model.ActualFixTime,
                DueDate = model.DueDate,
                IsBlocking = model.IsBlocking,
                RequiresDiscussion = model.RequiresDiscussion,
                IsAgreed = model.IsAgreed,
                AgreedBy = model.AgreedBy,
                AgreedByName = model.AgreedByName,
                AgreedAt = model.AgreedAt,
                // NEW: Set ReplyCount from model
                TotalReplyCount = model.TotalReplyCount > 0 ? model.TotalReplyCount : model.ReplyCount
            };

            if (model.Attachments != null)
                dto.Attachments = new List<string>(model.Attachments);
            if (model.MentionedUsers != null)
                dto.MentionedUsers = new List<string>(model.MentionedUsers);
            if (model.RelatedFiles != null)
                dto.RelatedFiles = new List<string>(model.RelatedFiles);
            if (model.RelatedScreenshots != null)
                dto.RelatedScreenshots = new List<string>(model.RelatedScreenshots);
            if (model.RelatedDocuments != null)
                dto.RelatedDocuments = new List<string>(model.RelatedDocuments);
            if (model.Tags != null)
                dto.Tags = new List<string>(model.Tags);

            return dto;
        }

        #endregion

        #region Hierarchy Building Methods

        public static List<TaskCommentModel> BuildHierarchy(IEnumerable<TaskCommentModel> comments)
        {
            var result = new List<TaskCommentModel>();
            var commentDict = comments.ToDictionary(c => c.Id, c => c);

            var rootComments = comments.Where(c => !c.ParentCommentId.HasValue).OrderBy(c => c.CreatedAt);

            foreach (var rootComment in rootComments)
            {
                AddCommentAndReplies(rootComment, result, commentDict, 0);
            }

            return result;
        }

        private static void AddCommentAndReplies(TaskCommentModel comment, List<TaskCommentModel> result,
            Dictionary<int, TaskCommentModel> commentDict, int level)
        {
            comment.HierarchyLevel = level;
            result.Add(comment);

            if (comment.IsExpanded)
            {
                var replies = commentDict.Values
                    .Where(c => c.ParentCommentId == comment.Id)
                    .OrderBy(c => c.CreatedAt);

                foreach (var reply in replies)
                {
                    AddCommentAndReplies(reply, result, commentDict, level + 1);
                }
            }
        }

        #endregion

        #region Load More SubTasks Support

        /// <summary>
        /// Show "Load More SubTasks" button khi còn subtasks chưa load
        /// </summary>
        public bool ShowLoadMoreTaskCommentButton =>
            IsExpanded && HasReplies && (Replies.Count < TotalReplyCount);

        /// <summary>
        /// Text cho Load More button
        /// </summary>
        public string LoadMoreSubTasksButtonText
        {
            get
            {
                var remaining = TotalReplyCount - Replies.Count;
                return remaining > 0 ? $"Xem thêm ({remaining} dự án)" : "";
            }
        }

        /// <summary>
        /// Số lượng subtasks còn lại chưa load
        /// </summary>
        public int RemainingSubTasksCount =>
            Math.Max(0, TotalReplyCount - Replies.Count);

        /// <summary>
        /// Tooltip cho Load More button
        /// </summary>
        public string LoadMoreSubTasksTooltip =>
            $"Click để load thêm {Math.Min(20, RemainingSubTasksCount)} dự án con.";

        /// <summary>
        /// Check if this is a "Load More" placeholder row
        /// </summary>
        public bool IsLoadMorePlaceholderRow => false;

        /// <summary>
        /// Display text for Load More row
        /// </summary>
        public string LoadMoreRowDisplayText
        {
            get
            {
                if (!IsLoadMorePlaceholderRow) return "";
                return $"Xem thêm";
            }
        }

        /// <summary>
        /// Style for Load More row
        /// </summary>
        public Brush LoadMoreRowBackground => new SolidColorBrush(Color.FromRgb(0xE3, 0xF2, 0xFD)); // Light Blue

        public Brush LoadMoreRowForeground => new SolidColorBrush(Color.FromRgb(0x19, 0x76, 0xD2)); // Blue



        #endregion
    }
}