using ManagementFile.App.Controls.Projects;
using ManagementFile.App.Models;
using ManagementFile.App.Models.Dialogs;
using ManagementFile.App.Models.Projects;
using ManagementFile.App.Models.Projects.PermissionProjects;
using ManagementFile.App.Models.Users;
using ManagementFile.App.Services;
using ManagementFile.App.ViewModels.Controls.Projects;
using ManagementFile.App.ViewModels.Project;
using ManagementFile.App.Views.Dialogs.Comments;
using ManagementFile.App.Views.Project;
using ManagementFile.Contracts.Enums;
using ManagementFile.Contracts.Requests.ProjectManagement.Projects;
using ManagementFile.Contracts.Requests.ProjectManagement.TaskComments;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ManagementFile.App.ViewModels.Controls
{
    /// <summary>
    /// ViewModel cho ProjectsControl với hỗ trợ hierarchy và lazy loading
    /// Enhanced version similar to TaskCommentsControlViewModel
    /// </summary>
    public class ProjectsControlViewModel : BaseViewModel
    {
        #region Private Fields

        private readonly ProjectApiService _projectApiService;
        private readonly UserManagementService _userService;
        private readonly AdminService _adminService;

        // Loading states
        private bool _isLoading;
        private string _loadingMessage;

        // Search and filter
        private string _searchKeyword;
        private ProjectStatus _selectedStatus = ProjectStatus.All;

        // Collections
        private ObservableCollection<ProjectModel> _projects;
        private ObservableCollection<ProjectModel> _filteredProjects;
        private ObservableCollection<ProjectModel> _flattenedProjects;

        // Selected items
        private ProjectModel _selectedProject;

        // Pagination
        private int _pageSize = 20;
        private int _currentPage = 1;
        private int _totalPages = 1;

        private int _initiaPage = 1;
        private bool _canLoadMore = true;

        // Hierarchy
        private bool _isTreeViewMode = false;

        #endregion

        #region Constructor

        public ProjectsControlViewModel(
            ProjectApiService projectApiService,
            UserManagementService userService,
            AdminService adminService)
        {
            _projectApiService = projectApiService ?? throw new ArgumentNullException(nameof(projectApiService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));

            _projects = new ObservableCollection<ProjectModel>();
            _filteredProjects = new ObservableCollection<ProjectModel>();
            _flattenedProjects = new ObservableCollection<ProjectModel>();

            InitializeCommands();

            // Load initial data
            Task.Run(async () => await LoadProjectsAsync());
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
        /// Search keyword cho projects
        /// </summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    RefreshFlattenedProjects();
                }
            }
        }

        /// <summary>
        /// Selected status filter
        /// </summary>
        public ProjectStatus SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                if (SetProperty(ref _selectedStatus, value))
                {
                    RefreshFlattenedProjects();
                }
            }
        }

        /// <summary>
        /// All projects (flat list from server)
        /// </summary>
        public ObservableCollection<ProjectModel> Projects
        {
            get => _projects;
            set => SetProperty(ref _projects, value);
        }

        /// <summary>
        /// Filtered projects for display
        /// </summary>
        public ObservableCollection<ProjectModel> FilteredProjects
        {
            get => _filteredProjects;
            set => SetProperty(ref _filteredProjects, value);
        }

        /// <summary>
        /// Flattened projects với hierarchy structure cho DataGrid
        /// </summary>
        public ObservableCollection<ProjectModel> FlattenedProjects
        {
            get => _flattenedProjects;
            set => SetProperty(ref _flattenedProjects, value);
        }

        /// <summary>
        /// Selected project
        /// </summary>
        public ProjectModel SelectedProject
        {
            get => _selectedProject;
            set
            {
                if(_selectedProject != null)
                {
                    _selectedProject.IsSelected = false;
                }

                if (SetProperty(ref _selectedProject, value))
                {
                    if(_selectedProject != null)
                    {
                        _selectedProject.IsSelected = true;
                    }
                    OnPropertyChanged(nameof(HasSelectedProject));
                    OnPropertyChanged(nameof(SelectedProjectInfo));
                }
            }
        }

        /// <summary>
        /// Project status items for filter ComboBox
        /// </summary>
        public ObservableCollection<ProjectStatusItem> ProjectStatusItems { get; set; }

        /// <summary>
        /// Page size for pagination
        /// </summary>
        public int PageSize
        {
            get => _pageSize;
            set => SetProperty(ref _pageSize, value);
        }

        /// <summary>
        /// Current page number
        /// </summary>
        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        /// <summary>
        /// Total pages available
        /// </summary>
        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        /// <summary>
        /// Is tree view mode (hierarchical display)
        /// </summary>
        public bool IsTreeViewMode
        {
            get => _isTreeViewMode;
            set => SetProperty(ref _isTreeViewMode, value);
        }
        /// <summary>
        /// trang thay load
        /// </summary>
        public bool CanLoadMore
        {
            get => _canLoadMore;
            set => SetProperty(ref _canLoadMore, value);
        }

        #endregion

        #region Computed Properties

        public string SelectedProjectInfo
        {
            get
            {
                if (SelectedProject == null)
                    return "Chưa chọn project nào";
                return $"{SelectedProject.ProjectName} ({SelectedProject.ProjectCode})";
            }
        }

        public string ProjectFilterInfo
        {
            get
            {
                var totalCount = Projects?.Count ?? 0;
                var filteredCount = FlattenedProjects?.Count ?? 0;

                if (string.IsNullOrEmpty(SearchKeyword))
                    return $"Hiển thị {filteredCount} / {totalCount} projects";

                return $"Tìm thấy {filteredCount} / {totalCount} projects";
            }
        }

        public string ProjectSummaryText
        {
            get
            {
                var totalProjects = Projects?.Count ?? 0;
                if (totalProjects == 0)
                    return "Chưa có project nào";

                var activeProjects = Projects?.Count(p => p.IsActive) ?? 0;
                var rootProjects = Projects?.Count(p => !p.ProjectParentId.HasValue) ?? 0;
                return $"Tổng {totalProjects} projects ({rootProjects} gốc), {activeProjects} đang hoạt động";
            }
        }

        public bool HasSelectedProject => SelectedProject != null;
        public bool HasProjects => Projects?.Count > 0;

        #endregion

        #region Commands

        public ICommand SearchCommand { get; private set; }
        public ICommand FilterStatusCommand { get; private set; }
        public ICommand RefreshProjectsCommand { get; private set; }
        public ICommand AddProjectCommand { get; private set; }
        public ICommand AddChilderProjectCommand { get; private set; }
        public ICommand EditProjectCommand { get; private set; }
        public ICommand DeleteProjectCommand { get; private set; }
        public ICommand ViewProjectDetailsCommand { get; private set; }
        public ICommand ViewProjectTasksCommand { get; private set; }
        public ICommand ViewProjectMemebersCommand { get; private set; }
        public ICommand ReloadProjectCommand { get; private set; }
        public ICommand ExpandChildrenCommand { get; private set; }

        public ICommand ProgresProjectCommand { get; private set; }
        public ICommand ChangeProjectStatusCommand { get; private set;}

        private void InitializeCommands()
        {
            // Search and filter commands
            SearchCommand = new RelayCommand<string>(ExecuteSearch);
            FilterStatusCommand = new RelayCommand<ProjectStatus>(ExecuteFilterStatus);
            RefreshProjectsCommand = new AsyncRelayCommand(ExecuteRefreshProjectsAsync);

            // Project CRUD commands
            AddProjectCommand = new AsyncRelayCommand(ExecuteAddProjectAsync);
            AddChilderProjectCommand = new AsyncRelayCommand(ExecuteAddChildernProjectAsync, () => HasSelectedProject);
            EditProjectCommand = new AsyncRelayCommand(ExecuteEditProjectAsync, () => HasSelectedProject);
            DeleteProjectCommand = new AsyncRelayCommand(ExecuteDeleteProjectAsync, () => HasSelectedProject);
            ViewProjectDetailsCommand = new AsyncRelayCommand(ExecuteViewProjectDetails, () => HasSelectedProject);

            // View commands
            ViewProjectTasksCommand = new AsyncRelayCommand(ExecuteViewProjectTasksCommand, () => HasSelectedProject);
            ViewProjectMemebersCommand = new AsyncRelayCommand(ExecuteViewProjectMembersCommand, () => HasSelectedProject);

            // Utility commands
            ReloadProjectCommand = new AsyncRelayCommand(ReloadProjectCommandExecute);
            ExpandChildrenCommand = new AsyncRelayCommand<ProjectModel>(ToggleProjectExpansion);

            // cap nhat tien do du an
            ProgresProjectCommand = new AsyncRelayCommand(ExecuteProgresProjectAsync, () => HasSelectedProject);
            ChangeProjectStatusCommand = new AsyncRelayCommand<object>(
                ChangeProjectStatusAsync,
                CanChangeProjectStatus
            );

            // Initialize status items
            ProjectStatusItems = ProjectStatusHelper.GetProjectStatusItems();



            System.Diagnostics.Debug.WriteLine("ProjectsControlViewModel commands initialized");
        }

        #endregion

        #region Data Loading Methods

        private async Task ExecuteRefreshProjectsAsync()
        {
            //CurrentPage = _initiaPage;

            await LoadProjectsAsync();
        }

        /// <summary>
        /// Load projects từ API với pagination
        /// Support cumulative loading - load all pages from 1 to current page
        /// </summary>
        public async Task LoadProjectsAsync()
        {
            try
            {
                if (!_userService.IsLoggedIn) return;

                IsLoading = true;
                LoadingMessage = "Đang tải danh sách projects...";

                // ✅ NEW: Load all pages from 1 to CurrentPage cumulatively
                var allProjects = new List<ProjectModel>();

                System.Diagnostics.Debug.WriteLine($"📚 [ViewModel] Loading cumulative pages 1-{CurrentPage}...");

                int oldPage = CurrentPage;

                for (int page = 1; page <= oldPage; page++)
                {
                    if (oldPage > TotalPages && page > TotalPages)
                    {
                        break;
                    }
                    LoadingMessage = $"Đang tải trang {page}/{CurrentPage}...";

                    var pageProjects = await LoadMoreProjectsAsync(page, PageSize);

                    if (pageProjects != null && pageProjects.Count > 0)
                    {
                        allProjects.AddRange(pageProjects);
                        System.Diagnostics.Debug.WriteLine($"📄 [ProjectViewModel] Loaded page {page}: {pageProjects.Count} projects");
                    }
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Projects.Clear();
                    foreach (var projectModel in allProjects)
                    {
                        Projects.Add(projectModel);
                    }

                    // Apply filter and build hierarchy
                    RefreshFlattenedProjects();

                    OnPropertyChanged(nameof(ProjectSummaryText));
                    OnPropertyChanged(nameof(HasProjects));
                });

                if (CurrentPage < TotalPages)
                {
                    CanLoadMore = true;
                }

                System.Diagnostics.Debug.WriteLine($"✅ [ViewModel] Cumulative load complete: {Projects.Count} total projects from {CurrentPage} pages");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading projects: {ex.Message}");
                MessageBox.Show($"Lỗi tải danh sách projects: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Load more projects for lazy loading (pagination)
        /// Enhanced with TotalPages tracking
        /// </summary>
        public async Task<List<ProjectModel>> LoadMoreProjectsAsync(int page, int pageSize)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"📥 [ViewModel] Loading projects: Page {page}, PageSize {pageSize}");

                var filter = new ProjectFilterRequest
                {
                    PageNumber = page,
                    PageSize = pageSize,
                    SearchTerm = SearchKeyword,
                    Status = SelectedStatus == ProjectStatus.All ? (ProjectStatus?)null : SelectedStatus,
                    SortDirection = "asc",
                };

                var result = await _projectApiService.GetProjectsAsync(filter);

                if (result?.Data == null || result.Data.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("📭 [ViewModel] No more projects to load");
                    return new List<ProjectModel>();
                }

                // Track TotalPages from API response
                if (result.TotalPages > 0)
                {
                    TotalPages = result.TotalPages;
                    System.Diagnostics.Debug.WriteLine($"📊 [ViewModel] Updated TotalPages: {TotalPages}");
                }

                // Track CurrentPage
                if (result.PageNumber > 0)
                {
                    CurrentPage = result.PageNumber;
                    System.Diagnostics.Debug.WriteLine($"📄 [ViewModel] Current Page: {CurrentPage}/{TotalPages}");
                }

                var projectModels = new List<ProjectModel>();
                foreach (var projectDto in result.Data)
                {
                    projectModels.Add(projectDto.ToProjectModel());
                }

                System.Diagnostics.Debug.WriteLine($"✅ [ProjectViewModel] Successfully loaded {projectModels.Count} projects");
                System.Diagnostics.Debug.WriteLine($"📈 [ProjectViewModel] API returned: TotalCount={result.TotalCount}, Page {result.PageNumber}/{result.TotalPages}");

                return projectModels;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ [ProjectViewModel] Error loading more projects: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Load children cho một project cụ thể
        /// Enhanced with GetProjectChildrenAsync API
        /// </summary>
        public async Task<List<ProjectModel>> LoadChildrenForProjectAsync(int parentProjectId, int page, int pageSize)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Loading children for project {parentProjectId} (Page {page}, Size {pageSize})");

                // Sử dụng dedicated API endpoint cho children
                var result = await _projectApiService.GetProjectChildrenAsync(parentProjectId, page, pageSize);

                if (result?.Data == null || result.Data.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"No children found for project {parentProjectId} on page {page}");
                    return new List<ProjectModel>();
                }

                var childModels = new List<ProjectModel>();
                foreach (var projectDto in result.Data)
                {
                    var childModel = projectDto.ToProjectModel();
                    childModels.Add(childModel);
                }

                System.Diagnostics.Debug.WriteLine($"Loaded {childModels.Count} children for project {parentProjectId}");
                return childModels;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading children for project {parentProjectId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// ✅ NEW: Load full hierarchy tree cho một project
        /// </summary>
        public async Task<ProjectModel> LoadProjectHierarchyAsync(int projectId, int maxDepth = 3)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Loading full hierarchy for project {projectId} (MaxDepth: {maxDepth})");

                var projectDto = await _projectApiService.GetProjectHierarchyAsync(projectId, maxDepth);

                if (projectDto == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Project {projectId} not found");
                    return null;
                }

                var projectModel = projectDto.ToProjectModel();

                System.Diagnostics.Debug.WriteLine($"Loaded hierarchy for project {projectId} with {projectModel.Children?.Count ?? 0} direct children");
                return projectModel;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading hierarchy for project {projectId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Move project to new parent
        /// </summary>
        public async Task<bool> MoveProjectToParentAsync(int projectId, int? newParentId, string reason = "")
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Moving project {projectId} to parent {newParentId}");

                IsLoading = true;
                LoadingMessage = "Đang di chuyển dự án...";

                var result = await _projectApiService.MoveProjectAsync(projectId, newParentId, reason);

                if (result != null)
                {
                    MessageBox.Show("Di chuyển dự án thành công!", "Thành công",
                                  MessageBoxButton.OK, MessageBoxImage.Information);

                    // Reload projects
                    await LoadProjectsAsync();

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error moving project {projectId}: {ex.Message}");
                MessageBox.Show($"Lỗi di chuyển dự án: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Filter and Hierarchy Methods

        /// <summary>
        /// Apply filter cho projects
        /// </summary>
        public void ApplyProjectFilter()
        {
            if (Projects == null) return;

            try
            {
                var filtered = Projects.AsEnumerable();

                // Filter by search keyword
                if (!string.IsNullOrEmpty(SearchKeyword))
                {
                    var keyword = SearchKeyword.ToLower();
                    filtered = filtered.Where(p =>
                        p.ProjectName?.ToLower().Contains(keyword) == true ||
                        p.ProjectCode?.ToLower().Contains(keyword) == true ||
                        p.Description?.ToLower().Contains(keyword) == true);
                }

                // Filter by status
                if (SelectedStatus != ProjectStatus.All)
                {
                    filtered = filtered.Where(p => p.Status == SelectedStatus);
                }

                FilteredProjects.Clear();
                foreach (var project in filtered)
                {
                    FilteredProjects.Add(project);
                }

                OnPropertyChanged(nameof(ProjectFilterInfo));

                System.Diagnostics.Debug.WriteLine($"Filtered projects: {FilteredProjects.Count} / {Projects.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying project filter: {ex.Message}");
            }
        }

        /// <summary>
        /// Refresh flattened projects for hierarchy display
        /// </summary>
        public void RefreshFlattenedProjects()
        {
            try
            {
                ApplyProjectFilter();
                var flattened = FlattenProjectHiearchyWithLoadMore(FilteredProjects, respectExpandState: true);
                SetHierarchyLevels(FilteredProjects);

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    FlattenedProjects.Clear();
                    foreach (var project in flattened)
                    {
                        FlattenedProjects.Add(project);
                    }

                });

                OnPropertyChanged(nameof(ProjectFilterInfo));
                OnPropertyChanged(nameof(ProjectSummaryText));


                System.Diagnostics.Debug.WriteLine($"Refreshed flattened projects: {FlattenedProjects.Count} visible items from {CountAllProjects(FilteredProjects)} total");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing flattened projects: {ex.Message}");
            }
        }

        /// <summary>
        /// Flatten hierarchy thành danh sách phẳng để hiển thị
        /// </summary>
        private List<ProjectModel> FlattenHierarchy(IEnumerable<ProjectModel> projects, bool respectExpandState = true)
        {
            var result = new List<ProjectModel>();

            if (projects == null) return result;

            foreach (var project in projects.OrderBy(p => p.CreatedAt))
            {
                result.Add(project);

                // Chỉ thêm children nếu project được expand (hoặc không respect expand state)
                if ((!respectExpandState || project.IsExpanded) && project.HasChildren)
                {
                    var flattenedChildren = FlattenHierarchy(project.Children, respectExpandState);
                    result.AddRange(flattenedChildren);
                }
            }

            return result;
        }

        /// <summary>
        /// Set hierarchy level cho tất cả projects
        /// </summary>
        private void SetHierarchyLevels(IEnumerable<ProjectModel> projects, int level = 0)
        {
            if (projects == null) return;

            foreach (var project in projects)
            {
                project.HierarchyLevel = level;
                if (project.HasChildren)
                {
                    SetHierarchyLevels(project.Children, level + 1);
                }
            }
        }

        /// <summary>
        /// Đếm tổng số project bao gồm cả children đệ quy
        /// </summary>
        private int CountAllProjects(IEnumerable<ProjectModel> projects)
        {
            if (projects == null) return 0;

            var count = 0;
            foreach (var project in projects)
            {
                count++; // Đếm project hiện tại
                count += CountAllProjects(project.Children); // Đếm đệ quy children
            }

            return count;
        }

        /// <summary>
        /// Toggle expand/collapse project với animation effect
        /// </summary>
        private async Task ToggleProjectExpansion(ProjectModel project)
        {
            if (project == null) return;

            try
            {
                project.IsExpanded = !project.IsExpanded;

                // Load children nếu chưa load và đang expand
                if (project.IsExpanded && project.Children.Count == 0 && project.HasChildren)
                {
                    project.IsLoadingChildren = true;

                    try
                    {
                        var children = await LoadChildrenForProjectAsync(project.Id, 1, PageSize);

                        Application.Current?.Dispatcher.Invoke(() =>
                        {
                            project.Children.Clear();
                            foreach (var child in children)
                            {
                                child.HierarchyLevel = project.HierarchyLevel + 1;
                                project.Children.Add(child);
                            }
                        });
                    }
                    finally
                    {
                        project.IsLoadingChildren = false;
                    }
                }

                RefreshFlattenedProjects();

                System.Diagnostics.Debug.WriteLine($"Toggled expansion for project {project.Id}: IsExpanded = {project.IsExpanded}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error toggling expansion: {ex.Message}");
            }
        }

        private List<ProjectModel> FlattenProjectHiearchyWithLoadMore(
            IEnumerable<ProjectModel> projects,
            bool respectExpandState = true)
        {
            var result = new List<ProjectModel>();
            if (projects == null) return result;
            foreach (var project in projects.OrderBy(p => p.CreatedAt))
            {
                result.Add(project);
                // Chỉ thêm children nếu project được expand (hoặc không respect expand state)
                if ((!respectExpandState || project.IsExpanded) &&
                    project.Children != null &&
                    project.Children.Count > 0)
                {
                    // Thêm nút Load More nếu có nhiều hơn PageSize children
                    var flattenedChildren = FlattenProjectHiearchyWithLoadMore(project.Children, respectExpandState);
                    result.AddRange(flattenedChildren);
                    // Thêm nút Load More
                    if(project.ShowLoadMoreSubTasksButton)
                    {
                        result.Add(CreateLoadMorePlacehoder(project));
                    }
                }
            }
            return result;
        }

        private ProjectModel CreateLoadMorePlacehoder(ProjectModel parentProject)
        {
            return new ProjectModel
            {
                Id = -parentProject.Id, // ID đặc biệt cho nút Load More
                ProjectCode = $"__LOADMORE_{parentProject.Id}__",
                ProjectName = parentProject.LoadMoreSubTasksButtonText,

                Description = parentProject.LoadMoreSubTasksTooltip,

                ProjectParentId = parentProject.Id,
                HierarchyLevel = parentProject.HierarchyLevel + 1,

                Children = new ObservableCollection<ProjectModel>(parentProject.Children),
                TotalChildCount = parentProject.TotalChildCount,
                IsActive = false,
            };
        }

        public bool IsLoadMorePlaceholder(ProjectModel project)
        {
            return project != null &&
                   project.Id < 0 &&
                   project.ProjectCode.StartsWith("__LOADMORE_") == true;
        }

        public int GetParentProjectIdFromLoadMorePlaceholder(ProjectModel project)
        {
            if (!IsLoadMorePlaceholder(project)) return 0;
            var idPart = project.ProjectCode.Replace("__LOADMORE_", "").Replace("__", "");
            if (int.TryParse(idPart, out int parentId))
            {
                return parentId;
            }
            return 0;
        }

        #endregion

        #region Command Execution Methods

        private void ExecuteSearch(string keyword)
        {
            SearchKeyword = keyword;
        }

        private void ExecuteFilterStatus(ProjectStatus status)
        {
            SelectedStatus = status;
        }

        private async Task ExecuteAddProjectAsync()
        {
            try
            {
                var dialog = AddEditProjectDialog.CreateForAdd();
                var result = dialog.ShowDialog();

                if (result == true)
                {
                    // Reload projects after adding
                    await LoadProjectsAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi mở dialog tạo project: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteAddChildernProjectAsync()
        {
            if (SelectedProject == null) return;

            try
            {
                var dialog = AddEditProjectDialog.CreateForChildern(SelectedProject.Id);
                var result = dialog.ShowDialog();

                if (result == true)
                {
                    // Reload projects after adding
                    await LoadProjectsAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi mở dialog tạo project con: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteEditProjectAsync()
        {
            if (SelectedProject == null) return;

            try
            {
                var dialog = AddEditProjectDialog.CreateForEdit(SelectedProject);
                var result = dialog.ShowDialog();

                if (result == true)
                {
                    // Reload projects after editing
                    await LoadProjectsAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi mở dialog sửa project: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteDeleteProjectAsync()
        {
            if (SelectedProject == null) return;

            if(PermissionProject.HasPermissionManagerProject() == false)
            {
                MessageBox.Show($"Bạn không có quyền xóa dự án này!", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var confirmMessage = $"Bạn có chắc chắn muốn xóa dự án '{SelectedProject.ProjectName}'?\n\n";

                if (SelectedProject.HasChildren)
                {
                    confirmMessage += $"⚠️ CHÚ Ý: Dự án này có {SelectedProject.ChildCount} dự án con!\n";
                    confirmMessage += "Xóa dự án cha sẽ ảnh hưởng đến tất cả dự án con.\n\n";
                }

                confirmMessage += "⚠️ Hành động này không thể hoàn tác!";

                var result = MessageBox.Show(confirmMessage, "Xác nhận xóa dự án",
                                           MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes) return;

                IsLoading = true;
                LoadingMessage = "Đang xóa project...";

                var success = await _projectApiService.DeleteProjectAsync(SelectedProject.Id);

                if (success)
                {
                    MessageBox.Show("Xóa project thành công!", "Thành công",
                                  MessageBoxButton.OK, MessageBoxImage.Information);

                    // Reload projects
                    await LoadProjectsAsync();
                }
                else
                {
                    MessageBox.Show("Xóa project thất bại!", "Lỗi",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xóa project: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ExecuteViewProjectDetails()
        {
            if (SelectedProject == null) return;

            try
            {
                var dialog = AddEditProjectDialog.CreateForView(SelectedProject);
                var result = dialog.ShowDialog();

                if (result == true)
                {
                    // Reload projects after editing
                    await LoadProjectsAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi mở dialog xem project: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteViewProjectTasksCommand()
        {
            if (SelectedProject == null) return;

            try
            {
                App.GetRequiredService<ProjectManagentsDragablzViewViewModel>().AddTabProjectTask(SelectedProject);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xem dự án task: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteViewProjectMembersCommand()
        {
            if (SelectedProject == null) return;

            try
            {
                App.GetRequiredService<ProjectManagentsDragablzViewViewModel>().AddTabProjectMembers(SelectedProject);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xem dự án thành viên: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ReloadProjectCommandExecute()
        {
            await LoadProjectsAsync();
        }

        private async Task ExecuteProgresProjectAsync()
        {
            if(SelectedProject == null) return;

            try
            {
                IsLoading = true;
                LoadingMessage = "Đang tiến hành dự án...";


                var comment = new Dictionary<string, CommentLineFieldConfig>
                {
                    { "CompletionPercentage" , new CommentLineFieldConfig                         
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
                    { "CompletionPercentage", SelectedProject.CompletionPercentage.ToString() },
                    { "ActualHours", SelectedProject.ActualHours.ToString() },
                };

                var progressInput = CommentLine.Show(title: title,
                                                      message: message,
                                                      fields: comment,
                                                      defaultValues: commentDefauls);

                if(progressInput != null)
                {
                    var progressRequest = new UpdateProjectProgressRequest
                    {
                        CompletionPercentage = decimal.Parse(progressInput["CompletionPercentage"]),
                        ActualHours = int.Parse(progressInput["ActualHours"]),
                    };

                    var result = await _projectApiService.UpdateProgressAsync(SelectedProject.Id, progressRequest);
                    if (result != null)
                    {
                        MessageBox.Show("Cập nhật tiến độ dự án thành công!", "Thành công",
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                        // Reload projects
                        await LoadProjectsAsync();
                    }
                }

                    
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error progressing project {SelectedProject.Id}: {ex.Message}");
                MessageBox.Show($"Lỗi cập nhật tiến độ dự án: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Thay đổi status của project
        /// </summary>
        private async Task ChangeProjectStatusAsync(object paramater)
        {
            if (SelectedProject == null)
                return;

            try
            {
                var currentStatus = SelectedProject.Status;

                if(paramater == null)
                    return;

                var parameterType = paramater.GetType();
                var projectPro = parameterType.GetProperty("Project");
                var statusPro = parameterType.GetProperty("Status");

                if(projectPro == null || statusPro == null)
                    return;

                var project = projectPro.GetValue(paramater) as ProjectModel;
                var newStatus = (ProjectStatus)statusPro.GetValue(paramater);

                if (project == null)
                    return;

                // Validate transition
                //var validation = ProjectStatusHelper.ValidateTransition(
                //    currentStatus,
                //    newStatus,
                //    SelectedProject.StartDate,
                //    SelectedProject.CompletionPercentage
                //);

                //if (!validation.IsValid)
                //{
                //    MessageBox.Show(
                //        $"Không thể thay đổi trạng thái dự án:\n{validation.ErrorMessage}",
                //        "Lỗi",
                //        MessageBoxButton.OK,
                //        MessageBoxImage.Error
                //    );
                //    return;
                //}

                // Confirm với user
                var currentStatusDesc = ProjectStatusHelper.GetDescription(currentStatus);
                var newStatusDesc = ProjectStatusHelper.GetDescription(newStatus);

                var confirmMessage = $"Bạn có chắc muốn thay đổi trạng thái?\n\n" +
                    $"Từ: {currentStatusDesc}\n" +
                    $"Sang: {newStatusDesc}";

                var result = MessageBox.Show(
                    confirmMessage,
                    "Xác nhận thay đổi trạng thái",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result != MessageBoxResult.Yes)
                    return;

                // Show loading
                IsLoading = true;
                LoadingMessage = "Đang cập nhật trạng thái...";

                var statusRequest = new UpdateProjectStatusRequest
                {
                    Status = newStatus,
                };

                // Call API
                var updated = await _projectApiService.UpdateProjectStatusAsync(
                    SelectedProject.Id,
                    statusRequest
                );

                if (updated != null)
                {
                    SelectedProject.Status = newStatus;

                    // Refresh project list
                    await LoadProjectsAsync();
                }
                else
                {
                    var errorMessage = $"Thay đổi status thất bại.";
                    MessageBox.Show(errorMessage, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Lỗi khi thay đổi status: {ex.Message}";
                MessageBox.Show(errorMessage, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Kiểm tra có thể chuyển sang status mới không
        /// </summary>
        private bool CanChangeProjectStatus(object paramater)
        {
            if (SelectedProject == null)
                return false;

            if (paramater == null)
                return false;

            var parameterType = paramater.GetType();
            var projectPro = parameterType.GetProperty("Project");
            var statusPro = parameterType.GetProperty("Status");

            if (projectPro == null || statusPro == null)
                return false;

            var project = projectPro.GetValue(paramater) as ProjectModel;
            var newStatus = (ProjectStatus)statusPro.GetValue(paramater);

            if (project == null)
                return false;

            // Không thể chuyển sang chính nó
            if (SelectedProject.Status == newStatus)
                return false;

            // Kiểm tra quyền
            var currentUser = App.GetCurrentUserModel();
            if (currentUser == null)
                return false;

            // Chỉ Admin, Project Manager, hoặc Project Creator có quyền đổi status
            if (!currentUser.IsAdmin &&
                !currentUser.IsProjectManager &&
                SelectedProject.CreatedBy != currentUser.Id)
                return false;

            // Kiểm tra transition hợp lệ
            return true;
        }

        #endregion

        #region Public Helper Methods

        /// <summary>
        /// Get current user information
        /// </summary>
        public UserModel GetCurrentUser()
        {
            var userDto = _userService?.GetCurrentUser();
            return UserModel.FromDto(userDto);
        }





        #endregion
    }
}
