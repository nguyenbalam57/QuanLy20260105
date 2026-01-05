using ManagementFile.App.Models;
using ManagementFile.App.Models.Projects;
using ManagementFile.App.Models.Users;
using ManagementFile.App.Services;
using ManagementFile.App.ViewModels;
using ManagementFile.App.ViewModels.Controls;
using ManagementFile.App.Views.Project;
using ManagementFile.Contracts.DTOs.ProjectManagement;
using ManagementFile.Contracts.Enums;
using ManagementFile.Contracts.Enums.Extensions;
using ManagementFile.Contracts.Requests.ProjectManagement.ProjectTasks;
using ManagementFile.Contracts.Requests.ProjectManagement.TaskComments;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ManagementFile.App.ViewModels.Project
{
    /// <summary>
    /// Enhanced ViewModel cho Add/Edit/View Task Dialog với đầy đủ tính năng
    /// </summary>
    public class AddEditTaskDialogViewModel : BaseViewModel
    {
        private readonly ProjectApiService _projectApiService;
        private readonly UserManagementService _userService;
        private ProjectTaskModel _originalTask;
        private readonly AdminService _adminService;
        private readonly TaskCommentService _taskCommentService;
        private int _projectId = -1;
        private DialogMode _dialogMode;
        private string _dialogTitle;

        #region Private Fields

        // Task Code removed - no longer needed
        private string _title;
        private string _taskCode;
        private string _description;
        private TaskStatuss _status = TaskStatuss.Todo;
        private TaskPriority _priority = TaskPriority.Normal;
        private UserModel _assignedToUser;
        private int _assignedToId;
        private UserModel _reporterUser;
        private int _reporterId;
        private decimal _estimatedHours;
        private decimal _actualHours;
        private DateTime? _startDate;
        private DateTime? _dueDate;
        private DateTime? _completedAt;
        private int _completedBy = 0;
        private Department _taskType = Department.OTHER;
        private decimal _progress;
        private bool _isBlocked;
        private string _blockReason;
        private bool _isSaving;
        private bool _isLoading;
        private bool _isActive = true;
        private int? _parentTaskId = 0;
        private string _parentTaskTitle;
        private long _version = 1;

        // Collections
        private ObservableCollection<TaskStatussItem> _statusItems;
        private ObservableCollection<TaskPriorityItem> _priorityItems;
        private ObservableCollection<DepartmentItem> _taskTypeItems;
        private ObservableCollection<string> _tags;
        private ObservableCollection<string> _dependencies;
        private ObservableCollection<ProjectTaskModel> _availableParentTasks;

        private ObservableCollection<UserModel> _assignedUsers;
        private List<int> _assignedId;

        // New properties for enhanced functionality
        private string _newTag;
        private string _newDependency;
        private Dictionary<string, object> _metadata;

        #endregion

        #region Constructor

        public AddEditTaskDialogViewModel(
            ProjectApiService projectApiService,
            UserManagementService userService,
            AdminService adminService,
            TaskCommentService taskCommentService)
        {
            _projectApiService = projectApiService ?? throw new ArgumentNullException(nameof(projectApiService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));
            _taskCommentService = taskCommentService ?? throw new ArgumentNullException(nameof(taskCommentService));

            InitializeCollections();
            AssignedUsers = new ObservableCollection<UserModel>();

            InitializeCommands();
            InitializeComboBoxItems();

        }

        #endregion

        #region Enhanced Public Properties

        public ProjectTaskModel OriginalTask
        {
            get { return _originalTask; }
            set { SetProperty(ref _originalTask, value); }
        }

        public int ProjectId { get => _projectId; }

        public string TaskCode
        {
            get => _taskCode;
            set 
            {
                if(SetProperty(ref _taskCode, value))
                {
                    LoadDialogTitile();
                }
            }
        }

        public DialogMode DialogMode
        {
            get => _dialogMode;
            set
            {
                if(SetProperty(ref _dialogMode, value))
                {
                    OnModeChanged();
                }
            }
        }

        public bool IsNewTask => DialogMode == DialogMode.Add;

        public bool IsViewMode => DialogMode == DialogMode.View;

        public bool IsEditMode => DialogMode == DialogMode.Edit;

        public bool IsEditNewMode => IsEditMode || IsNewTask;

        public bool IsReadOnly => IsViewMode;

        public string DialogTitle
        {
            get => _dialogTitle;
            set => SetProperty(ref _dialogTitle, value);
        }

        public string AssignedToUserScope => "ProjectMembers"; // Placeholder for future use
        public string AssignedToUserPlaceholder => "Chọn nguời thực hiện...";

        public string ReporterUserScope => "ProjectMembers"; // Placeholder for future use
        public string ReporterUserPlaceholder => "Chọn người báo cáo...";

        public string AssignedsToUserScope => "ProjectMembers";

        public string AssignedsUserPlaceholder => "Nhập tên để chọn người thực hiện ...";

        // TaskCode property removed - no longer needed

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public TaskStatuss Status
        {
            get => _status;
            set 
            {
                if (SetProperty(ref _status, value))
                {
                    UpddatePropertyStatus();
                }
            }
        }

        public TaskPriority Priority
        {
            get => _priority;
            set => SetProperty(ref _priority, value);
        }

        public UserModel SelectedAssignedToUser
        {
            get => _assignedToUser;
            set 
            { 
                if(SetProperty(ref _assignedToUser, value))
                {
                    if(value != null)
                        AssignedToId = value.Id;
                    else
                        AssignedToId = 0;
                }    
            }
        }

        public int AssignedToId
        {
            get => _assignedToId;
            set
            {
                if (SetProperty(ref _assignedToId, value))
                {
                }
            }
        }

        public UserModel SelectedReporterUser
        {
            get => _reporterUser;
            set
            {
                if (SetProperty(ref _reporterUser, value))
                {
                    if (value != null)
                        ReporterId = value.Id;
                    else
                        ReporterId = 0;
                }
            }
        }

        public int ReporterId
        {
            get => _reporterId;
            set
            {
                if (SetProperty(ref _reporterId, value))
                {
                }
            }
        }

        public ObservableCollection<UserModel> AssignedUsers
        {
            get => _assignedUsers;
            set => SetProperty(ref _assignedUsers, value);
        }

        public decimal EstimatedHours
        {
            get => _estimatedHours;
            set => SetProperty(ref _estimatedHours, Math.Max(0, value));
        }

        public decimal ActualHours
        {
            get => _actualHours;
            set => SetProperty(ref _actualHours, Math.Max(0, value));
        }

        public DateTime? StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        public DateTime? DueDate
        {
            get => _dueDate;
            set => SetProperty(ref _dueDate, value);
        }

        public DateTime? CompletedAt
        {
            get => _completedAt;
            set => SetProperty(ref _completedAt, value);
        }

        public int CompletedBy
        {
            get => _completedBy;
            set
            {
                if (SetProperty(ref _completedBy, value))
                {
                }
            }
        }

        public Department TaskType
        {
            get => _taskType;
            set => SetProperty(ref _taskType, value);
        }

        public decimal Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, Math.Max(0, Math.Min(100, value)));
        }

        public bool IsBlocked
        {
            get => _isBlocked;
            set => SetProperty(ref _isBlocked, value);
        }

        public string BlockReason
        {
            get => _blockReason;
            set => SetProperty(ref _blockReason, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public int? ParentTaskId
        {
            get => _parentTaskId;
            set
            {
                if (SetProperty(ref _parentTaskId, value))
                {
                    UpdateParentTaskTitle();
                    OnPropertyChanged(nameof(HasParentTaskId));
                }
            }
        }

        public string ParentTaskTitle
        {
            get => _parentTaskTitle;
            set => SetProperty(ref _parentTaskTitle, value);
        }

        public bool HasParentTaskId => ParentTaskId.HasValue && ParentTaskId.Value > 0;

        // UI State Properties
        public bool IsSaving
        {
            get => _isSaving;
            set => SetProperty(ref _isSaving, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private bool _isLoadingUsers;
        public bool IsLoadingUsers 
        { 
            get => _isLoadingUsers;
            private set => SetProperty(ref _isLoadingUsers, value);
        }


        // Tag Management
        public string NewTag
        {
            get => _newTag;
            set => SetProperty(ref _newTag, value);
        }

        // Dependency Management
        public string NewDependency
        {
            get => _newDependency;
            set => SetProperty(ref _newDependency, value);
        }

        // Metadata
        public Dictionary<string, object> Metadata
        {
            get => _metadata ?? (_metadata = new Dictionary<string, object>());
            set => SetProperty(ref _metadata, value);
        }

        #endregion

        #region Collections

        public ObservableCollection<TaskStatussItem> StatusItems
        {
            get => _statusItems;
            set => SetProperty(ref _statusItems, value);
        }

        public ObservableCollection<TaskPriorityItem> PriorityItems
        {
            get => _priorityItems;
            set => SetProperty(ref _priorityItems, value);
        }

        public ObservableCollection<DepartmentItem> TaskTypeItems
        {
            get => _taskTypeItems;
            set => SetProperty(ref _taskTypeItems, value);
        }

        public ObservableCollection<string> Tags
        {
            get => _tags;
            set => SetProperty(ref _tags, value);
        }

        public ObservableCollection<string> Dependencies
        {
            get => _dependencies;
            set => SetProperty(ref _dependencies, value);
        }

        public ObservableCollection<ProjectTaskModel> AvailableParentTasks
        {
            get => _availableParentTasks;
            set => SetProperty(ref _availableParentTasks, value);
        }

        #endregion

        #region Enhanced Properties for UI

        // Expander states
        private bool _isBasicInfoExpanded = true;
        public bool IsBasicInfoExpanded
        {
            get => _isBasicInfoExpanded;
            set => SetProperty(ref _isBasicInfoExpanded, value);
        }

        private bool _isAssignmentExpanded = true;
        public bool IsAssignmentExpanded
        {
            get => _isAssignmentExpanded;
            set => SetProperty(ref _isAssignmentExpanded, value);
        }

        private bool _isCommentsExpanded = true;
        public bool IsCommentsExpanded
        {
            get => _isCommentsExpanded;
            set => SetProperty(ref _isCommentsExpanded, value);
        }

        #endregion

        #region Commands

        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand EditCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }


        #endregion

        #region Events

        public event System.EventHandler<DialogCloseEventArgs> RequestClose;

        #endregion

        #region Initialization Methods

        public async Task InitProjectIdAsyns(
            int projectId,
            ProjectTaskModel projectTaskModel = null,
            DialogMode mode = DialogMode.Add,
            int? parentTaskId = null)
        {
            _projectId = projectId;
            _parentTaskId = parentTaskId;
            OriginalTask = projectTaskModel;
            DialogMode = mode;



            await InitializeProperties();
            

        }

        private void InitializeCollections()
        {
            StatusItems = new ObservableCollection<TaskStatussItem>();
            PriorityItems = new ObservableCollection<TaskPriorityItem>();
            TaskTypeItems = new ObservableCollection<DepartmentItem>();
            Tags = new ObservableCollection<string>();
            Dependencies = new ObservableCollection<string>();
            AvailableParentTasks = new ObservableCollection<ProjectTaskModel>();
        }

        private async Task InitializeProperties()
        {
            switch (DialogMode)
            {
                case DialogMode.Add:
                    DialogTitle = "Thêm Task Mới";
                    StartDate = DateTime.Now;
                    DueDate = DateTime.Now.AddDays(7);
                    Priority = TaskPriority.Normal;
                    Status = TaskStatuss.Todo;
                    Progress = 0;
                    IsActive = true;
                    break;

                case DialogMode.Edit:
                    DialogTitle = "Chỉnh Sửa Task";
                    await LoadTaskData();
                    break;

                case DialogMode.View:
                    DialogTitle = "Chi Tiết Task";
                    await LoadTaskData();
                    break;
            }
        }

        private void LoadDialogTitile()
        {
            switch (DialogMode)
            {
                case DialogMode.Add:
                    DialogTitle = "Thêm Task Mới";
                    break;
                case DialogMode.Edit:
                    DialogTitle = $"Chỉnh Sửa Task - {TaskCode}";
                    break;
                case DialogMode.View:
                    DialogTitle = $"Chi Tiết Task - {TaskCode}";
                    break;
            }
        }

        private void InitializeCommands()
        {
            SaveCommand = new AsyncRelayCommand(ExecuteSaveAsync, CanSave);
            CancelCommand = new RelayCommand(ExecuteCancel,() => true);
            EditCommand = new AsyncRelayCommand(ExcecuteEditCommand, CanEdit);
            RefreshCommand = new AsyncRelayCommand(RefreshDataAsync);

        }

        private void InitializeComboBoxItems()
        {
            // Load Status Items
            StatusItems = TaskStatussHelper.GetTaskStatusItems();

            // Load Priority Items
            PriorityItems = TaskPriorityHelper.GetTaskPriorityItems();

            // Load Task Types
            TaskTypeItems = DepartmentExtensions.GetDepartmentItems();

        }

        private void OnModeChanged()
        {
            OnPropertyChanged(nameof(IsNewTask));
            OnPropertyChanged(nameof(IsViewMode));
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(IsReadOnly));
            OnPropertyChanged(nameof(IsEditNewMode));

            // Refresh command states when mode changes

            RefreshCommandStates();
        }

        #endregion

        #region Data Loading Methods

        private async Task LoadTaskData()
        {
            if (_originalTask == null) return;

            // TaskCode loading removed - no longer needed
            Title = _originalTask.Title ?? "";
            Description = _originalTask.Description ?? "";
            TaskCode = _originalTask.TaskCode ?? "";
            Status = _originalTask.Status;
            Priority = _originalTask.Priority;
            AssignedToId = _originalTask.AssignedToId;
            SelectedAssignedToUser = await _userService.GetUserByIdAsync(_originalTask.AssignedToId);
            ReporterId = _originalTask.ReporterId;
            SelectedReporterUser = await _userService.GetUserByIdAsync(_originalTask.ReporterId);
            EstimatedHours = _originalTask.EstimatedHours;
            ActualHours = _originalTask.ActualHours;
            StartDate = _originalTask.StartDate;
            DueDate = _originalTask.DueDate;
            CompletedAt = _originalTask.CompletedAt;
            CompletedBy = _originalTask.CompletedBy;
            TaskType = _originalTask.TaskType;
            Progress = _originalTask.Progress;
            IsBlocked = _originalTask.IsBlocked;
            BlockReason = _originalTask.BlockReason ?? "";
            IsActive = _originalTask.IsActive;
            ParentTaskId = _originalTask.ParentTaskId;
            _version = _originalTask.Version;

            // Load tags and dependencies if available
            LoadTagsFromTask();
            LoadDependenciesFromTask();
            await LoadAssignedUsersAsync();

            // Update dialog title with task information for view mode
            if (IsViewMode)
            {
                DialogTitle = $"Chi Tiết Task - {Title}";
            }
        }

        private void LoadTagsFromTask()
        {
            if (_originalTask?.Tags != null)
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    Tags.Clear();
                    foreach (var tag in _originalTask.GetTags() ?? new List<string>())
                    {
                        Tags.Add(tag);
                    }
                });
            }
        }

        private void LoadDependenciesFromTask()
        {
            // Implementation remains the same
        }

        private async Task LoadAssignedUsersAsync()
        {
            if (_originalTask?.AssignedToIds == null || _originalTask.AssignedToIds.Count == 0)
                return;

            try
            {
                // Load users
                var userTasks = _originalTask.AssignedToIds
                    .Select(id => _userService.GetUserByIdAsync(id))
                    .ToList();

                var users = await Task.WhenAll(userTasks);

                AssignedUsers = new ObservableCollection<UserModel>(users);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading assigned users: {ex.Message}");
            }
        }

        private async Task RefreshDataAsync()
        {
            await LoadTaskData();
        }

        #endregion

        #region Helper Methods

        private void UpdateParentTaskTitle()
        {
            if (ParentTaskId <= 0)
            {
                ParentTaskTitle = "";
                return;
            }

            ParentTaskTitle = $"Cha là : {ParentTaskId}";
        }



        #endregion

        #region Validation Methods

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(Title) &&
                   !IsSaving &&
                   !IsViewMode &&
                   !IsLoading;
        }

        private bool CanEdit()
        {
            var result = IsViewMode;
            Debug.WriteLine($"[{DateTime.Now}] CanEdit: {result}, IsViewMode: {IsViewMode}");
            return result;
        }

        #endregion

        #region Command Execution Methods

        private async Task ExecuteSaveAsync()
        {
            try
            {
                IsSaving = true;
                ClearValidationMessage();

                if (!ValidateInput())
                {
                    return;
                }

                if (IsNewTask)
                {
                    await CreateNewTaskAsync();
                }
                else
                {
                    await UpdateExistingTaskAsync();
                }
            }
            catch (Exception ex)
            {
                SetValidationMessage($"Lỗi: {ex.Message}");
            }
            finally
            {
                IsSaving = false;
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(Title))
            {
                SetValidationMessage("Vui lòng nhập tiêu đề task.");
                return false;
            }

            if (DueDate.HasValue && StartDate.HasValue && DueDate < StartDate)
            {
                SetValidationMessage("Ngày kết thúc phải sau ngày bắt đầu.");
                return false;
            }

            if (EstimatedHours < 0)
            {
                SetValidationMessage("Số giờ ước tính phải lớn hơn hoặc bằng 0.");
                return false;
            }

            if (Progress < 0 || Progress > 100)
            {
                SetValidationMessage("Tiến độ phải nằm trong khoảng 0-100%.");
                return false;
            }

            if (Progress == 100 && Status != TaskStatuss.Completed)
            {
                SetValidationMessage("Khi tiến độ = 100%, trạng thái phải là 'Completed'.");
                return false;
            }

            if (Status == TaskStatuss.Completed && Progress < 100)
            {
                SetValidationMessage("Khi trạng thái = 'Completed', tiến độ phải = 100%.");
                return false;
            }

            if (IsBlocked && string.IsNullOrWhiteSpace(BlockReason))
            {
                SetValidationMessage("Vui lòng nhập lý do khi task bị block.");
                return false;
            }

            return true;
        }

        private async Task CreateNewTaskAsync()
        {
            var parentTemp = ParentTaskId > 0 ? ParentTaskId : (int?)null;

            var createModel = new CreateTaskRequest
            {
                ProjectId = _projectId,
                Title = Title?.Trim(),
                Description = Description?.Trim(),
                TaskType = TaskType,
                StartDate = StartDate,
                DueDate = DueDate,
                EstimatedHours = EstimatedHours,
                AssignedToId = AssignedToId,
                AssignedToIds = AssignedUsers?.Select(u => u.Id).ToList() ?? new List<int>(),
                ReporterId = ReporterId,
                ParentTaskId = parentTemp,
                Tags = Tags?.ToList() ?? new List<string>(),
                Metadata = Metadata ?? new Dictionary<string, object>()
            };

            var result = await _projectApiService.CreateTaskAsync(_projectId, createModel);
            if (result != null)
            {
                MessageBox.Show("Tạo task thành công!", "Thành công",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                RequestClose?.Invoke(this, new DialogCloseEventArgs(true));
            }
            else
            {
                MessageBox.Show("Lỗi tạo task. Vui lòng thử lại.", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task UpdateExistingTaskAsync()
        {
            var prarentTemp = ParentTaskId > 0 ? ParentTaskId : (int?)null;


            var updateModel = new UpdateTaskRequest
            {
                // TaskCode removed - will be preserved on server side
                Title = Title?.Trim(),
                Description = Description?.Trim(),
                Status = Status,
                Priority = Priority,
                TaskType = TaskType,
                StartDate = StartDate,
                DueDate = DueDate,
                EstimatedHours = EstimatedHours,
                Progress = Progress,
                AssignedToId = AssignedToId,
                ReporterId = ReporterId,
                AssignedToIds = AssignedUsers?.Select(u => u.Id).ToList() ?? new List<int>(),
                ParentTaskId = prarentTemp,
                CompletedAt = Status == TaskStatuss.Completed ? (CompletedAt ?? DateTime.Now) : null as DateTime?,
                CompletedBy = Status == TaskStatuss.Completed ? CompletedBy : -1,
                IsBlocked = IsBlocked,
                BlockReason = IsBlocked ? BlockReason?.Trim() : "",
                IsActive = IsActive,
                Version = _version,
                Tags = Tags?.ToList() ?? new List<string>(),
                Metadata = Metadata ?? new Dictionary<string, object>()
            };

            var result = await _projectApiService.UpdateTaskAsync(_originalTask.ProjectId, _originalTask.Id, updateModel);

            if (result != null)
            {
                MessageBox.Show("Cập nhật task thành công!", "Thành công",
                           MessageBoxButton.OK, MessageBoxImage.Information);
                RequestClose?.Invoke(this, new DialogCloseEventArgs(true));
            }
            else
            {
                MessageBox.Show("Lỗi cập nhật task. Vui lòng thử lại.", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExcecuteEditCommand()
        {
            try
            {
                Debug.WriteLine($"[{DateTime.Now}] EditCommand executed - Current Mode: {_dialogMode}");

                if (!IsViewMode)
                {
                    Debug.WriteLine($"[{DateTime.Now}] Already in Edit mode or not in View mode. Skipping...");
                    return;
                }

                // Change mode first
                DialogMode = DialogMode.Edit;
                DialogTitle = "Chỉnh Sửa Task";

                Debug.WriteLine($"[{DateTime.Now}] Mode changed to: {_dialogMode}");

                // Notify all mode-related properties
                OnModeChanged();

                // Refresh commands CanExecute
                RefreshCommandStates();

                //await InitializeProperties();

                Debug.WriteLine($"[{DateTime.Now}] Mode changed to Edit successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ExcecuteEditCommand: {ex.Message}");
                MessageBox.Show($"Lỗi khi chuyển sang chế độ chỉnh sửa: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteCancel()
        {
            try
            {
                Debug.WriteLine($"[{DateTime.Now}] CancelCommand executed");

                var result = MessageBox.Show(
                    "Bạn có chắc muốn hủy? Các thay đổi chưa lưu sẽ bị mất.",
                    "Xác nhận",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    RequestClose?.Invoke(this, new DialogCloseEventArgs(false, "Cancelled by user"));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ExecuteCancel: {ex.Message}");
                MessageBox.Show($"Lỗi khi đóng dialog: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshCommandStates()
        {
            try
            {
                Debug.WriteLine($"[{DateTime.Now}] RefreshCommandStates called");

                // Trigger CanExecuteChanged for all commands
                (SaveCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (EditCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (CancelCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (RefreshCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();

                Debug.WriteLine($"[{DateTime.Now}] SaveCommand CanExecute: {SaveCommand?.CanExecute(null)}");
                Debug.WriteLine($"[{DateTime.Now}] EditCommand CanExecute: {EditCommand?.CanExecute(null)}");
                Debug.WriteLine($"[{DateTime.Now}] CancelCommand CanExecute: {CancelCommand?.CanExecute(null)}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in RefreshCommandStates: {ex.Message}");
            }
        }

        #endregion

        #region Enhanced Comment Helper Methods

        private void UpddatePropertyStatus()
        {
            if(Status == TaskStatuss.Completed)
            {
                Progress = Progress > (decimal)0 ? Progress : (decimal)100;
                CompletedAt = CompletedAt ?? DateTime.Now;
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
                case nameof(Status):
                    break;

                case nameof(IsViewMode):
                case nameof(IsNewTask):
                case nameof(IsEditMode):
                    RefreshCommandStates(); // Thêm dòng này
                    break;

                case nameof(Title):
                case nameof(IsSaving):
                case nameof(IsLoading):
                    RefreshCommandStates(); // Thêm dòng này
                    break;
            }
        }


        #endregion

        #region Computed Properties

        private string _validationMessage = "";
        public string ValidationMessage
        {
            get => _validationMessage;
            private set => SetProperty(ref _validationMessage, value);
        }

        public bool HasValidationMessage => IsEditNewMode && (!string.IsNullOrEmpty(ValidationMessage) && !IsViewMode);

        public bool HasParentTask => ParentTaskId > 0;

        public bool HasTags => Tags?.Count > 0;

        public bool HasDependencies => Dependencies?.Count > 0;

        public bool IsSubTask => ParentTaskId > 0;

        public bool IsOverdue
        {
            get
            {
                return DueDate.HasValue &&
                       DateTime.Now > DueDate.Value &&
                       Status != TaskStatuss.Completed;
            }
        }

        public bool IsCompleted => Status == TaskStatuss.Completed;


        public string TagsDisplay => Tags?.Count > 0 ? string.Join(", ", Tags) : "Không có tags";

        public string DependenciesDisplay => Dependencies?.Count > 0 ? string.Join(", ", Dependencies) : "Không có dependencies";

        public string StatusDisplayText
        {
            get
            {
                return EnumExtensions.GetDescription(Status);
            }
        }

        public string PriorityDisplayText
        {
            get
            {
                return EnumExtensions.GetDescription(Priority);
            }
        }

        #endregion

        #region Helper Methods

        protected void SetValidationMessage(string message)
        {
            ValidationMessage = message;
            OnPropertyChanged(nameof(HasValidationMessage));
        }

        protected void ClearValidationMessage()
        {
            SetValidationMessage("");
        }

        #endregion

        /// <summary>
        /// Comparer for UserModel to avoid duplicates
        /// </summary>
        private class UserModelComparer : IEqualityComparer<UserModel>
        {
            public bool Equals(UserModel x, UserModel y)
            {
                return x?.Id == y?.Id;
            }

            public int GetHashCode(UserModel obj)
            {
                return obj?.Id.GetHashCode() ?? 0;
            }
        }
    }
}