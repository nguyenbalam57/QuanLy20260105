using ManagementFile.App.Models;
using ManagementFile.App.Models.Dialogs;
using ManagementFile.App.Models.Users;
using ManagementFile.App.Services;
using ManagementFile.App.Views.Dialogs.Comments;
using ManagementFile.App.Views.Dialogs.Users;
using ManagementFile.App.Views.Project;
using ManagementFile.Contracts.Enums;
using ManagementFile.Contracts.Requests.ProjectManagement.TaskComments;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ManagementFile.App.ViewModels.Controls
{
    /// <summary>
    /// ViewModel riêng biệt cho TaskCommentsControl - quản lý tất cả logic liên quan đến comment
    /// </summary>
    public class TaskCommentsControlViewModel : BaseViewModel
    {
        #region Private Fields

        private readonly TaskCommentService _taskCommentService;
        private readonly UserManagementService _userService;
        private readonly ProjectApiService _projectApiService;

        private int _taskId;
        private int _projectId;
        private bool _isViewMode;
        private bool _isNewTask;
        private bool _isLoading;
        private bool _isSaving;

        // Comment properties
        private ObservableCollection<TaskCommentModel> _taskComments;
        private TaskCommentModel _selectedComment;
        private string _lastCommentTime = "";

        private int initialLoadPageSize = 20;
        private int initialLoadPageNumber = 1;

        private ObservableCollection<CommentTypeItem> _commentTypes;
        private ObservableCollection<TaskPriorityItem> _priorityItems;
        private ObservableCollection<TaskStatussItem> _statusItems;

        private CommentType _selectedCommentType = CommentType.All;
        private TaskPriority _selectedPriority = TaskPriority.All;
        private TaskStatuss _selectedStatus = TaskStatuss.All;

        private string _searchKeyword;

        #endregion

        #region Constructor

        public TaskCommentsControlViewModel(
            TaskCommentService taskCommentService,
            UserManagementService userService,
            ProjectApiService projectApiService)
        {
            _taskCommentService = taskCommentService ?? throw new ArgumentNullException(nameof(taskCommentService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _projectApiService = projectApiService ?? throw new ArgumentNullException(nameof(projectApiService));

            InitializeCollections();
            InitializeCommands();
        }

        #endregion

        #region Public Properties

        public int PageSize
        {
            get => initialLoadPageSize;
            set => SetProperty(ref initialLoadPageSize, value);
        }

        /// <summary>
        /// Task ID hiện tại
        /// </summary>
        public int TaskId
        {
            get => _taskId;
            set => SetProperty(ref _taskId, value);
        }

        /// <summary>
        /// Project ID hiện tại
        /// </summary>
        public int ProjectId
        {
            get => _projectId;
            set => SetProperty(ref _projectId, value);
        }

        /// <summary>
        /// Có phải đang ở chế độ xem không
        /// </summary>
        public bool IsViewMode
        {
            get => _isViewMode;
            set => SetProperty(ref _isViewMode, value);
        }

        /// <summary>
        /// Có phải task mới không
        /// </summary>
        public bool IsNewTask
        {
            get => _isNewTask;
            set => SetProperty(ref _isNewTask, value);
        }

        /// <summary>
        /// Đang loading dữ liệu
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Đang save dữ liệu
        /// </summary>
        public bool IsSaving
        {
            get => _isSaving;
            set => SetProperty(ref _isSaving, value);
        }

        /// <summary>
        /// Collection của task comments
        /// </summary>
        public ObservableCollection<TaskCommentModel> TaskComments
        {
            get => _taskComments;
            set => SetProperty(ref _taskComments, value);
        }

        /// <summary>
        /// Comment được chọn trong DataGrid
        /// </summary>
        public TaskCommentModel SelectedComment
        {
            get => _selectedComment;
            set 
            {
                if (SetProperty(ref _selectedComment, value))
                {
                    OnPropertyChanged(nameof(SelectedTaskCommentInfo));
                }
            }
        }

        public string SelectedTaskCommentInfo =>
            SelectedComment != null ?
            $"ID {SelectedComment.Id}" :
            "Chưa có bình luận nào được chọn";

        /// <summary>
        /// Thời gian comment cuối cùng
        /// </summary>
        public string LastCommentTime
        {
            get => _lastCommentTime;
            private set => SetProperty(ref _lastCommentTime, value);
        }

        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if(SetProperty(ref _searchKeyword, value))
                {
                    RefreshFlattenedComments();
                }
            }
        }

        public ObservableCollection<CommentTypeItem> CommentTypes
        {
            get => _commentTypes;
            set => SetProperty(ref _commentTypes, value);
        }

        public CommentType SelectCommentType
        { 
            get => _selectedCommentType;
            set
            {
                if (SetProperty(ref _selectedCommentType, value))
                {
                    RefreshFlattenedComments();
                }
            }
        }

        public ObservableCollection<TaskPriorityItem> PriorityItems
        {
            get => _priorityItems;
            set => SetProperty(ref _priorityItems, value);
        }

        public TaskPriority SelectPriority
        {
            get => _selectedPriority;
            set
            {
                if (SetProperty(ref _selectedPriority, value))
                {
                    RefreshFlattenedComments();
                }
            }
        }

        public ObservableCollection<TaskStatussItem> StatusItems
        {
            get => _statusItems;
            set => SetProperty(ref _statusItems, value);
        }

        public TaskStatuss SelectStatus
        {
            get => _selectedStatus;
            set
            {
                if (SetProperty(ref _selectedStatus, value))
                {
                    RefreshFlattenedComments();
                }
            }
        }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Số lượng comment
        /// </summary>
        public int CommentCount => TaskComments?.Count ?? 0;

        /// <summary>
        /// Text hiển thị số lượng comment
        /// </summary>
        public string CommentCountText => $"{CommentCount} bình luận";

        /// <summary>
        /// Có comment hay không
        /// </summary>
        public bool HasComments => CommentCount > 0;

        /// <summary>
        /// Có thể thêm comment hay không
        /// </summary>
        public bool CanAddComments => !IsNewTask && !IsSaving;

        #region Hierarchy Properties

        private ObservableCollection<TaskCommentModel> _flattenedComments;
        /// <summary>
        /// Comments được flatten theo hierarchy để hiển thị trong DataGrid
        /// </summary>
        public ObservableCollection<TaskCommentModel> FlattenedComments
        {
            get => _flattenedComments;
            set => SetProperty(ref _flattenedComments, value);
        }

        private bool _isTreeViewMode = false;
        /// <summary>
        /// Có đang ở tree view mode không
        /// </summary>
        public bool IsTreeViewMode
        {
            get => _isTreeViewMode;
            set => SetProperty(ref _isTreeViewMode, value);
        }

        #endregion

        #endregion

        #region Public Access Methods

        /// <summary>
        /// Get current user information
        /// </summary>
        public UserModel GetCurrentUser()
        {
            var userDto = _userService?.GetCurrentUser();
            return UserModel.FromDto(userDto);
        }

        public UserModel GetUserProjectManagerOfProject()
        {
            var userProjectManager = _projectApiService.GetProjectMembersAsync(ProjectId).Result.FirstOrDefault(m => m.ProjectRole == UserRole.Manager && m.IsActive);
            return new UserModel 
            {
                Id = userProjectManager.UserId,
                FullName = userProjectManager.FullName,
                Role = userProjectManager.ProjectRole,
            };
        }

        /// <summary>
        /// Check if current user is admin
        /// </summary>
        public bool IsCurrentUserAdmin()
        {
            return _userService?.IsAdmin ?? false;
        }

        /// <summary>
        /// Check if current user is project manager
        /// </summary>
        public bool IsCurrentUserProjectManager()
        {
            return _userService?.IsProjectManager ?? false;
        }

        /// <summary>
        /// kiểm tra nguười xác nhận của dự án
        /// kiểm tra người quản lý dự án của dự án
        /// </summary>
        /// <param name="taskComment"></param>
        /// <returns></returns>
        public bool IsProjectManagerOfProject(TaskCommentModel taskComment)
        {
            return taskComment.ReviewerId == GetCurrentUser()?.Id ||
                GetCurrentUser().Id == GetUserProjectManagerOfProject().Id;
        }

        /// <summary>
        /// Check if current user can edit comment
        /// </summary>
        public bool CanCurrentUserEditComment(TaskCommentModel comment)
        {
            if (comment == null) return false;

            var currentUser = GetCurrentUser();
            return !comment.IsSystemComment &&
                   (comment.CreatedByName == currentUser?.FullName ||
                    IsCurrentUserAdmin());
        }

        /// <summary>
        /// Check if current user can delete comment
        /// </summary>
        public bool CanCurrentUserDeleteComment(TaskCommentModel comment)
        {
            if (comment == null) return false;

            var currentUser = GetCurrentUser();
            return !comment.IsSystemComment &&
                   (comment.CreatedByName == currentUser?.FullName ||
                    IsCurrentUserAdmin() ||
                    IsCurrentUserProjectManager());
        }

        /// <summary>
        /// Check if current user can perform advanced actions (assign, resolve, etc.)
        /// </summary>
        public bool CanCurrentUserPerformAdvancedActions()
        {
            return IsCurrentUserAdmin() || IsCurrentUserProjectManager();
        }

        /// <summary>
        /// người giải quyết vấn đề
        /// người được cấp phép sửa
        /// </summary>
        /// <returns></returns>
        public bool CanCurrentUserResolve(TaskCommentModel comment)
        {
            var currentUser = GetCurrentUser();
            return CanCurrentUserPerformAdvancedActions() || 
                   (comment.AssignedToId == currentUser?.Id);
        }

        #endregion

        #region Commands

        // Basic Commands
        public ICommand AddCommentCommand { get; private set; }
        public ICommand EditCommentCommand { get; private set; }
        public ICommand DeleteCommentCommand { get; private set; }
        public ICommand ViewCommentCommand { get; private set; }
        public ICommand RefreshCommentsCommand { get; private set; }
        public ICommand ExpandRepliesCommand { get; private set; }
        public ICommand AddReplyCommand { get; private set; }

        // Enhanced Commands for Context Menu
        public ICommand ResolveCommentCommand { get; private set; }
        public ICommand VerifyCommentCommand { get; private set; }
        public ICommand AssignCommentCommand { get; private set; }
        public ICommand SetReviewerCommand { get; private set; }
        public ICommand ChangePriorityCommand { get; private set; }
        public ICommand ToggleBlockingCommand { get; private set; }
        public ICommand ToggleDiscussionCommand { get; private set; }
        public ICommand ChangeStatusCommand { get; private set; }
        public ICommand ChangeTypeCommand { get; private set; }

        #endregion

        #region Events

        /// <summary>
        /// Event được fire khi có action comment hoàn thành (thêm, sửa, xóa)
        /// </summary>
        public event System.EventHandler<CommentActionEventArgs> CommentActionCompleted;

        #endregion

        #region Initialization

        private void InitializeCollections()
        {
            TaskComments = new ObservableCollection<TaskCommentModel>();
            CommentTypes = CommentTypeExtensions.GetCommentTypeItems();
            PriorityItems = TaskPriorityHelper.GetAllTaskPriorityItems();
            StatusItems = TaskStatussHelper.GetAllTaskStatusItems();
        }

        private void InitializeCommands()
        {
            // Basic Commands
            AddCommentCommand = new RelayCommand(ExecuteAddComment, CanExecuteAddComment);
            EditCommentCommand = new RelayCommand<TaskCommentModel>(
                comment => ExecuteEditComment(comment),
                comment => CanEditComment(comment));
            DeleteCommentCommand = new AsyncRelayCommand<TaskCommentModel>(
                ExecuteDeleteCommentAsync,
                CanDeleteComment);
            ViewCommentCommand = new RelayCommand<TaskCommentModel>(
                comment => ExecuteViewComment(comment));
            RefreshCommentsCommand = new AsyncRelayCommand(LoadTaskCommentsAsync);
            ExpandRepliesCommand = new RelayCommand<TaskCommentModel>(
                comment => ExecuteExpandReplies(comment),
                comment => CanExpandReplies(comment));
            AddReplyCommand = new RelayCommand<TaskCommentModel>(
                comment => ExecuteAddReply(comment),
                comment => CanAddReply(comment));

            // Enhanced Commands for Context Menu
            ResolveCommentCommand = new AsyncRelayCommand<TaskCommentModel>(
                ExecuteResolveCommentAsync,
                CanResolveComment);
            VerifyCommentCommand = new AsyncRelayCommand<TaskCommentModel>(
                ExecuteVerifyCommentAsync,
                CanVerifyComment);
            AssignCommentCommand = new AsyncRelayCommand<TaskCommentModel>(
                ExecuteAssignCommentAsync,
                CanAssignComment);
            SetReviewerCommand = new AsyncRelayCommand<TaskCommentModel>(
                ExecuteSetReviewerAsync,
                CanSetReviewer);
            ChangePriorityCommand = new AsyncRelayCommand<object>(
                ExecuteChangePriorityAsync,
                CanChangePriority);
            ToggleBlockingCommand = new AsyncRelayCommand<TaskCommentModel>(
                ExecuteToggleBlockingAsync,
                CanToggleBlocking);
            ToggleDiscussionCommand = new AsyncRelayCommand<TaskCommentModel>(
                ExecuteToggleDiscussionAsync,
                CanToggleDiscussion);
            ChangeStatusCommand = new AsyncRelayCommand<object>(
                ExecuteChangeStatusAsync,
                CanChangeStatus);
            ChangeTypeCommand = new AsyncRelayCommand<object>(
                ExecuteChangeTypeAsync,
                CanChangeType);


            
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize với thông tin task
        /// </summary>
        public async Task InitializeAsync(int taskId, int projectId, bool isViewMode = true, bool isNewTask = false)
        {
            TaskId = taskId;
            ProjectId = projectId;
            IsViewMode = isViewMode;
            IsNewTask = isNewTask;

            if (!isNewTask && taskId > 0)
            {
                await LoadTaskCommentsAsync();
            }
        }

        /// <summary>
        /// Refresh dữ liệu comment
        /// </summary>
        public async Task RefreshAsync()
        {
            await LoadTaskCommentsAsync();
        }

        #endregion

        #region Lazy Loading Methods

        /// <summary>
        /// Load more comments for lazy loading (pagination)
        /// </summary>
        /// <param name="page">Page number to load</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>List of newly loaded comments</returns>
        public async Task<List<TaskCommentModel>> LoadMoreCommentsAsync(int page, int pageSize)
        {
            try
            {
                if (IsNewTask || TaskId <= 0)
                {
                    System.Diagnostics.Debug.WriteLine("Cannot load more comments: Invalid task");
                    return new List<TaskCommentModel>();
                }

                System.Diagnostics.Debug.WriteLine($"Loading more comments: Page {page}, PageSize {pageSize}");

                // Load root comments for the specific page
                var request = new GetTaskCommentsRequest
                {
                    TaskId = TaskId,
                    PageSize = pageSize,
                    Page = page,
                    IncludeReplies = false,
                    SortDirection = "Asc",
                };

                var response = await _taskCommentService.GetTaskCommentsPagedAsync(ProjectId, request);

                if (response?.Data == null || response.Data.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No more comments to load");
                    return new List<TaskCommentModel>();
                }

                var result = new List<TaskCommentModel>();

                foreach (var commentDto in response.Data)
                {
                    var commentModel = TaskCommentModel.FromDto(commentDto);

                    // Optionally load first level of replies for better UX
                    if (commentModel.HasReplies)
                    {
                        var replies = await LoadCommentsRecursiveAsync(commentModel.Id);
                        foreach (var reply in replies)
                        {
                            commentModel.Replies.Add(reply);
                        }
                    }

                    result.Add(commentModel);
                }

                System.Diagnostics.Debug.WriteLine($"Successfully loaded {result.Count} more comments");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading more comments: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get total page count for comments
        /// </summary>
        public async Task<int> GetTotalPageCountAsync(int pageSize)
        {
            try
            {
                var request = new GetTaskCommentsRequest
                {
                    TaskId = TaskId,
                    PageSize = pageSize,
                    Page = 1,
                    ParentTaskCommentId = null
                };

                var response = await _taskCommentService.GetTaskCommentsPagedAsync(ProjectId, request);
                return response?.Pagination?.TotalPages ?? 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting total page count: {ex.Message}");
                return 0;
            }
        }

        #endregion

        #region Reply Management Methods - NEW

        /// <summary>
        /// Load replies for a specific comment (with pagination)
        /// </summary>
        public async Task<List<TaskCommentModel>> LoadRepliesForCommentAsync(int parentCommentId, int page, int pageSize)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Loading replies for comment {parentCommentId} (Page {page}, Size {pageSize})");

                var request = new GetTaskCommentsRequest
                {
                    TaskId = TaskId,
                    PageSize = pageSize,
                    Page = page,
                    ParentTaskCommentId = parentCommentId, // Load replies for this parent
                    IncludeReplies = true
                };

                var response = await _taskCommentService.GetTaskCommentsPagedAsync(ProjectId, request);

                if (response?.Data == null || response.Data.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"No replies found for comment {parentCommentId} on page {page}");
                    return new List<TaskCommentModel>();
                }

                var result = new List<TaskCommentModel>();

                foreach (var replyDto in response.Data)
                {
                    var replyModel = TaskCommentModel.FromDto(replyDto);

                    // Recursively load nested replies (up to 2 levels deep)
                    if (replyModel.HasReplies && replyModel.HierarchyLevel < 3)
                    {
                        var nestedReplies = await LoadRepliesForCommentAsync(replyModel.Id, 1, pageSize);
                        foreach (var nested in nestedReplies)
                        {
                            replyModel.Replies.Add(nested);
                        }
                    }

                    result.Add(replyModel);
                }

                System.Diagnostics.Debug.WriteLine($"Loaded {result.Count} replies for comment {parentCommentId}");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading replies for comment {parentCommentId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get a single comment by ID (for checking reply count changes)
        /// </summary>
        public async Task<TaskCommentModel> GetCommentByIdAsync(int commentId)
        {
            try
            {
                var commentDto = await _taskCommentService.GetTaskCommentByIdAsync(ProjectId, TaskId, commentId);
                return commentDto != null ? TaskCommentModel.FromDto(commentDto) : null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting comment {commentId}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get reply count for a comment from server
        /// </summary>
        public async Task<long> GetReplyCountAsync(int commentId)
        {
            try
            {
                var request = new GetTaskCommentsRequest
                {
                    TaskId = TaskId,
                    PageSize = 1,
                    Page = 1,
                    ParentTaskCommentId = commentId
                };

                var response = await _taskCommentService.GetTaskCommentsPagedAsync(ProjectId, request);
                return response?.Pagination?.TotalCount ?? 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting reply count for comment {commentId}: {ex.Message}");
                return 0;
            }
        }

        #endregion


        #region Data Loading Methods
        /// <summary>
        /// Load danh sách comment của task - UPDATED for lazy loading
        /// Chỉ load page đầu tiên, các page sau sẽ load khi scroll
        /// </summary>
        private async Task LoadTaskCommentsAsync()
        {
            try
            {
                if (IsNewTask || TaskId <= 0) return;

                IsLoading = true;

                // Load ONLY first page of root comments
                var firstPageComments = await LoadMoreCommentsAsync(initialLoadPageNumber, initialLoadPageSize);

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    TaskComments.Clear();
                    foreach (var comment in firstPageComments)
                    {
                        TaskComments.Add(comment);
                    }

                    // Build flattened hierarchy
                    RefreshFlattenedComments();
                });

                // Update computed properties
                OnPropertyChanged(nameof(HasComments));
                OnPropertyChanged(nameof(CommentCount));
                OnPropertyChanged(nameof(CommentCountText));

                // Update last comment time
                var lastComment = GetLastNonSystemComment(TaskComments);
                LastCommentTime = lastComment != null ?
                    $"Bình luận cuối: {lastComment.RelativeTimeText}" :
                    "Chưa có bình luận";

                var totalCount = CountAllComments(TaskComments);
                System.Diagnostics.Debug.WriteLine($"Loaded {totalCount} total comments (including replies) for task {TaskId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading task comments: {ex.Message}");
                CommentActionCompleted?.Invoke(this, new CommentActionEventArgs
                {
                    Action = AuditAction.LoadError,
                    Success = false,
                    Message = $"Lỗi khi tải bình luận: {ex.Message}"
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Load comments đệ quy cho một parent cụ thể
        /// </summary>
        /// <param name="parentId">ID của comment cha, null nếu là comment gốc</param>
        /// <param name="depth">Độ sâu hiện tại để tránh đệ quy vô hạn</param>
        /// <param name="maxDepth">Độ sâu tối đa cho phép (mặc định 10)</param>
        /// <returns>Danh sách comment models đã load đầy đủ replies</returns>
        private async Task<List<TaskCommentModel>> LoadCommentsRecursiveAsync(int? parentId)
        {
            var result = new List<TaskCommentModel>();

            var page = 1;
            var pageSize = 50;
            var loadedIds = new HashSet<int>(); // Tránh load trùng

            while (true)
            {
                var request = new GetTaskCommentsRequest
                {
                    TaskId = TaskId,
                    PageSize = pageSize,
                    Page = page,
                    ParentTaskCommentId = parentId
                };

                var response = await _taskCommentService.GetTaskCommentsPagedAsync(ProjectId, request);

                // Dừng nếu không có dữ liệu hoặc response null
                if (response?.Data == null || response.Data.Count == 0)
                {
                    break;
                }

                var hasNewData = false;

                // Xử lý từng comment
                foreach (var commentDto in response.Data)
                {
                    // Kiểm tra trùng ID để tránh vòng lặp
                    if (loadedIds.Contains(commentDto.Id))
                    {
                        System.Diagnostics.Debug.WriteLine($"Duplicate comment detected: {commentDto.Id}");
                        continue;
                    }

                    loadedIds.Add(commentDto.Id);
                    hasNewData = true;

                    var commentModel = TaskCommentModel.FromDto(commentDto);

                    // Load đệ quy replies với độ sâu tăng thêm 1
                    var replies = await LoadCommentsRecursiveAsync(commentModel.Id);

                    foreach (var reply in replies)
                    {
                        commentModel.Replies.Add(reply);
                    }

                    result.Add(commentModel);
                }

                // Dừng nếu không có dữ liệu mới hoặc ít hơn pageSize
                if (!hasNewData || response.Data.Count < pageSize || page > response.Pagination.TotalPages)
                {
                    break;
                }

                page++;

            }

            System.Diagnostics.Debug.WriteLine($"Loaded {result.Count} comments for parent {parentId}");
            return result;
        }

        /// <summary>
        /// Load replies cho một comment cụ thể (sử dụng logic đệ quy)
        /// </summary>
        private async Task LoadRepliesForCommentAsync(TaskCommentModel parentComment)
        {
            try
            {
                if (parentComment == null) return;

                IsLoading = true;

                var replies = await LoadCommentsRecursiveAsync(parentComment.Id);

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    parentComment.Replies.Clear();
                    foreach (var reply in replies)
                    {
                        parentComment.Replies.Add(reply);
                    }
                });

                var totalReplies = CountAllComments(replies);
                System.Diagnostics.Debug.WriteLine($"Loaded {totalReplies} total replies for comment {parentComment.Id}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading replies: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Đếm tổng số comment bao gồm cả replies đệ quy
        /// </summary>
        private int CountAllComments(IEnumerable<TaskCommentModel> comments)
        {
            if (comments == null) return 0;

            var count = 0;
            foreach (var comment in comments)
            {
                count++; // Đếm comment hiện tại
                count += CountAllComments(comment.Replies); // Đếm đệ quy replies
            }

            return count;
        }

        /// <summary>
        /// Lấy comment cuối cùng không phải system comment (tìm kiếm đệ quy)
        /// </summary>
        private TaskCommentModel GetLastNonSystemComment(IEnumerable<TaskCommentModel> comments)
        {
            if (comments == null) return null;

            TaskCommentModel lastComment = null;
            DateTime? lastTime = null;

            foreach (var comment in comments)
            {
                // Kiểm tra comment hiện tại
                if (!comment.IsSystemComment)
                {
                    if (lastTime == null || comment.CreatedAt > lastTime)
                    {
                        lastComment = comment;
                        lastTime = comment.CreatedAt;
                    }
                }

                // Tìm kiếm đệ quy trong replies
                var lastReply = GetLastNonSystemComment(comment.Replies);
                if (lastReply != null)
                {
                    if (lastTime == null || lastReply.CreatedAt > lastTime)
                    {
                        lastComment = lastReply;
                        lastTime = lastReply.CreatedAt;
                    }
                }
            }

            return lastComment;
        }

        /// <summary>
        /// loc taskcomment
        /// </summary>
        /// <param name="taskComments"></param>
        /// <returns></returns>
        private List<TaskCommentModel> ApplyTaskCommnentFilterToFlattened(IEnumerable<TaskCommentModel> taskComments)
        {
            if(taskComments == null) return new List<TaskCommentModel>();

            var filtered = taskComments.AsEnumerable();

            if(!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                var keyword = SearchKeyword.ToLower();
                filtered = filtered.Where(t =>
                    (!string.IsNullOrEmpty(t.ContentSummary) && t.ContentSummary.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrEmpty(t.IssueTitle) && t.IssueTitle.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrEmpty(t.SuggestedFix) && t.SuggestedFix.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrEmpty(t.ResolutionNotes) && t.ResolutionNotes.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrEmpty(t.VerificationNotes) && t.VerificationNotes.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrEmpty(t.AgreedByName) && t.AgreedByName.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrEmpty(t.ReviewerName) && t.ReviewerName.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrEmpty(t.CreatedByName) && t.CreatedByName.ToLower().Contains(keyword)));
            }

            if(SelectCommentType != CommentType.All)
            {
                filtered = filtered.Where(t => t.CommentType == SelectCommentType);
            }

            if (SelectPriority != TaskPriority.All)
            {
                filtered = filtered.Where(t => t.Priority == SelectPriority);
            }

            if (SelectStatus != TaskStatuss.All)
            {
                filtered = filtered.Where(t => t.CommentStatus == SelectStatus);
            }

            var fiter = new List<TaskCommentModel>();

            foreach(var task in filtered)
            {
                fiter.Add(task);
            }


            return fiter;

        }

        /// <summary>
        /// Refresh flattened comments for hierarchy display
        /// </summary>
        public void RefreshFlattenedComments()
        {
            try
            {
                
                SetHierarchyLevels(TaskComments);
                var flattened = FlattenHierarchy(
                    ApplyTaskCommnentFilterToFlattened(TaskComments),
                    respectExpandState: true);
                
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    if (FlattenedComments == null)
                        FlattenedComments = new ObservableCollection<TaskCommentModel>();
                    else
                        FlattenedComments.Clear();

                    foreach (var comment in flattened)
                    {
                        FlattenedComments.Add(comment);
                    }
                });

                System.Diagnostics.Debug.WriteLine($"Refreshed flattened comments: {FlattenedComments.Count} visible items from {CountAllComments(TaskComments)} total");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing flattened comments: {ex.Message}");
            }
        }

        /// <summary>
        /// Flatten hierarchy thành danh sách phẳng để hiển thị
        /// </summary>
        private List<TaskCommentModel> FlattenHierarchy(IEnumerable<TaskCommentModel> comments, bool respectExpandState = true)
        {
            var result = new List<TaskCommentModel>();

            if (comments == null) return result;

            foreach (var comment in comments.OrderBy(c => c.CreatedAt))
            {
                result.Add(comment);

                // Chỉ thêm children nếu comment được expand (hoặc không respect expand state)
                if ((!respectExpandState || comment.IsExpanded) && comment.HasReplies)
                {
                    var flattenedChildren = FlattenHierarchy(comment.Replies, respectExpandState);
                    result.AddRange(flattenedChildren);
                }
            }

            return result;
        }

        /// <summary>
        /// Set hierarchy level cho tất cả comments
        /// </summary>
        private void SetHierarchyLevels(IEnumerable<TaskCommentModel> comments, int level = 0)
        {
            if (comments == null) return;

            foreach (var comment in comments)
            {
                comment.HierarchyLevel = level;
                if (comment.HasReplies)
                {
                    SetHierarchyLevels(comment.Replies, level + 1);
                }
            }
        }

        /// <summary>
        /// Toggle expand/collapse với animation effect
        /// </summary>
        public void ToggleCommentExpansion(TaskCommentModel comment)
        {
            if (comment == null) return;

            try
            {
                comment.IsExpanded = !comment.IsExpanded;

                // Load replies nếu chưa load và đang expand
                if (comment.IsExpanded && comment.Replies.Count == 0 && comment.HasReplies)
                {
                    _ = LoadRepliesForCommentAsync(comment);
                }
                else
                {
                    // Refresh ngay lập tức
                    RefreshFlattenedComments();
                }

                System.Diagnostics.Debug.WriteLine($"Toggled expansion for comment {comment.Id}: IsExpanded = {comment.IsExpanded}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error toggling expansion: {ex.Message}");
            }
        }

        #endregion

        #region Basic Command Execution Methods

        private void ExecuteAddComment()
        {
            try
            {
                if (IsNewTask || TaskId <= 0)
                {
                    MessageBox.Show("Vui lòng lưu task trước khi thêm bình luận.", "Thông báo",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Open TaskCommentDetailDialog for adding new comment
                var commentDialog = TaskCommentDetailDialog.CreateForAdd(TaskId, ProjectId);
                //commentDialog.Owner = Application.Current.MainWindow;

                var result = commentDialog.ShowDialog();
                if (result == true)
                {
                    // Refresh comments after adding
                    _ = LoadTaskCommentsAsync();

                    // Notify parent
                    CommentActionCompleted?.Invoke(this, new CommentActionEventArgs
                    {
                        Action = AuditAction.Create,
                        Success = true,
                        Message = "Bình luận đã được thêm thành công!"
                    });
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Lỗi khi thêm bình luận: {ex.Message}";
                MessageBox.Show(errorMessage, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);

                CommentActionCompleted?.Invoke(this, new CommentActionEventArgs
                {
                    Action = AuditAction.Create,
                    Success = false,
                    Message = errorMessage
                });
            }
        }

        private void ExecuteEditComment(TaskCommentModel comment)
        {
            try
            {
                if (comment == null) return;

                // Convert TaskCommentModel to TaskCommentDto for the dialog
                var commentDto = TaskCommentModel.FromModel(comment);

                var commentDialog = TaskCommentDetailDialog.CreateForEdit(ProjectId, commentDto);
                //commentDialog.Owner = Application.Current.MainWindow;

                var result = commentDialog.ShowDialog();
                if (result == true)
                {
                    // Refresh comments after editing
                    _ = LoadTaskCommentsAsync();

                    // Notify parent
                    CommentActionCompleted?.Invoke(this, new CommentActionEventArgs
                    {
                        Action = AuditAction.Update,
                        Success = true,
                        Message = "Bình luận đã được cập nhật thành công!"
                    });
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Lỗi khi chỉnh sửa bình luận: {ex.Message}";
                MessageBox.Show(errorMessage, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);

                CommentActionCompleted?.Invoke(this, new CommentActionEventArgs
                {
                    Action = AuditAction.Update,
                    Success = false,
                    Message = errorMessage
                });
            }
        }

        private async Task ExecuteDeleteCommentAsync(TaskCommentModel comment)
        {
            try
            {
                if (comment == null) return;

                // Show deletion reason dialog
                var deletionReason = await ShowDeleteCommentReasonDialog(comment);
                if (string.IsNullOrEmpty(deletionReason))
                {
                    return; // User cancelled
                }

                // Confirm deletion
                var confirmMessage = $"Bạn có chắc chắn muốn xóa bình luận này không?\n\n" +
                                    $"📝 Nội dung: {comment.Content?.Substring(0, Math.Min(100, comment.Content?.Length ?? 0))}...\n" +
                                    $"👤 Tác giả: {comment.CreatedByName}\n" +
                                    $"📅 Ngày tạo: {comment.CreatedAt:dd/MM/yyyy HH:mm}\n\n" +
                                    $"🗑️ Lý do xóa: {deletionReason}\n\n" +
                                    $"⚠️ Hành động này không thể hoàn tác!";

                var result = MessageBox.Show(confirmMessage, "Xác nhận xóa bình luận",
                                           MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes) return;

                // Perform deletion
                IsSaving = true;

                var success = await _taskCommentService.DeleteTaskCommentAsync(ProjectId, TaskId, comment.Id);

                if (success)
                {
                    // Log deletion with reason
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] User 'nguyenbalam57' deleted comment ID: {comment.Id}, Reason: {deletionReason}");

                    // Add system comment about deletion
                    await AddSystemCommentForDeletion(comment, deletionReason);

                    // Refresh comments
                    await LoadTaskCommentsAsync();

                    // Notify parent
                    CommentActionCompleted?.Invoke(this, new CommentActionEventArgs
                    {
                        Action = AuditAction.Delete,
                        Success = true,
                        Message = "Bình luận đã được xóa thành công!"
                    });
                }
                else
                {
                    var errorMessage = "Không thể xóa bình luận. Vui lòng thử lại sau.";
                    MessageBox.Show(errorMessage, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);

                    CommentActionCompleted?.Invoke(this, new CommentActionEventArgs
                    {
                        Action = AuditAction.Delete,
                        Success = false,
                        Message = errorMessage
                    });
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Lỗi khi xóa bình luận: {ex.Message}";
                MessageBox.Show(errorMessage, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);

                System.Diagnostics.Debug.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Error deleting comment {comment?.Id}: {ex}");

                CommentActionCompleted?.Invoke(this, new CommentActionEventArgs
                {
                    Action = AuditAction.Delete,
                    Success = false,
                    Message = errorMessage
                });
            }
            finally
            {
                IsSaving = false;
            }
        }

        private void ExecuteViewComment(TaskCommentModel comment)
        {
            try
            {
                if (comment == null) return;

                // Convert TaskCommentModel to TaskCommentDto for the dialog
                var commentDto = TaskCommentModel.FromModel(comment);

                var commentDialog = TaskCommentDetailDialog.CreateForView(ProjectId, commentDto);
                //commentDialog.Owner = Application.Current.MainWindow;

                commentDialog.ShowDialog();
            }
            catch (Exception ex)
            {
                var errorMessage = $"Lỗi khi xem chi tiết bình luận: {ex.Message}";
                MessageBox.Show(errorMessage, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteExpandReplies(TaskCommentModel comment)
        {
            try
            {
                if (comment == null) return;

                comment.IsExpanded = !comment.IsExpanded;

                // Refresh flattened comments để update hierarchy display
                RefreshFlattenedComments();

                // Load replies if not already loaded
                if (comment.IsExpanded && comment.Replies.Count == 0)
                {
                    _ = LoadRepliesForCommentAsync(comment);
                }

                System.Diagnostics.Debug.WriteLine($"Expanded replies for comment {comment.Id}: {comment.IsExpanded}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error expanding replies: {ex.Message}");
            }
        }

        private void ExecuteAddReply(TaskCommentModel parentComment)
        {
            try
            {
                if (parentComment == null || IsNewTask || TaskId <= 0)
                {
                    MessageBox.Show("Không thể thêm reply cho comment này.", "Thông báo",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Create reply comment dialog
                var replyDialog = TaskCommentDetailDialog.CreateForReply(TaskId, ProjectId, parentComment.Id);
                //replyDialog.Owner = Application.Current.MainWindow;

                var result = replyDialog.ShowDialog();
                if (result == true)
                {
                    // Refresh comments after adding reply
                    _ = LoadTaskCommentsAsync();

                    // Notify parent
                    CommentActionCompleted?.Invoke(this, new CommentActionEventArgs
                    {
                        Action = AuditAction.Reply,
                        Success = true,
                        Message = "Reply đã được thêm thành công!"
                    });

                    System.Diagnostics.Debug.WriteLine($"Added reply to comment {parentComment.Id}");
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Lỗi khi thêm reply: {ex.Message}";
                MessageBox.Show(errorMessage, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);

                CommentActionCompleted?.Invoke(this, new CommentActionEventArgs
                {
                    Action = AuditAction.Reply,
                    Success = false,
                    Message = errorMessage
                });
            }
        }

        #endregion

        #region Enhanced Command Execution Methods

        /// <summary>
        /// Resolve comment - đánh dấu comment đã được giải quyết
        /// </summary>
        private async Task ExecuteResolveCommentAsync(TaskCommentModel comment)
        {
            try
            {
                if (comment == null) return;

                var comments = new Dictionary<string, CommentLineFieldConfig>
                {
                    { "ResolutionNotes", new CommentLineFieldConfig { Label = "Ghi chú giải quyết", Placeholder = "", Multiline = true, Required = true, MaxLength = 4000 } },
                    { "ResolutionCommitId", new CommentLineFieldConfig { Label = "Liên quan", Placeholder = "", Required = true } },
                    { "ActualFixTime", new CommentLineFieldConfig { Label = "Thời gian sửa chữa thực tế (giờ)", Placeholder = "", Multiline = false, Required = true } },
                };

                var title = "Lý do resolve";
                var message = "Vui lòng nhập lý do resolve cho comment này.";
                var commentDefautls = new Dictionary<string, string>
                {
                    { "ResolutionNotes", comment.ResolutionNotes ?? "" },
                    { "ResolutionCommitId", comment.ResolutionCommitId ?? "" },
                    { "ActualFixTime", comment.ActualFixTime.ToString() },
                };

                var resolves = CommentLine.Show(title: title,
                    message: message,
                    fields: comments,
                    defaultValues: commentDefautls);

                // Tao form nhập lý do resolve ở đây

                if (resolves != null)
                {
                    IsSaving = true;

                    var resolveTaskCommentRequest = new ResolveTaskCommentRequest
                    {
                        //id
                        CommentId = comment.Id,
                        ResolutionNotes = resolves["ResolutionNotes"],
                        ResolutionCommitId = resolves["ResolutionCommitId"],
                        ActualFixTime = decimal.Parse(resolves["ActualFixTime"]),
                        Version = comment.Version,
                    };

                    // TODO: Call API to update comment
                    await _taskCommentService.ResolveTaskCommentAsync(ProjectId, TaskId, comment.Id, resolveTaskCommentRequest);

                    // Add system comment
                    await AddSystemComment($"Comment đã được resolve bởi {comment.ResolvedByName}");

                    // Refresh comments
                    await LoadTaskCommentsAsync();

                    CommentActionCompleted?.Invoke(this, new CommentActionEventArgs
                    {
                        Action = AuditAction.Update,
                        Success = true,
                        Message = "Comment đã được đánh dấu resolved!"
                    });
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Lỗi khi resolve comment: {ex.Message}";
                MessageBox.Show(errorMessage, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);

                CommentActionCompleted?.Invoke(this, new CommentActionEventArgs
                {
                    Action = AuditAction.Update,
                    Success = false,
                    Message = errorMessage
                });
            }
            finally
            {
                IsSaving = false;
            }
        }

        /// <summary>
        /// Verify comment resolution
        /// Đã được duyệt thành công
        /// </summary>
        private async Task ExecuteVerifyCommentAsync(TaskCommentModel comment)
        {
            try
            {
                if (comment == null) return;

                var result = MessageBox.Show(
                    $"Verify rằng comment này đã được giải quyết đúng cách?\n\n📝 {comment.ContentSummary}",
                    "Xác nhận verify comment",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    IsSaving = true;

                    var verifyTaskCommentRequest = new VerifyTaskCommentRequest
                    {
                        CommentId = comment.Id,
                        VerificationNotes = comment.VerificationNotes,
                        Version = comment.Version,
                    };

                    // Call API to update comment
                    await _taskCommentService.VerifyTaskCommentAsync(ProjectId, TaskId, comment.Id, verifyTaskCommentRequest);

                    // Add system comment
                    await AddSystemComment($"Comment đã được verify bởi {comment.VerifiedByName}");

                    // Refresh comments
                    await LoadTaskCommentsAsync();

                    CommentActionCompleted?.Invoke(this, new CommentActionEventArgs
                    {
                        Action = AuditAction.Update,
                        Success = true,
                        Message = "Comment đã được verify thành công!"
                    });
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Lỗi khi verify comment: {ex.Message}";
                MessageBox.Show(errorMessage, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);

                CommentActionCompleted?.Invoke(this, new CommentActionEventArgs
                {
                    Action = AuditAction.Update,
                    Success = false,
                    Message = errorMessage
                });
            }
            finally
            {
                IsSaving = false;
            }
        }

        /// <summary>
        /// Assign comment to someone
        /// </summary>
        private async Task ExecuteAssignCommentAsync(TaskCommentModel comment)
        {
            try
            {
                if (comment == null) return;

                var assignedUser = await _userService.GetUserByIdAsync(comment.AssignedToId ?? 0);

                var assignee = UserDialog.ShowSingleUser(
                    "Chọn người thực hiện",
                    "Chọn một người để assign comment này.",
                    ProjectId,
                    "ProjectMembers",
                    assignedUser);

                if (assignee != null)
                {
                    IsSaving = true;

                    var assignedRequest = new AssignTaskCommentRequest
                    {
                        CommentId = comment.Id,
                        AssignedToId = assignee.Id,
                        Version = comment.Version,
                    };


                    // Call API to update comment
                    await _taskCommentService.AssignTaskCommentAsync(ProjectId, TaskId, comment.Id, assignedRequest);

                    // Add system comment
                    await AddSystemComment($"Comment đã được assign cho {assignee.FullName}");

                    // Refresh comments
                    await LoadTaskCommentsAsync();

                    CommentActionCompleted?.Invoke(this, new CommentActionEventArgs
                    {
                        Action = AuditAction.Update,
                        Success = true,
                        Message = $"Comment đã được assign cho {assignee.FullName}!"
                    });
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Lỗi khi assign comment: {ex.Message}";
                MessageBox.Show(errorMessage, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);

                CommentActionCompleted?.Invoke(this, new CommentActionEventArgs
                {
                    Action = AuditAction.Update,
                    Success = false,
                    Message = errorMessage
                });
            }
            finally
            {
                IsSaving = false;
            }
        }

        /// <summary>
        /// Set reviewer for comment
        /// </summary>
        private async Task ExecuteSetReviewerAsync(TaskCommentModel comment)
        {
            try
            {
                if (comment == null) return;

                var reviewUser = await _userService.GetUserByIdAsync(comment.ReviewerId ?? 0);

                var review = UserDialog.ShowSingleUser(
                    "Chọn người review",
                    "Chọn một người để review comment này.",
                    ProjectId,
                    "ProjectMembers",
                    reviewUser);

                if (review != null)
                {
                    IsSaving = true;

                    var reviewRequest = new ReviewTaskCommentRequest
                    {
                        CommentId = comment.Id,
                        ReviewerId = review.Id,
                        Version = comment.Version,
                    };


                    // Call API to update comment
                    await _taskCommentService.ReviewTaskCommentAsync(ProjectId, TaskId, comment.Id, reviewRequest);

                    // Add system comment
                    await AddSystemComment($"Comment đã được review cho {review.FullName}");

                    // Refresh comments
                    await LoadTaskCommentsAsync();

                    CommentActionCompleted?.Invoke(this, new CommentActionEventArgs
                    {
                        Action = AuditAction.Update,
                        Success = true,
                        Message = $"Comment đã được review cho {review.FullName}!"
                    });
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Lỗi khi review comment: {ex.Message}";
                MessageBox.Show(errorMessage, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);

                CommentActionCompleted?.Invoke(this, new CommentActionEventArgs
                {
                    Action = AuditAction.Update,
                    Success = false,
                    Message = errorMessage
                });
            }
            finally
            {
                IsSaving = false;
            }
        }

        /// <summary>
        /// Change comment priority
        /// Parameter: { Comment = TaskCommentModel, Priority = TaskPriority }
        /// </summary>
        private async Task ExecuteChangePriorityAsync(object parameter)
        {
            try
            {
                if (parameter == null) return;

                // Extract comment and priority from parameter
                var paramType = parameter.GetType();
                var commentProperty = paramType.GetProperty("Comment");
                var priorityProperty = paramType.GetProperty("Priority");

                if (commentProperty == null || priorityProperty == null) return;

                var comment = commentProperty.GetValue(parameter) as TaskCommentModel;
                var priority = (TaskPriority)priorityProperty.GetValue(parameter);

                if (comment == null) return;

                var oldPriority = comment.Priority;
                var result = MessageBox.Show(
                    $"Thay đổi priority từ '{oldPriority.GetDisplayName()}' sang '{priority.GetDisplayName()}'?\n\n📝 {comment.ContentSummary}",
                    "Xác nhận thay đổi priority",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    IsSaving = true;

                    var prioritys = new PriorityTaskCommentRequest
                    {
                        CommentId = comment.Id,
                        Priority = priority,
                        Version = comment.Version,
                    };

                    // Call API to update comment
                    await _taskCommentService.PriorityTaskCommentAsync(ProjectId, TaskId, comment.Id, prioritys);

                    // Add system comment với priority icons
                    await AddSystemComment($"Priority đã thay đổi từ {oldPriority.GetIcon()} {oldPriority.GetDisplayName()} sang {priority.GetIcon()} {priority.GetDisplayName()}");

                    // Refresh comments
                    await LoadTaskCommentsAsync();

                    CommentActionCompleted?.Invoke(this, new CommentActionEventArgs
                    {
                        Action = AuditAction.Update,
                        Success = true,
                        Message = $"Priority đã được thay đổi thành {priority.GetDisplayName()}!"
                    });
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Lỗi khi thay đổi priority: {ex.Message}";
                MessageBox.Show(errorMessage, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);

                CommentActionCompleted?.Invoke(this, new CommentActionEventArgs
                {
                    Action = AuditAction.Update,
                    Success = false,
                    Message = errorMessage
                });
            }
            finally
            {
                IsSaving = false;
            }
        }

        /// <summary>
        /// Toggle blocking flag for comment
        /// </summary>
        private async Task ExecuteToggleBlockingAsync(TaskCommentModel comment)
        {
            try
            {
                if (comment == null) return;

                var newBlockingState = !comment.IsBlocking;
                var action = newBlockingState ? "đánh dấu blocking" : "bỏ đánh dấu blocking";

                var result = MessageBox.Show(
                    $"Bạn có muốn {action} comment này?\n\n📝 {comment.ContentSummary}",
                    $"Xác nhận {action}",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    IsSaving = true;

                    var blockingRequest = new ToggleBlockingTaskCommentRequest
                    {
                        CommentId = comment.Id,
                        IsBlocking = newBlockingState,
                        Version = comment.Version,
                    };

                    // Call API to update comment
                    await _taskCommentService.BlockTaskCommentAsync(ProjectId, TaskId, comment.Id, blockingRequest);

                    // Add system comment
                    var blockingIcon = newBlockingState ? "🚫" : "✅";
                    await AddSystemComment($"{blockingIcon} Comment đã được {action}");

                    // Refresh comments
                    await LoadTaskCommentsAsync();

                    CommentActionCompleted?.Invoke(this, new CommentActionEventArgs
                    {
                        Action = AuditAction.Update,
                        Success = true,
                        Message = $"Comment đã được {action}!"
                    });
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Lỗi khi toggle blocking: {ex.Message}";
                MessageBox.Show(errorMessage, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);

                CommentActionCompleted?.Invoke(this, new CommentActionEventArgs
                {
                    Action = AuditAction.Update,
                    Success = false,
                    Message = errorMessage
                });
            }
            finally
            {
                IsSaving = false;
            }
        }

        /// <summary>
        /// Toggle discussion requirement for comment
        /// </summary>
        private async Task ExecuteToggleDiscussionAsync(TaskCommentModel comment)
        {
            try
            {
                if (comment == null) return;

                var newDiscussionState = !comment.RequiresDiscussion;
                var action = newDiscussionState ? "yêu cầu thảo luận" : "bỏ yêu cầu thảo luận";

                var result = MessageBox.Show(
                    $"Bạn có muốn {action} cho comment này?\n\n📝 {comment.ContentSummary}",
                    $"Xác nhận {action}",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    IsSaving = true;

                    var discussionRequest = new ToggleDiscussionTaskCommentRequest
                    {
                        CommentId = comment.Id,
                        RequiresDiscussion = newDiscussionState,
                        Version = comment.Version,
                    };

                    // Call API to update comment
                    await _taskCommentService.RequiresDiscussionTaskCommentAsync(ProjectId, TaskId, comment.Id, discussionRequest);

                    // Add system comment
                    var discussionIcon = newDiscussionState ? "💬" : "🔇";
                    await AddSystemComment($"{discussionIcon} Comment đã được {action}");

                    // Refresh comments
                    await LoadTaskCommentsAsync();

                    CommentActionCompleted?.Invoke(this, new CommentActionEventArgs
                    {
                        Action = AuditAction.Update,
                        Success = true,
                        Message = $"Comment đã được {action}!"
                    });
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Lỗi khi toggle discussion: {ex.Message}";
                MessageBox.Show(errorMessage, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);

                CommentActionCompleted?.Invoke(this, new CommentActionEventArgs
                {
                    Action = AuditAction.Update,
                    Success = false,
                    Message = errorMessage
                });
            }
            finally
            {
                IsSaving = false;
            }
        }

        /// <summary>
        /// Change comment status
        /// Parameter: { Comment = TaskCommentModel, Status = TaskStatuss }
        /// </summary>
        private async Task ExecuteChangeStatusAsync(object parameter)
        {
            try
            {
                if (parameter == null) return;

                var paramType = parameter.GetType();
                var commentProperty = paramType.GetProperty("Comment");
                var statusProperty = paramType.GetProperty("Status");

                if (commentProperty == null || statusProperty == null) return;

                var comment = commentProperty.GetValue(parameter) as TaskCommentModel;
                var status = (TaskStatuss)statusProperty.GetValue(parameter);

                if (comment == null) return;

                var oldStatus = comment.CommentStatus;
                var result = MessageBox.Show(
                    $"Thay đổi status từ '{oldStatus.GetDisplayName()}' sang '{status.GetDisplayName()}'?\n\n📝 {comment.ContentSummary}",
                    "Xác nhận thay đổi status",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    IsSaving = true;

                    var statusRequest = new StatusTaskCommentRequest
                    {
                        CommentId = comment.Id,
                        Status = status,
                        Version = comment.Version,
                    };

                    // TODO: Call API to update comment
                    await _taskCommentService.StatusTaskCommentAsync(ProjectId, TaskId, comment.Id, statusRequest);

                    // Add system comment với status icons
                    await AddSystemComment($"Status đã thay đổi từ {oldStatus.GetIcon()} {oldStatus.GetDisplayName()} sang {status.GetIcon()} {status.GetDisplayName()}");

                    // Refresh comments
                    await LoadTaskCommentsAsync();

                    CommentActionCompleted?.Invoke(this, new CommentActionEventArgs
                    {
                        Action = AuditAction.Update,
                        Success = true,
                        Message = $"Status đã được thay đổi thành {status.GetDisplayName()}!"
                    });
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Lỗi khi thay đổi status: {ex.Message}";
                MessageBox.Show(errorMessage, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);

                CommentActionCompleted?.Invoke(this, new CommentActionEventArgs
                {
                    Action = AuditAction.Update,
                    Success = false,
                    Message = errorMessage
                });
            }
            finally
            {
                IsSaving = false;
            }
        }

        /// <summary>
        /// Change comment type
        /// Parameter: { Comment = TaskCommentModel, Type = CommentType }
        /// </summary>
        private async Task ExecuteChangeTypeAsync(object parameter)
        {
            try
            {
                if (parameter == null) return;

                var paramType = parameter.GetType();
                var commentProperty = paramType.GetProperty("Comment");
                var typeProperty = paramType.GetProperty("Type");

                if (commentProperty == null || typeProperty == null) return;

                var comment = commentProperty.GetValue(parameter) as TaskCommentModel;
                var commentType = (CommentType)typeProperty.GetValue(parameter);

                if (comment == null) return;

                var oldType = comment.CommentType;
                var result = MessageBox.Show(
                    $"Thay đổi loại comment từ '{oldType.GetDisplayName()}' sang '{commentType.GetDisplayName()}'?\n\n📝 {comment.ContentSummary}",
                    "Xác nhận thay đổi loại comment",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    IsSaving = true;

                    var typeRequest = new CommentTypeTaskCommentRequest
                    {
                        CommentId = comment.Id,
                        CommentType = commentType,
                        Version = comment.Version,
                    };

                    // Call API to update comment
                    await _taskCommentService.CommentTypeTaskCommentAsync(ProjectId, TaskId, comment.Id, typeRequest);

                    // Add system comment với type icons
                    await AddSystemComment($"Loại comment đã thay đổi từ {oldType.GetIcon()} {oldType.GetDisplayName()} sang {commentType.GetIcon()} {commentType.GetDisplayName()}");

                    // Refresh comments
                    await LoadTaskCommentsAsync();

                    CommentActionCompleted?.Invoke(this, new CommentActionEventArgs
                    {
                        Action = AuditAction.Update,
                        Success = true,
                        Message = $"Loại comment đã được thay đổi thành {commentType.GetDisplayName()}!"
                    });
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Lỗi khi thay đổi loại comment: {ex.Message}";
                MessageBox.Show(errorMessage, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);

                CommentActionCompleted?.Invoke(this, new CommentActionEventArgs
                {
                    Action = AuditAction.Update,
                    Success = false,
                    Message = errorMessage
                });
            }
            finally
            {
                IsSaving = false;
            }
        }

        #endregion

        #region Enhanced Validation Methods

        private bool CanResolveComment(TaskCommentModel comment)
        {
            return comment != null &&
                   !comment.IsSystemComment &&
                   !comment.IsResolved &&
                   !IsLoading &&
                   (_userService.IsAdmin || _userService.IsProjectManager ||
                    comment.CreatedByName == _userService.GetCurrentUser()?.FullName);
        }

        private bool CanVerifyComment(TaskCommentModel comment)
        {
            return comment != null &&
                   !comment.IsSystemComment &&
                   comment.IsResolved &&
                   !comment.IsVerified &&
                   !IsLoading &&
                   (_userService.IsAdmin || _userService.IsProjectManager);
        }

        private bool CanAssignComment(TaskCommentModel comment)
        {
            return comment != null &&
                   !comment.IsSystemComment &&
                   !IsLoading &&
                   (_userService.IsAdmin || _userService.IsProjectManager);
        }

        private bool CanSetReviewer(TaskCommentModel comment)
        {
            return comment != null &&
                   !comment.IsSystemComment &&
                   !IsLoading &&
                   (_userService.IsAdmin || _userService.IsProjectManager);
        }

        private bool CanChangePriority(object parameter)
        {
            if (parameter == null) return false;

            var paramType = parameter.GetType();
            var commentProperty = paramType.GetProperty("Comment");
            if (commentProperty == null) return false;

            var comment = commentProperty.GetValue(parameter) as TaskCommentModel;
            return comment != null &&
                   !comment.IsSystemComment &&
                   !IsLoading &&
                   (_userService.IsAdmin || _userService.IsProjectManager ||
                    comment.CreatedByName == _userService.GetCurrentUser()?.FullName);
        }

        private bool CanToggleBlocking(TaskCommentModel comment)
        {
            return comment != null &&
                   !comment.IsSystemComment &&
                   !IsLoading &&
                   comment.CommentType.CanBeBlocking() &&
                   (_userService.IsAdmin || _userService.IsProjectManager);
        }

        private bool CanToggleDiscussion(TaskCommentModel comment)
        {
            return comment != null &&
                   !comment.IsSystemComment &&
                   !IsLoading &&
                   (_userService.IsAdmin || _userService.IsProjectManager ||
                    comment.CreatedByName == _userService.GetCurrentUser()?.FullName);
        }

        private bool CanChangeStatus(object parameter)
        {
            if (parameter == null) return false;

            var paramType = parameter.GetType();
            var commentProperty = paramType.GetProperty("Comment");
            if (commentProperty == null) return false;

            var comment = commentProperty.GetValue(parameter) as TaskCommentModel;
            return comment != null &&
                   !comment.IsSystemComment &&
                   !IsLoading &&
                   (_userService.IsAdmin || _userService.IsProjectManager ||
                    comment.CreatedByName == _userService.GetCurrentUser()?.FullName);
        }

        private bool CanChangeType(object parameter)
        {
            if (parameter == null) return false;

            var paramType = parameter.GetType();
            var commentProperty = paramType.GetProperty("Comment");
            if (commentProperty == null) return false;

            var comment = commentProperty.GetValue(parameter) as TaskCommentModel;
            return comment != null &&
                   !comment.IsSystemComment &&
                   !IsLoading &&
                   (_userService.IsAdmin || _userService.IsProjectManager ||
                    comment.CreatedByName == _userService.GetCurrentUser()?.FullName);
        }

        #endregion

        #region Basic Validation Methods (unchanged)

        private bool CanExecuteAddComment()
        {
            return !IsNewTask && !IsSaving && TaskId > 0;
        }

        private bool CanEditComment(TaskCommentModel comment)
        {
            return comment != null &&
                   !IsLoading &&
                   !comment.IsSystemComment &&
                   (comment.CreatedByName == _userService.GetCurrentUser()?.FullName ||
                    _userService.IsAdmin);
        }

        private bool CanDeleteComment(TaskCommentModel comment)
        {
            return comment != null &&
                   !IsLoading &&
                   !comment.IsSystemComment &&
                   (comment.CreatedByName == _userService.GetCurrentUser()?.FullName ||
                    _userService.IsAdmin ||
                    _userService.IsProjectManager);
        }

        private bool CanExpandReplies(TaskCommentModel comment)
        {
            return comment != null && comment.HasReplies;
        }

        private bool CanAddReply(TaskCommentModel comment)
        {
            return comment != null && !IsNewTask;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Thêm system comment chung
        /// </summary>
        private async Task AddSystemComment(string content)
        {
            try
            {
                var systemComment = new TaskCommentModel
                {
                    Id = 0,
                    TaskId = TaskId,
                    Content = content,
                    CommentType = CommentType.StatusUpdate,
                    CreatedAt = DateTime.Now,
                    CreatedByName = "System",
                    IsSystemComment = true,
                    Priority = TaskPriority.Low
                };

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    TaskComments.Add(systemComment);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding system comment: {ex.Message}");
            }
        }

        /// <summary>
        /// Hiển thị dialog nhập lý do xóa comment
        /// </summary>
        private async Task<string> ShowDeleteCommentReasonDialog(TaskCommentModel comment)
        {
            try
            {
                // Create a simple input dialog for deletion reason
                var inputDialog = new CommentDeletionReasonDialog(comment);

                // Safely set owner
                try
                {
                    if (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsLoaded)
                    {
                        inputDialog.Owner = Application.Current.MainWindow;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CommentLine] Could not set owner: {ex.Message}");
                    // Continue without owner
                }

                var result = inputDialog.ShowDialog();
                return result == true ? inputDialog.DeletionReason : null;
            }
            catch
            {
                // Fallback to simple input
                return await ShowSimpleInputDialog("Lý do xóa bình luận",
                    "Vui lòng nhập lý do xóa bình luận này:",
                    "Không cần thiết nữa");
            }
        }

        /// <summary>
        /// Hiển thị dialog input đơn giản
        /// </summary>
        private async Task<string> ShowSimpleInputDialog(string title, string message, string defaultValue = "")
        {
            try
            {
                // Simple input using MessageBox alternative
                var inputWindow = new Window
                {
                    Title = title,
                    Width = 400,
                    Height = 300,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Application.Current.MainWindow,
                    ResizeMode = ResizeMode.NoResize
                };

                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var stackPanel = new StackPanel { Margin = new Thickness(20) };
                var messageBlock = new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                var textBox = new TextBox
                {
                    Text = defaultValue,
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    Height = 80,
                    VerticalContentAlignment = VerticalAlignment.Top
                };

                stackPanel.Children.Add(messageBlock);
                stackPanel.Children.Add(textBox);
                Grid.SetRow(stackPanel, 0);

                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(20, 10, 20, 20)
                };

                var okButton = new Button
                {
                    Content = "OK",
                    Width = 80,
                    Height = 30,
                    Margin = new Thickness(0, 0, 10, 0),
                    IsDefault = true
                };
                var cancelButton = new Button
                {
                    Content = "Hủy",
                    Width = 80,
                    Height = 30,
                    IsCancel = true
                };

                string result = null;
                okButton.Click += (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        result = textBox.Text.Trim();
                        inputWindow.DialogResult = true;
                    }
                    else
                    {
                        MessageBox.Show("Vui lòng nhập lý do xóa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                };
                cancelButton.Click += (s, e) => inputWindow.DialogResult = false;

                buttonPanel.Children.Add(okButton);
                buttonPanel.Children.Add(cancelButton);
                Grid.SetRow(buttonPanel, 1);

                grid.Children.Add(stackPanel);
                grid.Children.Add(buttonPanel);
                inputWindow.Content = grid;

                textBox.Focus();
                textBox.SelectAll();

                var dialogResult = inputWindow.ShowDialog();
                return dialogResult == true ? result : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Thêm system comment khi có comment bị xóa
        /// </summary>
        private async Task AddSystemCommentForDeletion(TaskCommentModel deletedComment, string reason)
        {
            try
            {
                var systemComment = new TaskCommentModel
                {
                    Id = 0, // New comment
                    TaskId = TaskId,
                    Content = $"Bình luận đã bị xóa bởi {_userService.GetCurrentUser()?.FullName ?? "System"}.\n\n" +
                             $"📝 Nội dung đã xóa: \"{deletedComment.Content?.Substring(0, Math.Min(200, deletedComment.Content?.Length ?? 0))}...\"\n" +
                             $"🗑️ Lý do xóa: {reason}\n" +
                             $"⏰ Thời gian xóa: {DateTime.Now:dd/MM/yyyy HH:mm:ss}",
                    CommentType = CommentType.StatusUpdate,
                    CreatedAt = DateTime.Now,
                    CreatedByName = "System",
                    IsSystemComment = true,
                    Priority = TaskPriority.Low
                };

                // Add to the comments list immediately for UI feedback
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    TaskComments.Add(systemComment);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding system comment for deletion: {ex.Message}");
            }
        }

        /// <summary>
        /// Update computed properties when status changes
        /// </summary>
        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            // Update computed properties when relevant properties change
            switch (propertyName)
            {
                case nameof(TaskComments):
                    OnPropertyChanged(nameof(HasComments));
                    OnPropertyChanged(nameof(CommentCount));
                    OnPropertyChanged(nameof(CommentCountText));
                    OnPropertyChanged(nameof(CanAddComments));
                    break;

                case nameof(IsNewTask):
                case nameof(IsSaving):
                    OnPropertyChanged(nameof(CanAddComments));
                    break;
            }
        }

        #endregion
    }

    #region Event Args Classes

    /// <summary>
    /// Event args cho comment actions
    /// </summary>
    public class CommentActionEventArgs : EventArgs
    {
        public AuditAction Action { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public TaskCommentModel Comment { get; set; }
    }

    /// <summary>
    /// Enum cho các loại audit action
    /// </summary>
    public enum AuditAction
    {
        Create,
        Update,
        Delete,
        Reply,
        View,
        LoadError
    }

    #endregion
}