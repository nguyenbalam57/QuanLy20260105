using ManagementFile.App.Models;
using ManagementFile.App.Models.Dialogs;
using ManagementFile.App.Models.Projects;
using ManagementFile.App.Models.Projects.PermissionProjects;
using ManagementFile.App.Services;
using ManagementFile.App.ViewModels.Project;
using ManagementFile.App.Views.Dialogs.Comments;
using ManagementFile.App.Views.Project;
using ManagementFile.Contracts.DTOs.ProjectManagement;
using ManagementFile.Contracts.Enums;
using ManagementFile.Contracts.Requests.ProjectManagement.Projects;
using ManagementFile.Contracts.Requests.ProjectManagement.ProjectTasks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ManagementFile.App.ViewModels.Controls.Projects
{
    public class ProjectTasksControlViewModel : BaseViewModel
    {

        #region Fields

        private readonly ProjectApiService _projectApiService;
        private readonly UserManagementService _userManagementService;
        private readonly AdminService _adminService;

        // Loading states
        private bool _isLoading;
        private string _loadingMessage;

        // Search and filter
        private string _taskSearchKeyword;
        private TaskStatuss _taskSelectedStatus = TaskStatuss.All;

        // Collections
        private ObservableCollection<ProjectTaskModel> _projectTasks;
        private ObservableCollection<ProjectTaskModel> _filteredTasks;

        // Selected items
        private ProjectModel _selectedProject;
        private ProjectTaskModel _selectedTask;

        // Hierarchy support fields
        private ProjectTaskModel _currentParentTask;
        private ObservableCollection<ProjectTaskModel> _breadcrumbPath;
        private Dictionary<int, bool> _expandedTasksCache;

        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalPages = 1;

        private bool _canLoadMoreTasks = true;

        #endregion

        #region Constructor

        public ProjectTasksControlViewModel(
            ProjectApiService projectApiService,
            UserManagementService userManagementService,
            AdminService adminService)
        {
            _projectApiService = projectApiService ?? throw new ArgumentNullException(nameof(projectApiService));
            _userManagementService = userManagementService ?? throw new ArgumentNullException(nameof(userManagementService));
            _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));

            // ✅ Initialize hierarchy cache
            _expandedTasksCache = new Dictionary<int, bool>();
            _breadcrumbPath = new ObservableCollection<ProjectTaskModel>();

            InitializeCommands();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Đang loading
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Loading message
        /// </summary>
        public string LoadingMessage
        {
            get => _loadingMessage ?? "Đang tải dữ liệu...";
            set => SetProperty(ref _loadingMessage, value);
        }

        /// <summary>
        /// Search keyword cho tasks
        /// </summary>
        public string TaskSearchKeyword
        {
            get => _taskSearchKeyword;
            set
            {
                if (SetProperty(ref _taskSearchKeyword, value))
                {
                    RefreshFlattenedTasks();
                }
            }
        }

        /// <summary>
        /// Selected task status filter
        /// </summary>
        public TaskStatuss TaskSelectedStatus
        {
            get => _taskSelectedStatus;
            set
            {
                if (SetProperty(ref _taskSelectedStatus, value))
                {
                    RefreshFlattenedTasks();
                }
            }
        }

        public ObservableCollection<TaskStatussItem> TaskStatussItems
        {
            get;
            set;
        }

        /// <summary>
        /// Project tasks
        /// </summary>
        public ObservableCollection<ProjectTaskModel> ProjectTasks
        {
            get => _projectTasks;
            set => SetProperty(ref _projectTasks, value);
        }

        /// <summary>
        /// Filtered tasks for display
        /// </summary>
        public ObservableCollection<ProjectTaskModel> FilteredTasks
        {
            get => _filteredTasks;
            set => SetProperty(ref _filteredTasks, value);
        }

        /// <summary>
        /// Selected project
        /// </summary>
        public ProjectModel SelectedProject
        {
            get => _selectedProject;
            set
            {
                if (SetProperty(ref _selectedProject, value))
                {
                    OnPropertyChanged(nameof(HasSelectedProject));
                    OnPropertyChanged(nameof(HasSelectedTask));
                    OnPropertyChanged(nameof(SelectedProjectInfo));

                    // Reset hierarchy navigation
                    CurrentParentTask = null;
                    BreadcrumbPath.Clear();

                    // Khi đổi project, load lại tasks
                    Task.Run(async () => await LoadProjectTasksAsync());
                }
            }
        }

        /// <summary>
        /// Selected task
        /// </summary>
        public ProjectTaskModel SelectedTask
        {
            get => _selectedTask;
            set
            {
                if (SetProperty(ref _selectedTask, value))
                {
                    OnPropertyChanged(nameof(HasSelectedTask));
                    OnPropertyChanged(nameof(SelectedTaskInfo));
                    OnPropertyChanged(nameof(CanMoveTask));
                }
            }
        }

        // Hierarchy Properties

        /// <summary>
        /// Current parent task being viewed (null = root level)
        /// </summary>
        public ProjectTaskModel CurrentParentTask
        {
            get => _currentParentTask;
            set
            {
                if (SetProperty(ref _currentParentTask, value))
                {
                    OnPropertyChanged(nameof(IsViewingRootLevel));
                    OnPropertyChanged(nameof(IsViewingSubTasks));
                    OnPropertyChanged(nameof(CurrentLevelInfo));
                }
            }
        }

        /// <summary>
        /// Breadcrumb path từ root đến current level
        /// </summary>
        public ObservableCollection<ProjectTaskModel> BreadcrumbPath
        {
            get => _breadcrumbPath;
            set => SetProperty(ref _breadcrumbPath, value);
        }

        /// <summary>
        /// Đang xem root level (không có parent task)
        /// </summary>
        public bool IsViewingRootLevel => CurrentParentTask == null;

        /// <summary>
        /// Đang xem subtasks của một task
        /// </summary>
        public bool IsViewingSubTasks => CurrentParentTask != null;

        /// <summary>
        /// Thông tin level hiện tại
        /// </summary>
        public string CurrentLevelInfo
        {
            get
            {
                if (IsViewingRootLevel)
                    return "Root Tasks";
                return $"Subtasks of: {CurrentParentTask?.Title ?? CurrentParentTask?.TaskName}";
            }
        }

        // UI Helper Properties
        public bool HasSelectedProject => SelectedProject != null;
        public bool HasSelectedTask => SelectedProject != null && SelectedTask != null;

        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        public int PageSize
        {
            get => _pageSize;
            set => SetProperty(ref _pageSize, value);
        }

        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        public bool CanLoadMoreTasks
        {
            get => _canLoadMoreTasks;
            set => SetProperty(ref _canLoadMoreTasks, value);
        }

        /// <summary>
        /// Có thể move task (task được chọn và không phải root project)
        /// </summary>
        public bool CanMoveTask => HasSelectedTask && SelectedTask.ParentTaskId.HasValue;

        public string SelectedProjectInfo
        {
            get
            {
                if (SelectedProject == null)
                    return "Chưa chọn project nào";
                return $"{SelectedProject.ProjectName} ({SelectedProject.ProjectCode})";
            }
        }

        public string SelectedTaskInfo
        {
            get
            {
                if (SelectedTask == null)
                    return "Chưa chọn task nào";
                var taskName = !string.IsNullOrEmpty(SelectedTask.TaskName) ? SelectedTask.TaskName : SelectedTask.Title;
                return $"{taskName} ({SelectedTask.TaskCode})";
            }
        }

        public string TaskFilterInfo
        {
            get
            {
                var totalCount = ProjectTasks?.Count ?? 0;
                var filteredCount = FilteredTasks?.Count ?? 0;

                if (string.IsNullOrEmpty(TaskSearchKeyword))
                    return $"Hiển thị {filteredCount} / {totalCount} tasks";

                return $"Tìm thấy {filteredCount} / {totalCount} tasks";
            }
        }


        #endregion

        #region Commands

        public ICommand SearchTasksCommand { get; private set; }
        public ICommand FilterTasksByStatusCommand { get; private set; }
        public ICommand AddTaskCommand { get; private set; }
        public ICommand EditTaskCommand { get; private set; }
        public ICommand DeleteTaskCommand { get; private set; }
        public ICommand ViewTaskDetailsCommand { get; private set; }
        public ICommand ViewTaskCommentCommand { get; private set; }
        public ICommand StartTimeLogCommand { get; private set; }
        public ICommand StopTimeLogCommand { get; private set; }
        public ICommand RefreshTasksCommand { get; private set; }

        // Hierarchy Commands
        public ICommand ExpandTaskCommand { get; private set; }
        public ICommand CollapseTaskCommand { get; private set; }
        public ICommand NavigateToSubTasksCommand { get; private set; }
        public ICommand NavigateToParentCommand { get; private set; }
        public ICommand NavigateToBreadcrumbCommand { get; private set; }
        public ICommand MoveTaskCommand { get; private set; }
        public ICommand AddSubTaskCommand { get; private set; }
        public ICommand ViewTaskHierarchyCommand { get; private set; }


        public ICommand UpdateTaskProgressCommand { get; private set; }

        // Hiên thị bảng chấm công tuần
        public ICommand TimeTrackingCommand { get; private set; }

        private void InitializeCommands()
        {
            // Task commands
            SearchTasksCommand = new RelayCommand<string>(ExecuteSearchTasks);
            FilterTasksByStatusCommand = new RelayCommand<TaskStatuss>(ExecuteFilterTasksByStatus);
            AddTaskCommand = new RelayCommand(async () => await ExecuteAddTask(), () => HasSelectedProject);
            EditTaskCommand = new RelayCommand(ExecuteEditTask, () => HasSelectedTask);
            DeleteTaskCommand = new RelayCommand(async () => await ExecuteDeleteTaskAsync(), () => HasSelectedTask);
            ViewTaskDetailsCommand = new RelayCommand(ExecuteViewTaskDetails, () => HasSelectedTask);
            ViewTaskCommentCommand = new RelayCommand(() => ExecuteViewTaskComment(), () => HasSelectedTask);
            RefreshTasksCommand = new RelayCommand(async () => await RefreshTasksAsync());

            // Hierarchy commands
            ExpandTaskCommand = new RelayCommand<ProjectTaskModel>(async (task) => await ExecuteExpandTaskAsync(task));
            CollapseTaskCommand = new RelayCommand<ProjectTaskModel>(ExecuteCollapseTask);
            NavigateToSubTasksCommand = new RelayCommand<ProjectTaskModel>(async (task) => await ExecuteNavigateToSubTasksAsync(task));
            NavigateToParentCommand = new RelayCommand(async () => await ExecuteNavigateToParentAsync(), () => IsViewingSubTasks);
            NavigateToBreadcrumbCommand = new RelayCommand<ProjectTaskModel>(async (task) => await ExecuteNavigateToBreadcrumbAsync(task));
            MoveTaskCommand = new RelayCommand(async () => await ExecuteMoveTaskAsync(), () => CanMoveTask);
            AddSubTaskCommand = new RelayCommand(ExecuteAddSubTask, () => HasSelectedTask);
            ViewTaskHierarchyCommand = new RelayCommand(async () => await ExecuteViewTaskHierarchyAsync(), () => HasSelectedTask);

            UpdateTaskProgressCommand = new AsyncRelayCommand(ExecuteProgresProjectAsync, () => HasSelectedTask);

            TimeTrackingCommand = new RelayCommand(() => ExecuteTimeTracking(), () => HasSelectedProject);

            ProjectTasks = new ObservableCollection<ProjectTaskModel>();
            FilteredTasks = new ObservableCollection<ProjectTaskModel>();
            TaskStatussItems = TaskStatussHelper.GetAllTaskStatusItems();
        }

        #endregion

        #region Methods

        private async Task RefreshTasksAsync()
        {
            await LoadProjectTasksAsync();
        }

        public async void InitializeAsync(ProjectModel projectModel)
        {
            SelectedProject = projectModel;

            await LoadProjectTasksAsync();
        }

        /// <summary>
        /// Load tasks của project hiện tại
        /// Hỗ trợ load theo hierarchy level
        /// </summary>
        private async Task LoadProjectTasksAsync()
        {
            if (SelectedProject == null) return;

            try
            {
                IsLoading = true;
                LoadingMessage = "Đang tải danh sách tasks...";

                var allTasks = new List<ProjectTaskModel>();

                System.Diagnostics.Debug.WriteLine(
                    $"🔄 Loading tasks for project {SelectedProject.ProjectName} ");

                int oldPages = CurrentPage;

                for (int i = 1; i <= oldPages; i++)
                {
                    LoadingMessage = $"Đang tải trang {i + 1}/{oldPages}...";
                    var pagedResult = await LoadMoreProjectsTaskAsync(i, PageSize);
                    allTasks.AddRange(pagedResult);
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    ProjectTasks.Clear();
                    foreach (var task in allTasks)
                    {
                        // Restore expanded state from cache
                        if (_expandedTasksCache.ContainsKey(task.Id))
                        {
                            task.IsExpanded = _expandedTasksCache[task.Id];
                        }
                        
                        ProjectTasks.Add(task);
                    }

                    // Use RefreshFlattenedTasks() instead of ApplyTaskFilter()
                    RefreshFlattenedTasks();
                    
                    OnPropertyChanged(nameof(TaskFilterInfo));

                    System.Diagnostics.Debug.WriteLine(
                        $"✅ Loaded {allTasks.Count} tasks");
                });

                if (CurrentPage < TotalPages)
                {
                    CanLoadMoreTasks = true;
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading tasks: {ex.Message}");

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Lỗi tải danh sách tasks: {ex.Message}", "Lỗi",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task<List<ProjectTaskModel>> LoadMoreProjectsTaskAsync(int page, int pageSize)
        {
            if (SelectedProject == null) return new List<ProjectTaskModel>();
            
            try
            {

                var filter = new TaskFilterRequest
                {
                    ProjectId = SelectedProject.Id,
                    SearchTerm = TaskSearchKeyword,
                    Status = TaskSelectedStatus != TaskStatuss.All ? (TaskStatuss?)TaskSelectedStatus : null,
                    PageNumber = page,
                    PageSize = pageSize,
                    SortDirection = "asc",
                };

                var result = await _projectApiService.GetProjectTasksAsync(filter);
                
                if (result?.Data != null)
                {
                    var allTasks = result.Data
                        .Select(dto => ProjectTaskModel.MapToProjectTaskModel(dto))
                        .ToList();
                    System.Diagnostics.Debug.WriteLine($"✅ Loaded total {allTasks.Count} tasks");
                    
                    if(result.TotalPages > 0)
                    {
                        TotalPages = result.TotalPages;
                        OnPropertyChanged(nameof(TotalPages));
                    }

                    if(result.PageNumber > 0)
                    {
                        CurrentPage = result.PageNumber;
                        OnPropertyChanged(nameof(CurrentPage));
                    }


                    return allTasks;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ No tasks found");
                    return new List<ProjectTaskModel>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading all tasks: {ex.Message}");
                MessageBox.Show($"Lỗi tải tất cả tasks: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<ProjectTaskModel>();
            }

        }

        #endregion

        #region Command Implementations

        private void ExecuteSearchTasks(string keyword)
        {
            TaskSearchKeyword = keyword;
        }

        private void ExecuteFilterTasksByStatus(TaskStatuss status)
        {
            TaskSelectedStatus = status;
        }

        private async Task ExecuteAddTask()
        {
            if (SelectedProject == null) return;

            try
            {
                var dialog = AddEditTaskDialog.CreateAddDialog(
                    SelectedProject.Id); 

                var result = dialog.ShowDialog();

                if (result == true)
                {
                    // Reload tasks after adding
                     await LoadProjectTasksAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi mở dialog tạo task: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteEditTask()
        {
            if (SelectedTask == null) return;

            try
            {
                var dialog = AddEditTaskDialog.CreateEditDialog(SelectedTask.ProjectId, SelectedTask);
                var result = dialog.ShowDialog();

                if (result == true)
                {
                    // Reload tasks after editing
                    Task.Run(async () => await LoadProjectTasksAsync());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi mở dialog sửa task: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteDeleteTaskAsync()
        {
            if (SelectedProject == null || SelectedTask == null) return;

            try
            {
                var confirmResult = MessageBox.Show(
                    $"Bạn có chắc chắn muốn xóa công việc '{SelectedTask.Title}'?",
                    "Xác nhận xóa",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmResult != MessageBoxResult.Yes)
                    return;

                IsLoading = true;
                LoadingMessage = "Đang xóa công việc...";

                var success = await _projectApiService.DeleteTaskAsync(SelectedProject.Id, SelectedTask.Id);

                if (success)
                {
                    MessageBox.Show("Xóa task thành công!", "Thành công",
                                  MessageBoxButton.OK, MessageBoxImage.Information);

                    // Reload tasks
                    await LoadProjectTasksAsync();
                }
                else
                {
                    MessageBox.Show("Xóa task thất bại!", "Lỗi",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xóa task: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteViewTaskDetails()
        {
            if (SelectedTask == null) return;

            try
            {
                var dialog = AddEditTaskDialog.CreateViewDialog(SelectedTask.ProjectId, SelectedTask);
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi mở dialog xem task: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteViewTaskComment()
        {
            if (SelectedTask == null) return;
            try
            {
                App.GetRequiredService<ProjectManagentsDragablzViewViewModel>()
                   .AddTabTaskComment(SelectedTask);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi mở dialog xem bình luận task: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteTimeTracking()
        {
            if( SelectedProject == null) return;
            try
            {
                App.GetRequiredService<ProjectManagentsDragablzViewViewModel>()
                   .AddTabTimeTrackings(SelectedProject);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi mở bảng chấm công tuần: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Hierarchy Command Implementations

        /// <summary>
        /// Expand task để show subtasks inline
        /// </summary>
        private async Task ExecuteExpandTaskAsync(ProjectTaskModel task)
        {
            if (task == null || !task.HasSubTasks) return;

            try
            {
                task.IsLoadingSubTasks = true;
                LoadingMessage = $"Đang tải subtasks của {task.Title}...";

                var subtasks = await _projectApiService.GetSubTasksAsync(SelectedProject.Id, task.Id);

                if (subtasks != null && subtasks.Count > 0)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        task.SubTasks.Clear();
                        foreach (var subtaskDto in subtasks)
                        {
                            var subtaskModel = ProjectTaskModel.MapToProjectTaskModel(subtaskDto);
                            // Set hierarchy level for subtask
                            subtaskModel.HierarchyLevel = task.HierarchyLevel + 1;
                            task.SubTasks.Add(subtaskModel);
                        }

                        task.IsExpanded = true;
                        _expandedTasksCache[task.Id] = true;

                        // Refresh flattened tasks để hiển thị subtasks
                        RefreshFlattenedTasks();

                        System.Diagnostics.Debug.WriteLine($"✅ Expanded task {task.Title} with {subtasks.Count} subtasks");
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi load subtasks: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                task.IsLoadingSubTasks = false;
            }
        }

        /// <summary>
        /// Collapse task để ẩn subtasks
        /// </summary>
        private void ExecuteCollapseTask(ProjectTaskModel task)
        {
            if (task == null) return;

            task.IsExpanded = false;
            _expandedTasksCache[task.Id] = false;
            // Don't clear SubTasks, just hide them
            
            // Refresh flattened tasks để ẩn subtasks
            RefreshFlattenedTasks();

            System.Diagnostics.Debug.WriteLine($"✅ Collapsed task {task.Title}");
        }

        /// <summary>
        /// Navigate vào subtasks của một task (full screen navigation)
        /// </summary>
        private async Task ExecuteNavigateToSubTasksAsync(ProjectTaskModel task)
        {
            if (task == null || !task.HasSubTasks) return;

            try
            {
                // Update breadcrumb path
                if (CurrentParentTask != null && !BreadcrumbPath.Contains(CurrentParentTask))
                {
                    BreadcrumbPath.Add(CurrentParentTask);
                }

                // Set current parent
                CurrentParentTask = task;

                // Reload tasks at new level
                await LoadProjectTasksAsync();

                System.Diagnostics.Debug.WriteLine($"✅ Navigated to subtasks of {task.Title}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi navigate: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Navigate về parent task (back navigation)
        /// </summary>
        private async Task ExecuteNavigateToParentAsync()
        {
            if (CurrentParentTask == null) return;

            try
            {
                // Get parent from breadcrumb
                if (BreadcrumbPath.Count > 0)
                {
                    var lastIndex = BreadcrumbPath.Count - 1;
                    CurrentParentTask = BreadcrumbPath[lastIndex];
                    BreadcrumbPath.RemoveAt(lastIndex);
                }
                else
                {
                    // Back to root
                    CurrentParentTask = null;
                }

                // Reload tasks at new level
                await LoadProjectTasksAsync();

                System.Diagnostics.Debug.WriteLine($"✅ Navigated to parent");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi navigate về parent: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Navigate đến một level trong breadcrumb
        /// </summary>
        private async Task ExecuteNavigateToBreadcrumbAsync(ProjectTaskModel task)
        {
            if (task == null) return;

            try
            {
                var index = BreadcrumbPath.IndexOf(task);
                if (index >= 0)
                {
                    // Remove items after clicked item
                    while (BreadcrumbPath.Count > index + 1)
                    {
                        BreadcrumbPath.RemoveAt(BreadcrumbPath.Count - 1);
                    }

                    CurrentParentTask = task;
                    await LoadProjectTasksAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi navigate breadcrumb: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Move task sang parent mới
        /// </summary>
        private async Task ExecuteMoveTaskAsync()
        {
            if (SelectedTask == null) return;

            try
            {
                // TODO: Show dialog để chọn new parent task
                // For now, move to root (parentId = null)

                var confirmResult = MessageBox.Show(
                    $"Di chuyển task '{SelectedTask.Title}' về root level?",
                    "Xác nhận di chuyển",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmResult != MessageBoxResult.Yes)
                    return;

                IsLoading = true;
                LoadingMessage = "Đang di chuyển task...";

                var movedTask = await _projectApiService.MoveTaskAsync(
                    SelectedProject.Id,
                    SelectedTask.Id,
                    null, // newParentTaskId = null = move to root
                    "Moved by user");

                if (movedTask != null)
                {
                    MessageBox.Show("Di chuyển task thành công!", "Thành công",
                                  MessageBoxButton.OK, MessageBoxImage.Information);

                    await LoadProjectTasksAsync();
                }
                else
                {
                    MessageBox.Show("Di chuyển task thất bại!", "Lỗi",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi di chuyển task: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Thêm subtask cho task hiện tại
        /// </summary>
        private void ExecuteAddSubTask()
        {
            if (SelectedTask == null) return;

            try
            {
                // Open dialog với parentTaskId = SelectedTask.Id
                var dialog = AddEditTaskDialog.CreateAddSubTaskDialog(
                    SelectedProject.Id,
                    SelectedTask.Id);

                var result = dialog.ShowDialog();

                if (result == true)
                {
                    // Reload to show new subtask
                    Task.Run(async () => await LoadProjectTasksAsync());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tạo subtask: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Xem full hierarchy tree của task
        /// </summary>
        private async Task ExecuteViewTaskHierarchyAsync()
        {
            if (SelectedTask == null) return;

            try
            {
                IsLoading = true;
                LoadingMessage = "Đang tải hierarchy tree...";

                var hierarchyTask = await _projectApiService.GetTaskHierarchyAsync(
                    SelectedProject.Id,
                    SelectedTask.Id,
                    maxDepth: 5);

                if (hierarchyTask != null)
                {
                    // TODO: Show hierarchy tree in a dialog or panel
                    MessageBox.Show(
                        $"Task: {hierarchyTask.Title}\n" +
                        $"Total Subtasks: {hierarchyTask.TotalChildCount}\n" +
                        $"Direct Children: {hierarchyTask.SubTasks?.Count ?? 0}",
                        "Task Hierarchy",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi load hierarchy: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Cap nhat tien do

        private async Task ExecuteProgresProjectAsync()
        {
            if (SelectedProject == null || SelectedTask == null) return;

            try
            {
                IsLoading = true;
                LoadingMessage = "Đang tiến hành dự án...";


                var comment = new Dictionary<string, CommentLineFieldConfig>
                {
                    { "Progress" , new CommentLineFieldConfig 
                        {
                            Label = "Phần trăm hoàn thành",
                            Placeholder = "Tiến độ từ 0 -> 100.",
                            Required = true,
                        }
                    },
                    { "ActualHours" , new CommentLineFieldConfig
                        {
                            Label = "Thời gian hoàn thành thực tế (giờ).",
                            Placeholder = "",
                            Required = true,
                        }
                    },
                };

                var title = $"Tiến độ dự án: {SelectedProject.ProjectName}";
                var message = "Vui lòng nhập thông tin tiến độ dự án:";
                var commentDefauls = new Dictionary<string, string>
                {
                    { "Progress", SelectedTask.Progress.ToString() },
                    { "ActualHours", SelectedTask.ActualHours.ToString() },
                };

                var progressInput = CommentLine.Show(title: title,
                                                      message: message,
                                                      fields: comment,
                                                      defaultValues: commentDefauls);

                if (progressInput != null)
                {
                    var progressRequest = new TaskProgressUpdateRequest
                    {
                        Progress = decimal.Parse(progressInput["Progress"]),
                        ActualHours = int.Parse(progressInput["ActualHours"]),
                    };

                    var result = await _projectApiService.UpdateTaskProgressAsync(SelectedProject.Id, SelectedTask.Id, progressRequest);
                    if (result != null)
                    {
                        MessageBox.Show("Cập nhật tiến độ công việc thành công!", "Thành công",
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                        // Reload projects
                        await LoadProjectTasksAsync();
                    }
                }


            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error progressing project {SelectedProject.Id}: {ex.Message}");
                MessageBox.Show($"Lỗi cập nhật tiến độ công việc: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Hierarchy Helper Methods

        /// <summary>
        /// Refresh flattened tasks để hiển thị hierarchy trong DataGrid
        /// Pattern copied from ProjectsControlViewModel.RefreshFlattenedProjects()
        /// Insert "Load More" placeholder rows
        /// </summary>
        public void RefreshFlattenedTasks()
        {
            try
            {
                // Flatten hierarchy với respect expand state
                var flattened = FlattenTaskHierarchyWithLoadMore(ProjectTasks, respectExpandState: true);
                
                // Set hierarchy levels
                SetTaskHierarchyLevels(ProjectTasks);

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    // Apply filter to flattened list
                    ApplyTaskFilterToFlattened(flattened);
                });

                OnPropertyChanged(nameof(TaskFilterInfo));

                System.Diagnostics.Debug.WriteLine(
                    $"🔄 Refreshed flattened tasks: {FilteredTasks.Count} visible from {CountAllTasks(ProjectTasks)} total");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing flattened tasks: {ex.Message}");
            }
        }

        /// <summary>
        /// Flatten task hierarchy WITH "Load More" placeholder rows
        /// </summary>
        private List<ProjectTaskModel> FlattenTaskHierarchyWithLoadMore(
            IEnumerable<ProjectTaskModel> tasks, 
            bool respectExpandState = true)
        {
            var result = new List<ProjectTaskModel>();

            if (tasks == null) return result;

            foreach (var task in tasks.OrderBy(t => t.CreatedAt))
            {
                result.Add(task);

                // Chỉ thêm subtasks nếu task được expand
                if ((!respectExpandState || task.IsExpanded) && task.SubTasks != null && task.SubTasks.Count > 0)
                {
                    // Add subtasks recursively
                    var flattenedSubTasks = FlattenTaskHierarchyWithLoadMore(task.SubTasks, respectExpandState);
                    result.AddRange(flattenedSubTasks);

                    // Insert "Load More" placeholder row if needed
                    if (task.ShowLoadMoreSubTasksButton)
                    {
                        var loadMorePlaceholder = CreateLoadMorePlaceholder(task);
                        result.Add(loadMorePlaceholder);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Create a special placeholder task for "Load More" button
        /// </summary>
        private ProjectTaskModel CreateLoadMorePlaceholder(ProjectTaskModel parentTask)
        {
            return new ProjectTaskModel
            {
                // Special marker properties
                Id = -parentTask.Id, // Negative ID to mark as placeholder
                TaskCode = $"__LOADMORE_{parentTask.Id}__",
                TaskName = "── Load More Subtasks ──",
                Title = parentTask.LoadMoreSubTasksButtonText,

                // Hierarchy properties
                HierarchyLevel = parentTask.HierarchyLevel + 1,
                ParentTaskId = parentTask.Id,
                IsSubTask = true,

                // Visual properties
                Description = parentTask.LoadMoreSubTasksTooltip,

                // Disable all interactions except click
                Status = TaskStatuss.InProgress,
                Priority = TaskPriority.Low,

                // Link back to parent
                ProjectId = parentTask.ProjectId,

                TotalSubTaskCount = parentTask.TotalSubTaskCount,

                // Mark as special placeholder
                IsActive = false // Use this to identify placeholder rows


            };
        }

        /// <summary>
        /// Check if a task is a "Load More" placeholder
        /// </summary>
        public bool IsLoadMorePlaceholder(ProjectTaskModel task)
        {
            return task != null && 
                   task.Id < 0 && 
                   task.TaskCode?.StartsWith("__LOADMORE_") == true;
        }

        /// <summary>
        /// Get parent task ID from "Load More" placeholder
        /// </summary>
        public int GetParentTaskIdFromPlaceholder(ProjectTaskModel placeholder)
        {
            if (!IsLoadMorePlaceholder(placeholder))
                return 0;

            var idPart = placeholder.TaskCode.Replace("__LOADMORE_", "").Replace("__", "");
            if (int.TryParse(idPart, out int parentId))
            {
                return parentId;
            }

            return 0;
        }

        /// <summary>
        /// Flatten task hierarchy thành danh sách phẳng để hiển thị
        /// ⚠️ DEPRECATED: Use FlattenTaskHierarchyWithLoadMore instead
        /// </summary>
        private List<ProjectTaskModel> FlattenTaskHierarchy(
            IEnumerable<ProjectTaskModel> tasks, 
            bool respectExpandState = true)
        {
            var result = new List<ProjectTaskModel>();

            if (tasks == null) return result;

            foreach (var task in tasks.OrderBy(t => t.CreatedAt))
            {
                result.Add(task);

                // Chỉ thêm subtasks nếu task được expand (hoặc không respect expand state)
                if ((!respectExpandState || task.IsExpanded) && task.SubTasks != null && task.SubTasks.Count > 0)
                {
                    var flattenedSubTasks = FlattenTaskHierarchy(task.SubTasks, respectExpandState);
                    result.AddRange(flattenedSubTasks);
                }
            }

            return result;
        }

        /// <summary>
        /// Set hierarchy level cho tất cả tasks recursively
        /// </summary>
        private void SetTaskHierarchyLevels(IEnumerable<ProjectTaskModel> tasks, int level = 0)
        {
            if (tasks == null) return;

            foreach (var task in tasks)
            {
                task.HierarchyLevel = level;
                if (task.SubTasks != null && task.SubTasks.Count > 0)
                {
                    SetTaskHierarchyLevels(task.SubTasks, level + 1);
                }
            }
        }

        /// <summary>
        /// Đếm tổng số tasks bao gồm cả subtasks đệ quy
        /// </summary>
        private int CountAllTasks(IEnumerable<ProjectTaskModel> tasks)
        {
            if (tasks == null) return 0;

            var count = 0;
            foreach (var task in tasks)
            {
                count++; // Đếm task hiện tại
                count += CountAllTasks(task.SubTasks); // Đếm đệ quy subtasks
            }

            return count;
        }

        /// <summary>
        /// Apply filter to flattened task list
        /// </summary>
        private void ApplyTaskFilterToFlattened(List<ProjectTaskModel> flattenedTasks)
        {
            if (flattenedTasks == null) return;

            var filtered = flattenedTasks.AsEnumerable();

            // Filter by search keyword
            if (!string.IsNullOrEmpty(TaskSearchKeyword))
            {
                var keyword = TaskSearchKeyword.ToLower();
                filtered = filtered.Where(t =>
                    (!string.IsNullOrEmpty(t.TaskName) && t.TaskName.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrEmpty(t.Title) && t.Title.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrEmpty(t.TaskCode) && t.TaskCode.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrEmpty(t.Description) && t.Description.ToLower().Contains(keyword)));
            }

            // Filter by status
            if (TaskSelectedStatus != TaskStatuss.All)
            {
                filtered = filtered.Where(t => t.Status == TaskSelectedStatus);
            }

            FilteredTasks.Clear();
            foreach (var task in filtered)
            {
                FilteredTasks.Add(task);
            }

            OnPropertyChanged(nameof(TaskFilterInfo));
        }

        #endregion

        #region Public Helper Methods for Lazy Loading

        /// <summary>
        /// Get subtasks of a task
        /// </summary>
        public async Task<List<ProjectTaskDto>> GetSubTasksAsync(int projectId, int parentTaskId)
        {
            try
            {
                return await _projectApiService.GetSubTasksAsync(projectId, parentTaskId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting subtasks: {ex.Message}");
                return new List<ProjectTaskDto>();
            }
        }

        /// <summary>
        /// lấy subtasks (con) theo page
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="parentTaskId"></param>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<List<ProjectTaskModel>> GetSubTaskModelPageAsync(
            int projectId, 
            int parentTaskId, 
            int pageNumber, 
            int pageSize)
        {
            try
            {

                var result = await _projectApiService.GetSubTasksPagedAsync(
                    projectId: projectId,
                    parentTaskId: parentTaskId,
                    pageNumber: pageNumber,
                    pageSize: pageSize);

                if (result?.Data != null)
                {
                    return result.Data
                        .Select(dto => ProjectTaskModel.MapToProjectTaskModel(dto))
                        .ToList();
                }
                else
                {
                    return new List<ProjectTaskModel>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting subtask page: {ex.Message}");
                return new List<ProjectTaskModel>();
            }
        }


        #endregion
    }
}
