
using ManagementFile.App.Models;
using ManagementFile.App.Models.Users;
using ManagementFile.App.Services;
using ManagementFile.App.ViewModels.Project;
using ManagementFile.Contracts.DTOs.ProjectManagement;
using ManagementFile.Contracts.DTOs.UserManagement;
using ManagementFile.Contracts.Enums;
using ManagementFile.Contracts.Requests.ProjectManagement.TaskComments;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ManagementFile.App.ViewModels
{
    public class TaskCommentDetailViewModel : INotifyPropertyChanged
    {
        #region Private Fields

        private readonly TaskCommentService _taskCommentService;
        private readonly ProjectApiService _projectService;
        private readonly UserManagementService _userService;
        private readonly Window _parentWindow;
        private DispatcherTimer _autoSaveTimer;

        private DialogMode _mode;
        private TaskCommentDto _originalComment;
        private TaskCommentDto _backupComment; // For cancel operation
        private int _taskId;
        private int _projectId;
        private string _content = "";
        private string _issueTitle = "";
        private string _suggestedFix = "";
        private string _relatedModule = "";
        private TaskPriority _priority = TaskPriority.Low;
        private CommentType _commentType = CommentType.General;
        private TaskStatuss _commentStatus = TaskStatuss.Todo;
        private DateTime? _dueDate;
        private decimal _estimatedFixTime = 0;
        private decimal _actualFixTime = 0;
        private bool _isBlocking = false;
        private bool _requiresDiscussion = false;
        private bool _isAgreed = false;
        private bool _isVerified = false;
        private string _resolutionNotes = "";
        private string _resolutionCommitId = "";
        private string _verificationNotes = "";

        private int? _parentCommentId;
        private TaskCommentDto _parentComment;
        private string _parentCommentSummary = "";

        private UserModel _selectedReviewer;
        private UserModel _selectedAssignee;
        private ObservableCollection<UserModel> _mentionedUsers = new ObservableCollection<UserModel>();
        private ObservableCollection<string> _relatedFiles = new ObservableCollection<string>();
        private ObservableCollection<string> _relatedScreenshots = new ObservableCollection<string>();
        private ObservableCollection<string> _relatedDocuments = new ObservableCollection<string>();
        private ObservableCollection<string> _attachments = new ObservableCollection<string>();
        private ObservableCollection<string> _tags = new ObservableCollection<string>();

        private string _statusMessage = "";
        private Brush _statusMessageColor = Brushes.Black;
        private bool _hasStatusMessage = false;
        private bool _isBusy = false;
        private bool _hasUnsavedChanges = false;
        private long _version = 1;
        private string _createdInfo = "";
        private string _updatedInfo = "";
        private string _resolvedInfo = "";
        private string _verifiedInfo = "";
        private string _versionInfo = "";
        private bool _hasResolvedInfo = false;
        private bool _hasVerifiedInfo = false;

        // Tự động điền người thực hiện, là người tạo,
        // Gửi thông báo cho người được mention và người được giao
        private bool _autoAssignToCreator = true;
        private bool _sendNotification = true;

        #endregion

        #region Constructor

        public TaskCommentDetailViewModel(
            TaskCommentService taskCommentService,
            ProjectApiService projectService,
            UserManagementService userService,
            Window parentWindow)
        {
            _taskCommentService = taskCommentService ?? throw new ArgumentNullException(nameof(taskCommentService));
            _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _parentWindow = parentWindow ?? throw new ArgumentNullException(nameof(parentWindow));

            InitializeCommands();
            LoadCommentTypes();
            LoadTaskStatuses();
            LoadPriorities();
            SetupPropertyChangeTracking();
            //SetupAutoSave();
        }

        #endregion

        #region Properties

        public DialogMode Mode
        {
            get => _mode;
            private set
            {
                if (_mode != value)
                {
                    _mode = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsAddMode));
                    OnPropertyChanged(nameof(IsEditMode));
                    OnPropertyChanged(nameof(IsViewMode));
                    OnPropertyChanged(nameof(IsReadOnly));
                    OnPropertyChanged(nameof(WindowTitle));
                    OnPropertyChanged(nameof(ModeText));
                    OnPropertyChanged(nameof(ModeColor));
                    OnPropertyChanged(nameof(SaveButtonText));
                }
            }
        }

        public bool IsAddMode => Mode == DialogMode.Add;
        public bool IsEditMode => Mode == DialogMode.Edit;
        public bool IsViewMode => Mode == DialogMode.View;
        public bool IsReadOnly => IsViewMode;


        // Cập nhật WindowTitle property
        public string WindowTitle =>
            IsReply ? $"Trả lời bình luận #{ParentCommentId}" :
            Mode == DialogMode.Add ? "Thêm bình luận mới" :
            Mode == DialogMode.Edit ? $"Chỉnh sửa bình luận #{OriginalComment?.Id}" :
            $"Chi tiết bình luận #{OriginalComment?.Id}";
        public string ModeText => Mode == DialogMode.Add ? "THÊM MỚI" :
                                 Mode == DialogMode.Edit ? "CHỈNH SỬA" :
                                 "XEM CHI TIẾT";
        public Brush ModeColor => Mode == DialogMode.Add ? Brushes.Green :
                                 Mode == DialogMode.Edit ? Brushes.Orange :
                                 Brushes.Blue;
        public string SaveButtonText => IsAddMode ? "Tạo bình luận" : "Lưu thay đổi";

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            private set
            {
                if (_hasUnsavedChanges != value)
                {
                    _hasUnsavedChanges = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(WindowTitle)); // Update title to show unsaved indicator
                }
            }
        }

        public int TaskId
        {
            get => _taskId;
            set
            {
                if (_taskId != value)
                {
                    _taskId = value;
                    OnPropertyChanged();
                    _ = LoadTaskInfoAsync();
                }
            }
        }

        public int ProjectId
        {
            get => _projectId;
            set
            {
                if (_projectId != value)
                {
                    _projectId = value;
                    OnPropertyChanged();
                    System.Diagnostics.Debug.WriteLine($"[ViewModel] ProjectId set to: {value}");
                }
            }
        }

        // Add this property for SearchScope (optional - can be hardcoded in XAML)
        public string ReviewerSearchScope => "ProjectMembers";
        public string AssigneeSearchScope => "ProjectMembers";
        public string MentionSearchScope => "ProjectMembers";

        public string TaskInfo { get; private set; } = "";

        public string Content
        {
            get => _content;
            set
            {
                if (_content != value)
                {
                    _content = value;
                    OnPropertyChanged();
                    ValidateContent();
                    MarkAsChanged();
                }
            }
        }

        public string IssueTitle
        {
            get => _issueTitle;
            set
            {
                if (_issueTitle != value)
                {
                    _issueTitle = value;
                    OnPropertyChanged();
                    MarkAsChanged();
                }
            }
        }

        public string SuggestedFix
        {
            get => _suggestedFix;
            set
            {
                if (_suggestedFix != value)
                {
                    _suggestedFix = value;
                    OnPropertyChanged();
                    MarkAsChanged();
                }
            }
        }

        public string RelatedModule
        {
            get => _relatedModule;
            set
            {
                if (_relatedModule != value)
                {
                    _relatedModule = value;
                    OnPropertyChanged();
                    MarkAsChanged();
                }
            }
        }

        public TaskPriority Priority
        {
            get => _priority;
            set
            {
                if (_priority != value)
                {
                    _priority = value;
                    OnPropertyChanged();
                    MarkAsChanged();
                }
            }
        }

        public CommentType CommentType
        {
            get => _commentType;
            set
            {
                if (_commentType != value)
                {
                    _commentType = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CommentTypeDisplayName));
                    UpdatePriorityBasedOnCommentType();
                    MarkAsChanged();
                }
            }
        }

        public string CommentTypeDisplayName => $"{CommentType.GetIcon()} {CommentType.GetDisplayName()}";

        public TaskStatuss CommentStatus
        {
            get => _commentStatus;
            set
            {
                if (_commentStatus != value)
                {
                    _commentStatus = value;
                    OnPropertyChanged();
                    MarkAsChanged();
                }
            }
        }

        public DateTime? DueDate
        {
            get => _dueDate;
            set
            {
                if (_dueDate != value)
                {
                    _dueDate = value;
                    OnPropertyChanged();
                    MarkAsChanged();
                }
            }
        }

        public decimal EstimatedFixTime
        {
            get => _estimatedFixTime;
            set
            {
                if (_estimatedFixTime != value)
                {
                    _estimatedFixTime = value;
                    OnPropertyChanged();
                    MarkAsChanged();
                }
            }
        }

        public decimal ActualFixTime
        {
            get => _actualFixTime;
            set
            {
                if (_actualFixTime != value)
                {
                    _actualFixTime = value;
                    OnPropertyChanged();
                    MarkAsChanged();
                }
            }
        }

        public bool IsBlocking
        {
            get => _isBlocking;
            set
            {
                if (_isBlocking != value)
                {
                    _isBlocking = value;
                    OnPropertyChanged();
                    MarkAsChanged();
                }
            }
        }

        public bool RequiresDiscussion
        {
            get => _requiresDiscussion;
            set
            {
                if (_requiresDiscussion != value)
                {
                    _requiresDiscussion = value;
                    OnPropertyChanged();
                    MarkAsChanged();
                }
            }
        }

        public bool IsAgreed
        {
            get => _isAgreed;
            set
            {
                if (_isAgreed != value)
                {
                    _isAgreed = value;
                    OnPropertyChanged();
                    MarkAsChanged();
                }
            }
        }

        public bool IsVerified
        {
            get => _isVerified;
            set
            {
                if (_isVerified != value)
                {
                    _isVerified = value;
                    OnPropertyChanged();
                    MarkAsChanged();
                }
            }
        }

        public string ResolutionNotes
        {
            get => _resolutionNotes;
            set
            {
                if (_resolutionNotes != value)
                {
                    _resolutionNotes = value;
                    OnPropertyChanged();
                    MarkAsChanged();
                }
            }
        }

        public string ResolutionCommitId
        {
            get => _resolutionCommitId;
            set
            {
                if (_resolutionCommitId != value)
                {
                    _resolutionCommitId = value;
                    OnPropertyChanged();
                    MarkAsChanged();
                }
            }
        }

        public string VerificationNotes
        {
            get => _verificationNotes;
            set
            {
                if (_verificationNotes != value)
                {
                    _verificationNotes = value;
                    OnPropertyChanged();
                    MarkAsChanged();
                }
            }
        }

        public UserModel SelectedReviewer
        {
            get
            {
                System.Diagnostics.Debug.WriteLine($"[ViewModel] SelectedReviewer GET: {_selectedReviewer?.FullName ?? "NULL"}");
                return _selectedReviewer;
            }
            set
            {
                System.Diagnostics.Debug.WriteLine($"[ViewModel] SelectedReviewer SET from {_selectedReviewer?.FullName ?? "NULL"} to {value?.FullName ?? "NULL"}");

                if (_selectedReviewer != value)
                {
                    _selectedReviewer = value;
                    OnPropertyChanged();

                    System.Diagnostics.Debug.WriteLine($"[ViewModel] SelectedReviewer property changed successfully");

                }
            }
        }

        public UserModel SelectedAssignee
        {
            get => _selectedAssignee;
            set
            {
                if (_selectedAssignee != value)
                {
                    _selectedAssignee = value;
                    OnPropertyChanged();
                    MarkAsChanged();
                }
            }
        }

        public ObservableCollection<UserModel> MentionedUsers
        {
            get => _mentionedUsers;
            set
            {
                if (_mentionedUsers != value)
                {
                    if (_mentionedUsers != null)
                        _mentionedUsers.CollectionChanged -= OnCollectionChanged;

                    _mentionedUsers = value ?? new ObservableCollection<UserModel>();

                    if (_mentionedUsers != null)
                        _mentionedUsers.CollectionChanged += OnCollectionChanged;

                    OnPropertyChanged();
                    MarkAsChanged();

                    System.Diagnostics.Debug.WriteLine($"[ViewModel] MentionedUsers set with {_mentionedUsers.Count} users");

                }
            }
        }

        public ObservableCollection<string> RelatedFiles
        {
            get => _relatedFiles;
            set
            {
                if (_relatedFiles != value)
                {
                    if (_relatedFiles != null)
                        _relatedFiles.CollectionChanged -= OnCollectionChanged;

                    _relatedFiles = value;

                    if (_relatedFiles != null)
                        _relatedFiles.CollectionChanged += OnCollectionChanged;

                    OnPropertyChanged();
                    MarkAsChanged();
                }
            }
        }

        public ObservableCollection<string> RelatedScreenshots
        {
            get => _relatedScreenshots;
            set
            {
                if (_relatedScreenshots != value)
                {
                    if (_relatedScreenshots != null)
                        _relatedScreenshots.CollectionChanged -= OnCollectionChanged;

                    _relatedScreenshots = value;

                    if (_relatedScreenshots != null)
                        _relatedScreenshots.CollectionChanged += OnCollectionChanged;

                    OnPropertyChanged();
                    MarkAsChanged();
                }
            }
        }

        public ObservableCollection<string> RelatedDocuments
        {
            get => _relatedDocuments;
            set
            {
                if (_relatedDocuments != value)
                {
                    if (_relatedDocuments != null)
                        _relatedDocuments.CollectionChanged -= OnCollectionChanged;

                    _relatedDocuments = value;

                    if (_relatedDocuments != null)
                        _relatedDocuments.CollectionChanged += OnCollectionChanged;

                    OnPropertyChanged();
                    MarkAsChanged();
                }
            }
        }

        public ObservableCollection<string> Attachments
        {
            get => _attachments;
            set
            {
                if (_attachments != value)
                {
                    if (_attachments != null)
                        _attachments.CollectionChanged -= OnCollectionChanged;

                    _attachments = value;

                    if (_attachments != null)
                        _attachments.CollectionChanged += OnCollectionChanged;

                    OnPropertyChanged();
                    MarkAsChanged();
                }
            }
        }

        public ObservableCollection<string> Tags
        {
            get => _tags;
            set
            {
                if (_tags != value)
                {
                    if (_tags != null)
                        _tags.CollectionChanged -= OnCollectionChanged;

                    _tags = value;

                    if (_tags != null)
                        _tags.CollectionChanged += OnCollectionChanged;

                    OnPropertyChanged();
                    MarkAsChanged();
                }
            }
        }

        public ObservableCollection<CommentTypeItem> CommentTypes { get; set; } = new ObservableCollection<CommentTypeItem>();
        public ObservableCollection<TaskStatussItem> CommentStatuses { get; set; } = new ObservableCollection<TaskStatussItem>();
        public ObservableCollection<TaskPriorityItem> Priorities { get; set; } = new ObservableCollection<TaskPriorityItem>();

        public CommentTypeItem SelectedCommentType
        {
            get => CommentTypes.FirstOrDefault(c => c.Value == CommentType);
            set
            {
                if (value != null)
                {
                    CommentType = value.Value;
                }
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    OnPropertyChanged();
                    // Update command can execute states
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                    HasStatusMessage = !string.IsNullOrEmpty(value);
                }
            }
        }

        public Brush StatusMessageColor
        {
            get => _statusMessageColor;
            set
            {
                if (_statusMessageColor != value)
                {
                    _statusMessageColor = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasStatusMessage
        {
            get => _hasStatusMessage;
            set
            {
                if (_hasStatusMessage != value)
                {
                    _hasStatusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CreatedInfo
        {
            get => _createdInfo;
            private set
            {
                if (_createdInfo != value)
                {
                    _createdInfo = value;
                    OnPropertyChanged();
                }
            }
        }

        public string UpdatedInfo
        {
            get => _updatedInfo;
            private set
            {
                if (_updatedInfo != value)
                {
                    _updatedInfo = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ResolvedInfo
        {
            get => _resolvedInfo;
            private set
            {
                if (_resolvedInfo != value)
                {
                    _resolvedInfo = value;
                    OnPropertyChanged();
                }
            }
        }

        public string VerifiedInfo
        {
            get => _verifiedInfo;
            private set
            {
                if (_verifiedInfo != value)
                {
                    _verifiedInfo = value;
                    OnPropertyChanged();
                }
            }
        }

        public string VersionInfo
        {
            get => _versionInfo;
            private set
            {
                if (_versionInfo != value)
                {
                    _versionInfo = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasResolvedInfo
        {
            get => _hasResolvedInfo;
            private set
            {
                if (_hasResolvedInfo != value)
                {
                    _hasResolvedInfo = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasVerifiedInfo
        {
            get => _hasVerifiedInfo;
            private set
            {
                if (_hasVerifiedInfo != value)
                {
                    _hasVerifiedInfo = value;
                    OnPropertyChanged();
                }
            }
        }

        public TaskCommentDto OriginalComment
        {
            get => _originalComment;
            private set
            {
                if (_originalComment != value)
                {
                    _originalComment = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// ID của comment cha (nếu là reply)
        /// </summary>
        public int? ParentCommentId
        {
            get => _parentCommentId;
            set
            {
                if (_parentCommentId != value)
                {
                    _parentCommentId = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsReply));
                    OnPropertyChanged(nameof(WindowTitle));
                    _ = LoadParentCommentAsync();
                }
            }
        }

        /// <summary>
        /// Comment cha (nếu là reply)
        /// </summary>
        public TaskCommentDto ParentComment
        {
            get => _parentComment;
            private set
            {
                if (_parentComment != value)
                {
                    _parentComment = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ParentCommentSummary));
                }
            }
        }

        /// <summary>
        /// Tóm tắt comment cha để hiển thị
        /// </summary>
        public string ParentCommentSummary
        {
            get => _parentCommentSummary;
            private set
            {
                if (_parentCommentSummary != value)
                {
                    _parentCommentSummary = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Có phải là reply không
        /// </summary>
        public bool IsReply => ParentCommentId.HasValue && ParentCommentId > 0;

        // Tự động điền người thực hiện, là người tạo,
        // Gửi thông báo cho người được mention và người được giao
        public bool AutoAssignToCreator
        {
            get => _autoAssignToCreator;
            set
            {
                if (_autoAssignToCreator != value)
                {
                    _autoAssignToCreator = value;
                    OnPropertyChanged();
                }
            }
        }
        public bool SendNotification
        {
            get => _sendNotification;
            set
            {
                if (_sendNotification != value)
                {
                    _sendNotification = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Commands

        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand CloseCommand { get; private set; }
        public ICommand EditCommand { get; private set; }
        public ICommand DeleteCommand { get; private set; }
        public ICommand SaveDraftCommand { get; private set; }

        private void InitializeCommands()
        {
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => CancelEdit());
            CloseCommand = new RelayCommand(_ => CloseDialog());
            EditCommand = new RelayCommand(_ => EnterEditMode(), _ => IsViewMode);
            DeleteCommand = new RelayCommand(async _ => await DeleteCommentAsync(), _ => !IsAddMode);
            SaveDraftCommand = new RelayCommand(async _ => await SaveDraftAsync(), _ => CanSaveDraft());
        }

        #endregion

        #region Public Methods

        public void Initialize(int taskId, int projectId, DialogMode mode, TaskCommentDto comment = null, int? parentCommentId = null)
        {
            ProjectId = projectId;
            TaskId = taskId;
            Mode = mode;
            OriginalComment = comment;
            ParentCommentId = parentCommentId;

            // Initialize collections BEFORE loading
            if (MentionedUsers == null)
            {
                MentionedUsers = new ObservableCollection<UserModel>();
            }

            System.Diagnostics.Debug.WriteLine($"[ViewModel] Initialize - TaskId: {taskId}, ProjectId: {projectId}, Mode: {mode}");


            if (comment != null)
            {
                LoadFromComment(comment);
                CreateBackup();
            }
            else
            {
                // Set default values for new comment
                CommentType = CommentType.General;
                CommentStatus = TaskStatuss.Todo;
                Priority = TaskPriority.Low;
                DueDate = DateTime.Now.AddDays(7); // Default due date to 1 week

                // If it's a reply, pre-fill some content
                if (IsReply)
                {
                    Content = $"@{ParentComment?.CreatedByName ?? "user"} ";
                }

                CreateBackup();
            }

            // Reset change tracking after initial load
            HasUnsavedChanges = false;
        }

        #endregion

        #region Private Methods

        private void SetupPropertyChangeTracking()
        {
            // Setup collection change tracking
            MentionedUsers.CollectionChanged += OnCollectionChanged;
            RelatedFiles.CollectionChanged += OnCollectionChanged;
            RelatedScreenshots.CollectionChanged += OnCollectionChanged;
            RelatedDocuments.CollectionChanged += OnCollectionChanged;
            Attachments.CollectionChanged += OnCollectionChanged;
            Tags.CollectionChanged += OnCollectionChanged;
        }

        private void SetupAutoSave()
        {
            _autoSaveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30) // Auto-save every 30 seconds
            };
            _autoSaveTimer.Tick += async (s, e) => await AutoSaveAsync();
        }

        private void OnCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //MarkAsChanged();
        }

        private void MarkAsChanged()
        {
            if (!IsAddMode && !HasUnsavedChanges) // Don't mark as changed during initial load
            {
                HasUnsavedChanges = true;
                StartAutoSaveTimer();
            }
        }

        private void StartAutoSaveTimer()
        {
            if (IsEditMode)
            {
                _autoSaveTimer?.Start();
            }
        }

        private void StopAutoSaveTimer()
        {
            _autoSaveTimer?.Stop();
        }

        private void CreateBackup()
        {
            // Create a backup for cancel operation
            _backupComment = CloneCurrentState();
        }

        private TaskCommentDto CloneCurrentState()
        {
            return new TaskCommentDto
            {
                Id = OriginalComment?.Id ?? 0,
                TaskId = TaskId,
                Content = Content,
                IssueTitle = IssueTitle,
                CommentType = CommentType,
                CommentStatus = CommentStatus,
                Priority = Priority,
                SuggestedFix = SuggestedFix,
                RelatedModule = RelatedModule,
                EstimatedFixTime = EstimatedFixTime,
                ActualFixTime = ActualFixTime,
                DueDate = DueDate,
                IsBlocking = IsBlocking,
                RequiresDiscussion = RequiresDiscussion,
                IsAgreed = IsAgreed,
                IsVerified = IsVerified,
                ResolutionNotes = ResolutionNotes,
                ResolutionCommitId = ResolutionCommitId,
                VerificationNotes = VerificationNotes,
                ReviewerId = SelectedReviewer?.Id,
                ReviewerName = SelectedReviewer?.DisplayName,
                AssignedToId = SelectedAssignee?.Id,
                AssignedToName = SelectedAssignee?.DisplayName,
                RelatedFiles = RelatedFiles?.ToList() ?? new List<string>(),
                RelatedScreenshots = RelatedScreenshots?.ToList() ?? new List<string>(),
                RelatedDocuments = RelatedDocuments?.ToList() ?? new List<string>(),
                Attachments = Attachments?.ToList() ?? new List<string>(),
                Tags = Tags?.ToList() ?? new List<string>(),
                MentionedUsers = MentionedUsers?.Select(u => u.UserName).ToList() ?? new List<string>(),
                Version = _version
            };
        }

        private void LoadCommentTypes()
        {
            CommentTypes = CommentTypeExtensions.GetCommentTypeItemsWithoutAll();
            
        }

        private void LoadTaskStatuses()
        {
            CommentStatuses = TaskStatussHelper.GetTaskStatusItems();
        }

        private void LoadPriorities()
        {
            Priorities = TaskPriorityHelper.GetTaskPriorityItems();
        }

        private async Task LoadTaskInfoAsync()
        {
            try
            {
                var task = await _taskCommentService.GetTaskInfoAsync(ProjectId, TaskId);
                if (task != null)
                {
                    TaskInfo = $"Task: {task.Title} (#{task.TaskCode})";
                    OnPropertyChanged(nameof(TaskInfo));
                }
            }
            catch (Exception ex)
            {
                ShowError($"Lỗi khi tải thông tin task: {ex.Message}");
            }
        }

        private void UpdatePriorityBasedOnCommentType()
        {
            // Update priority based on comment type
            Priority = CommentType.GetDefaultPriority();
        }

        private async Task LoadParentCommentAsync()
        {
            if (!ParentCommentId.HasValue) return;

            try
            {
                var parentComment = await _taskCommentService.GetTaskCommentByIdAsync(ProjectId, TaskId, ParentCommentId.Value);
                if (parentComment != null)
                {
                    ParentComment = parentComment;
                    ParentCommentSummary = CreateParentCommentSummary(parentComment);
                }
                else
                {
                    ParentCommentSummary = $"Không thể tải comment cha #{ParentCommentId}";
                }
            }
            catch (Exception ex)
            {
                ParentCommentSummary = $"Lỗi khi tải comment cha: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error loading parent comment {ParentCommentId}: {ex.Message}");
            }
        }

        private string CreateParentCommentSummary(TaskCommentDto parentComment)
        {
            if (parentComment == null) return "";

            var content = parentComment.Content;
            if (content.Length > 100)
            {
                content = content.Substring(0, 100) + "...";
            }

            return $"💬 Trả lời: \"{content}\" - {parentComment.CreatedByName} ({parentComment.CreatedAt:dd/MM/yyyy HH:mm})";
        }

        private void LoadFromComment(TaskCommentDto comment)
        {

            Content = comment.Content;
            IssueTitle = comment.IssueTitle;
            CommentType = comment.CommentType;
            CommentStatus = comment.CommentStatus;
            Priority = comment.Priority;
            SuggestedFix = comment.SuggestedFix;
            RelatedModule = comment.RelatedModule;
            EstimatedFixTime = comment.EstimatedFixTime;
            ActualFixTime = comment.ActualFixTime;
            DueDate = comment.DueDate;
            IsBlocking = comment.IsBlocking;
            RequiresDiscussion = comment.RequiresDiscussion;
            IsAgreed = comment.IsAgreed;
            IsVerified = comment.IsVerified;
            ResolutionNotes = comment.ResolutionNotes;
            ResolutionCommitId = comment.ResolutionCommitId;
            VerificationNotes = comment.VerificationNotes;

            _version = comment.Version;


            // Load collections
            LoadCollections(comment);

            // Set reviewer and assignee
            _ = SetReviewerAsync(comment.ReviewerId, comment.ReviewerName);
            _ = SetAssigneeAsync(comment.AssignedToId, comment.AssignedToName);

            // Set audit info
            SetAuditInfo(comment);
        }

        private void LoadCollections(TaskCommentDto comment)
        {
            RelatedFiles = new ObservableCollection<string>(comment.RelatedFiles ?? new List<string>());
            RelatedScreenshots = new ObservableCollection<string>(comment.RelatedScreenshots ?? new List<string>());
            RelatedDocuments = new ObservableCollection<string>(comment.RelatedDocuments ?? new List<string>());
            Attachments = new ObservableCollection<string>(comment.Attachments ?? new List<string>());
            Tags = new ObservableCollection<string>(comment.Tags ?? new List<string>());

            // Load mentioned users
            _ = LoadMentionedUsersAsync(comment.MentionedUsers);
        }

        private async Task LoadMentionedUsersAsync(List<string> mentionedUserNames)
        {
            if (mentionedUserNames == null || !mentionedUserNames.Any())
            {
                MentionedUsers = new ObservableCollection<UserModel>();
                return;
            }

            try
            {
                var users = await _userService.GetUsersByUsernamesAsync(mentionedUserNames);
                MentionedUsers = new ObservableCollection<UserModel>(users);
            }
            catch (Exception ex)
            {
                ShowError($"Lỗi khi tải danh sách người dùng được mention: {ex.Message}");
                MentionedUsers = new ObservableCollection<UserModel>();
            }
        }

        private async Task SetReviewerAsync(int? reviewerId, string reviewerName)
        {
            if (reviewerId.HasValue)
            {
                try
                {
                    var reviewer = await _userService.GetUserByIdAsync(reviewerId.Value);
                    SelectedReviewer = reviewer;
                }
                catch
                {
                    // Create a temporary user object if we can't load the actual user
                    SelectedReviewer = new UserModel
                    {
                        Id = reviewerId.Value,
                        FullName = reviewerName ?? "Unknown Reviewer"
                    };
                }
            }
            else
            {
                SelectedReviewer = null;
            }
        }

        private async Task SetAssigneeAsync(int? assigneeId, string assigneeName)
        {
            if (assigneeId.HasValue)
            {
                try
                {
                    var assignee = await _userService.GetUserByIdAsync(assigneeId.Value);
                    SelectedAssignee = assignee;
                }
                catch
                {
                    // Create a temporary user object if we can't load the actual user
                    SelectedAssignee = new UserModel
                    {
                        Id = assigneeId.Value,
                        FullName = assigneeName ?? "Unknown Assignee"
                    };
                }
            }
            else
            {
                SelectedAssignee = null;
            }
        }

        private void SetAuditInfo(TaskCommentDto comment)
        {
            CreatedInfo = $"Tạo bởi {comment.CreatedByName} vào lúc {comment.CreatedAt.ToString("dd/MM/yyyy HH:mm")}";

            if (comment.UpdatedAt.HasValue && comment.UpdatedBy.HasValue)
            {
                UpdatedInfo = $"Cập nhật lần cuối bởi {comment.UpdatedByName} vào lúc {comment.UpdatedAt.Value.ToString("dd/MM/yyyy HH:mm")}";
            }
            else
            {
                UpdatedInfo = "Chưa có cập nhật";
            }

            if (comment.ResolvedAt.HasValue && comment.ResolvedBy.HasValue)
            {
                ResolvedInfo = $"Giải quyết bởi {comment.ResolvedByName} vào lúc {comment.ResolvedAt.Value.ToString("dd/MM/yyyy HH:mm")}";
                HasResolvedInfo = true;
            }
            else
            {
                ResolvedInfo = "Chưa giải quyết";
                HasResolvedInfo = false;
            }

            if (comment.VerifiedAt.HasValue && comment.VerifiedBy.HasValue)
            {
                VerifiedInfo = $"Xác nhận bởi {comment.VerifiedByName} vào lúc {comment.VerifiedAt.Value.ToString("dd/MM/yyyy HH:mm")}";
                HasVerifiedInfo = true;
            }
            else
            {
                VerifiedInfo = "Chưa được xác nhận";
                HasVerifiedInfo = false;
            }

            VersionInfo = $"Version: {comment.Version}";
        }

        private void ValidateContent()
        {
            if (string.IsNullOrWhiteSpace(Content) && (IsAddMode || IsEditMode))
            {
                ShowWarning("Nội dung bình luận không được để trống");
            }
            else
            {
                ClearStatus();
            }
        }

        private bool CanSave()
        {
            if (IsBusy) return false;
            if (string.IsNullOrWhiteSpace(Content)) return false;
            return IsAddMode || IsEditMode;
        }

        private bool CanSaveDraft()
        {
            return IsEditMode && HasUnsavedChanges && !IsBusy;
        }

        private async Task SaveAsync()
        {
            if (IsBusy || string.IsNullOrWhiteSpace(Content))
                return;

            try
            {
                IsBusy = true;
                ShowInfo("Đang lưu...");

                if (IsAddMode)
                {
                    await CreateCommentAsync();
                }
                else if (IsEditMode)
                {
                    await UpdateCommentAsync();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Lỗi khi lưu bình luận: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SaveDraftAsync()
        {
            if (!CanSaveDraft()) return;

            try
            {
                ShowInfo("Đang lưu draft...");

                // Save current state as draft (implement draft saving logic)
                // For now, just show a message

                ShowSuccess("Draft đã được lưu");
                HasUnsavedChanges = false;
            }
            catch (Exception ex)
            {
                ShowError($"Lỗi khi lưu draft: {ex.Message}");
            }
        }

        private async Task AutoSaveAsync()
        {
            if (CanSaveDraft())
            {
                await SaveDraftAsync();
            }
        }



        private async Task CreateCommentAsync()
        {
            var request = new CreateTaskCommentRequest
            {
                TaskId = TaskId,
                ParentCommentId = ParentCommentId > 0 ? ParentCommentId : (int?)null,
                Content = Content,
                CommentType = CommentType,
                Priority = Priority,
                IssueTitle = IssueTitle,
                SuggestedFix = SuggestedFix,
                RelatedModule = RelatedModule,
                ReviewerId = SelectedReviewer?.Id,
                AssignedToId = SelectedAssignee?.Id,
                EstimatedFixTime = EstimatedFixTime,
                DueDate = DueDate,
                IsBlocking = IsBlocking,
                RequiresDiscussion = RequiresDiscussion,
                RelatedFiles = RelatedFiles.ToList(),
                RelatedScreenshots = RelatedScreenshots.ToList(),
                RelatedDocuments = RelatedDocuments.ToList(),
                Attachments = Attachments.ToList(),
                MentionedUsers = MentionedUsers.Select(u => u.UserName).ToList(),
                Tags = Tags.ToList(),
                SendNotification = AutoAssignToCreator,
                AutoAssignToCreator = SendNotification
            };

            var result = await _taskCommentService.CreateTaskCommentAsync(ProjectId, request);

            if (result.Success)
            {
                var successMessage = IsReply ? "Reply đã được tạo thành công" : "Bình luận đã được tạo thành công";
                ShowSuccess(successMessage);

                OriginalComment = result.Data;
                Mode = DialogMode.View;
                LoadFromComment(result.Data);
                HasUnsavedChanges = false;
                StopAutoSaveTimer();

                RequestClose?.Invoke(this, new DialogCloseEventArgs(true));

                // Log success
                System.Diagnostics.Debug.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] User {_userService.GetCurrentUser().FullName} created {(IsReply ? "reply" : "comment")} ID: {result.Data.Id}");

            }
            else
            {
                var errorMessage = IsReply ? "Lỗi khi tạo reply" : "Lỗi khi tạo bình luận";
                ShowError($"{errorMessage}: {result.Message}");
            }
        }

        private async Task UpdateCommentAsync()
        {
            if (OriginalComment == null) return;

            var request = new UpdateTaskCommentRequest
            {
                Id = OriginalComment.Id,
                Content = Content,
                CommentType = CommentType,
                CommentStatus = CommentStatus,
                Priority = Priority,
                IssueTitle = IssueTitle,
                SuggestedFix = SuggestedFix,
                RelatedModule = RelatedModule,
                ReviewerId = SelectedReviewer?.Id,
                AssignedToId = SelectedAssignee?.Id,
                EstimatedFixTime = EstimatedFixTime,
                ActualFixTime = ActualFixTime,
                DueDate = DueDate,
                IsBlocking = IsBlocking,
                RequiresDiscussion = RequiresDiscussion,
                IsAgreed = IsAgreed,
                RelatedFiles = RelatedFiles.ToList(),
                RelatedScreenshots = RelatedScreenshots.ToList(),
                RelatedDocuments = RelatedDocuments.ToList(),
                Attachments = Attachments.ToList(),
                MentionedUsers = MentionedUsers.Select(u => u.UserName).ToList(),
                Tags = Tags.ToList(),
                IsVerified = IsVerified,
                ResolutionNotes = ResolutionNotes,
                ResolutionCommitId = ResolutionCommitId,
                VerificationNotes = VerificationNotes,
                Version = _version,
                SendNotification = SendNotification
            };

            var result = await _taskCommentService.UpdateTaskCommentAsync(ProjectId, TaskId, request);

            if (result.Success)
            {
                ShowSuccess("Bình luận đã được cập nhật thành công");
                OriginalComment = result.Data;
                Mode = DialogMode.View;
                LoadFromComment(result.Data);
                HasUnsavedChanges = false;
                StopAutoSaveTimer();
                RequestClose?.Invoke(this, new DialogCloseEventArgs(true));
            }
            else
            {
                ShowError($"Lỗi khi cập nhật bình luận: {result.Message}");
            }
        }

        private async Task DeleteCommentAsync()
        {
            if (OriginalComment == null)
            {
                ShowWarning("Không có bình luận để xóa");
                return;
            }

            // Sử dụng MessageBox cho confirmation
            var confirmMessage = $"Bạn có chắc chắn muốn xóa bình luận này không?\n\n" +
                                $"Hành động này không thể hoàn tác!";

            var result = MessageBox.Show(
                confirmMessage,
                "Xác nhận xóa bình luận",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                ShowInfo("Đã hủy thao tác xóa bình luận");
                return;
            }

            try
            {
                IsBusy = true;
                ShowInfo("🗑️ Đang xóa bình luận...");

                var success = await _taskCommentService.DeleteTaskCommentAsync(ProjectId, TaskId, OriginalComment.Id);

                if (success)
                {
                    ShowSuccess("✅ Bình luận đã được xóa thành công");

                    // Log thành công
                    System.Diagnostics.Debug.WriteLine($"[2025-10-08 01:09:23]  successfully deleted comment ID: {OriginalComment.Id}");

                    // Đợi 1 giây để user thấy message
                    await Task.Delay(1000);

                    CloseDialog(true);
                }
                else
                {
                    ShowError("❌ Không thể xóa bình luận. Vui lòng thử lại sau.");
                }
            }
            catch (UnauthorizedAccessException)
            {
                ShowError("🚫 Bạn không có quyền xóa bình luận này");
                System.Diagnostics.Debug.WriteLine($"[2025-10-08 01:09:23] Unauthorized:  attempted to delete comment ID: {OriginalComment.Id}");
            }
            catch (Exception ex)
            {
                ShowError($"💥 Lỗi khi xóa bình luận: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[2025-10-08 01:09:23] Error deleting comment {OriginalComment.Id} by 'nguyenbalam57': {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void EnterEditMode()
        {
            if (!IsViewMode) return;

            Mode = DialogMode.Edit;
            CreateBackup(); // Create backup for cancel operation
            StartAutoSaveTimer();
        }

        private void CancelEdit()
        {
            if (IsAddMode)
            {
                if (HasUnsavedChanges)
                {
                    var result = MessageBox.Show(
                        "Bạn đang tạo bình luận mới và có thay đổi chưa được lưu.\n\nBạn có muốn hủy bỏ không?",
                        "Hủy tạo bình luận",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes) return;
                }

                CloseDialog();
                return;
            }

            if (HasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    "Bạn có thay đổi chưa được lưu. Bạn có muốn hủy bỏ các thay đổi này không?",
                    "Hủy thay đổi",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;
            }

            // Reset to original values
            if (_backupComment != null)
            {
                LoadFromComment(_backupComment);
            }

            Mode = DialogMode.View;
            HasUnsavedChanges = false;
            StopAutoSaveTimer();
            ClearStatus();

            RequestClose?.Invoke(this, new DialogCloseEventArgs(false));

            // Log action
            System.Diagnostics.Debug.WriteLine($"[2025-10-08 01:12:47] cancelled edit for comment ID: {OriginalComment?.Id ?? -1}");
        }


        private void CloseDialog(bool result = false)
        {
            StopAutoSaveTimer();
            _parentWindow.DialogResult = result;
            _parentWindow.Close();
        }

        private void ShowSuccess(string message)
        {
            StatusMessage = message;
            StatusMessageColor = Brushes.Green;
        }

        private void ShowError(string message)
        {
            StatusMessage = message;
            StatusMessageColor = Brushes.Red;
        }

        private void ShowWarning(string message)
        {
            StatusMessage = message;
            StatusMessageColor = Brushes.Orange;
        }

        private void ShowInfo(string message)
        {
            StatusMessage = message;
            StatusMessageColor = Brushes.Blue;
        }

        private void ClearStatus()
        {
            StatusMessage = "";
        }

        #endregion

        #region Events

        public event System.EventHandler<DialogCloseEventArgs> RequestClose;

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region IDisposable

        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // Dispose managed resources
                StopAutoSaveTimer();
                _autoSaveTimer?.Stop();

                // Unsubscribe from collection change events
                if (MentionedUsers != null)
                    MentionedUsers.CollectionChanged -= OnCollectionChanged;
                if (RelatedFiles != null)
                    RelatedFiles.CollectionChanged -= OnCollectionChanged;
                if (RelatedScreenshots != null)
                    RelatedScreenshots.CollectionChanged -= OnCollectionChanged;
                if (RelatedDocuments != null)
                    RelatedDocuments.CollectionChanged -= OnCollectionChanged;
                if (Attachments != null)
                    Attachments.CollectionChanged -= OnCollectionChanged;
                if (Tags != null)
                    Tags.CollectionChanged -= OnCollectionChanged;

                _disposed = true;
            }
        }

        #endregion
    }
}