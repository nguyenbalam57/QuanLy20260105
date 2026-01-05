using ManagementFile.App.Models;
using ManagementFile.App.Models.Projects;
using ManagementFile.App.Models.Users;
using ManagementFile.App.Services;
using ManagementFile.App.ViewModels;
using ManagementFile.App.Views.Project;
using ManagementFile.Contracts.Enums;
using ManagementFile.Contracts.Enums.Extensions;
using ManagementFile.Contracts.Requests.ProjectManagement.Projects;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ManagementFile.App.ViewModels.Project
{
    /// <summary>
    /// Enhanced ViewModel for AddEditProjectDialog with comprehensive project management features
    /// Supports Department extensions, TagInputControl, statistics, and export functionality
    /// </summary>
    public class AddEditProjectDialogViewModel : BaseViewModel, IDisposable
    {
        private readonly ProjectApiService _projectApiService;
        private readonly UserManagementService _userManagementService;
        private readonly AdminService _adminService;
        private bool _isInitializing = true;
        private bool _disposed = false;

        #region Fields

        private int? _projectParentId;
        private ProjectModel _originalProject;
        private DialogMode _dialogMode;
        private string _dialogTitle;
        private string _projectName;
        private string _projectCode;
        private string _description;
        private TaskPriority _priority = TaskPriority.Normal;
        private ProjectStatus _projectStatus = ProjectStatus.Planning;
        private Department _department = Department.PM;
        private int? _projectManagerId;
        private UserModel _selectedProjectManager;
        private string _clientId;
        private string _clientName;
        private DateTime _startDate = DateTime.Now;
        private DateTime? _plannedEndDate;
        private DateTime? _actualEndDate;
        private decimal _estimatedHours;
        private decimal _actualHours;
        private decimal _progress;
        private bool _isPublic = true;
        private bool _isActive = true;
        private bool _isSaving;
        private ObservableCollection<string> _tagsList;
        private string _successMessage;
        private string _infoMessage;
        private string _loadingMessage = "Đang xử lý...";
        private bool _hasUnsavedChanges;

        // Repository status fields
        private string _repositoryStatusText = "Chưa kiểm tra";
        private string _repositoryStatusIcon = "❓";
        private string _lastCommitInfo = "";


        #endregion

        #region Constructor

        public AddEditProjectDialogViewModel(
            ProjectApiService projectApiService,
            UserManagementService userManagementService,
            AdminService adminService)
        {
            _projectApiService = projectApiService ?? throw new ArgumentNullException(nameof(projectApiService));
            _userManagementService = userManagementService ?? throw new ArgumentNullException(nameof(userManagementService));
            _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));

            InitializeCommands();
            TagsList = new ObservableCollection<string>();
        }

        #endregion

        #region Properties

        public int? ProjectParentId
        {
            get => _projectParentId;
            set
            { 
                if (SetProperty(ref _projectParentId, value))
                {
                    OnPropertyChanged(nameof(IsViewChildernProjects));
                    UpdateProjectParentTitle();
                }
            }
        }

        public string ProjectParentTitle { get; set; }

        public DialogMode DialogMode
        {
            get => _dialogMode;
            set
            {
                if (SetProperty(ref _dialogMode, value))
                {
                    OnModeChanged();
                }
            }
        }

        public bool IsNewProject => DialogMode == DialogMode.Add;
        public bool IsEditMode => DialogMode == DialogMode.Edit;
        public bool IsViewMode => DialogMode == DialogMode.View;
        public bool IsEditableMode => IsNewProject || IsEditMode;
        public bool IsViewChildernProjects => ProjectParentId.HasValue && ProjectParentId > 0;

        public string DialogTitle
        {
            get => _dialogTitle;
            set => SetProperty(ref _dialogTitle, value);
        }

        public string ProjectName
        {
            get => _projectName;
            set => SetProperty(ref _projectName, value);
        }

        public string ProjectCode
        {
            get => _projectCode;
            set
            { 
                if(SetProperty(ref _projectCode, value))
                {
                    LoadDialogTitle();
                }
            } 
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public TaskPriority Priority
        {
            get => _priority;
            set 
            { 
                if(SetProperty(ref _priority, value))
                {
                }
            }
        }

        public ProjectStatus ProjectStatus
        {
            get => _projectStatus;
            set => SetProperty(ref _projectStatus, value);
        }


        public int? ProjectManagerId
        {
            get => _projectManagerId;
            set => SetProperty(ref _projectManagerId, value);
            
        }

        public UserModel SelectedProjectManager
        {
            get => _selectedProjectManager;
            set 
            { 
                if (SetProperty(ref _selectedProjectManager, value))
                {
                    if(SelectedProjectManager != null)
                        ProjectManagerId = SelectedProjectManager.Id;
                }
            }
        }
        public string ManagerSearchScope => "AllUsers";
        public string ManagerSearchPlaceholder => "Tìm kiếm quản lý dự án...";

        public string ClientId
        {
            get => _clientId;
            set => SetProperty(ref _clientId, value);
        }

        public string ClientName
        {
            get => _clientName;
            set => SetProperty(ref _clientName, value);
        }

        public DateTime StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        public DateTime? PlannedEndDate
        {
            get => _plannedEndDate;
            set => SetProperty(ref _plannedEndDate, value);
        }

        public DateTime? ActualEndDate
        {
            get => _actualEndDate;
            set => SetProperty(ref _actualEndDate, value);
        }

        public decimal EstimatedHours
        {
            get => _estimatedHours;
            set => SetProperty(ref _estimatedHours, value);
        }

        public decimal ActualHours
        {
            get => _actualHours;
            set => SetProperty(ref _actualHours, value);
        }

        public decimal Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        public bool IsPublic
        {
            get => _isPublic;
            set => SetProperty(ref _isPublic, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public bool IsSaving
        {
            get => _isSaving;
            set => SetProperty(ref _isSaving, value);
        }

        // Project duration and progress properties
        private string _projectDurationText;
        public string ProjectDurationText
        {
            get => _projectDurationText;
            private set => SetProperty(ref _projectDurationText, value);
        }

        private string _projectProgressText;
        public string ProjectProgressText
        {
            get => _projectProgressText;
            private set => SetProperty(ref _projectProgressText, value);
        }

        private double _projectProgressPercentage;
        public double ProjectProgressPercentage
        {
            get => _projectProgressPercentage;
            private set => SetProperty(ref _projectProgressPercentage, value);
        }

        /// <summary>
        /// Tags as ObservableCollection for TagInputControl binding
        /// </summary>s
        public ObservableCollection<string> TagsList
        {
            get => _tagsList;
            set => SetProperty(ref _tagsList, value);
        }

        public ObservableCollection<TaskPriorityItem> PriorityItems { get; set; }

        public ObservableCollection<ProjectStatusItem> ProjectStatusItems { get; set; }

        private bool _isLoadingProject;
        public bool IsLoadingProject
        {
            get => _isLoadingProject;
            set => SetProperty(ref _isLoadingProject, value);
        }

        public bool IsAnyLoading => IsLoadingProject || IsSaving ;

        public string SuccessMessage
        {
            get => _successMessage;
            set => SetProperty(ref _successMessage, value);
        }

        public string InfoMessage
        {
            get => _infoMessage;
            set => SetProperty(ref _infoMessage, value);
        }

        public string LoadingMessage
        {
            get => _loadingMessage;
            set => SetProperty(ref _loadingMessage, value);
        }

        public string SaveButtonText => IsNewProject ? "Tạo dự án mới" : "Cập nhật dự án";

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set => SetProperty(ref _hasUnsavedChanges, value);
        }

        public string CloseButtonText => IsViewMode ? "Đóng" : "Hủy bỏ";
        public string CloseButtonIcon => IsViewMode ? "❌" : "❌";

        #endregion

        #region Commands

        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand EditCommand { get; private set; }
        public ICommand ResetCommand { get; private set; }

        #endregion

        #region Events

        public event System.EventHandler<DialogCloseEventArgs> RequestClose;

        #endregion

        #region Initialization Methods

        public async Task InitializeAsync(ProjectModel project = null, DialogMode mode = DialogMode.Add, int? projectParentId = null)
        {
            try
            {
                _isInitializing = true;
                _originalProject = project;
                DialogMode = mode;
                ProjectParentId = projectParentId;

                await InitializePropertiesAsync();

                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] - ViewModel initialized with mode: {mode}");
            }
            finally
            {
                _isInitializing = false;
                HasUnsavedChanges = false;

                // UPDATE COMPUTED PROPERTIES SAU KHI KHỞI TẠO XONG
                UpdateComputedProperties();
            }
        }

        private async Task InitializePropertiesAsync()
        {
            PriorityItems = TaskPriorityHelper.GetTaskPriorityItems();
            ProjectStatusItems = ProjectStatusHelper.GetProjectStatusItemsWithoutAll();

            switch (DialogMode)
            {
                case DialogMode.Add:
                    DialogTitle = "Thêm dự án mới";
                    ProjectManagerId = 0;
                    break;

                case DialogMode.Edit:
                    DialogTitle = "Chỉnh sửa dự án";
                    await LoadProjectDataAsync();
                    break;

                case DialogMode.View:
                    DialogTitle = "Xem chi tiết dự án";
                    await LoadProjectDataAsync();
                    break;
            }
        }

        private void LoadDialogTitle()
        {
            switch (DialogMode)
            {
                case DialogMode.Add:
                    DialogTitle = "Thêm dự án mới";
                    break;
                case DialogMode.Edit:
                    DialogTitle = $"Chỉnh sửa dự án - {ProjectCode}";
                    break;
                case DialogMode.View:
                    DialogTitle = $"Xem chi tiết dự án - {ProjectCode}";
                    break;
            }
        }

        private void InitializeCommands()
        {
            SaveCommand = new RelayCommand(async () => await ExecuteSaveAsync(), CanSave);
            CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, new DialogCloseEventArgs(false, "User cancelled")));
            EditCommand = new RelayCommand(() =>
            {
                DialogMode = DialogMode.Edit;
                DialogTitle = "Chỉnh sửa dự án";

                // UPDATE COMPUTED PROPERTIES SAU KHI KHỞI TẠO XONG
                UpdateComputedProperties();

                OnPropertyChanged(nameof(IsEditableMode));
                OnPropertyChanged(nameof(IsViewMode));
                OnPropertyChanged(nameof(ValidationMessage));
                OnPropertyChanged(nameof(HasValidationMesage));
            }, () => IsViewMode);
            ResetCommand = new RelayCommand(async () => await ResetToOriginalAsync(), () => IsEditMode && _originalProject != null);
        }

        #endregion

        #region Tags Management

        private void UpdateComputedProperties()
        {
            // Update ProjectDurationText
            if (PlannedEndDate == null)
            {
                ProjectDurationText = "Thời gian không xác định";
            }
            else
            {
                var duration = PlannedEndDate.Value - StartDate;
                var daysTotal = (int)duration.TotalDays;

                if (IsViewMode)
                {
                    var daysElapsed = (int)(DateTime.Now - StartDate).TotalDays;
                    var daysRemaining = Math.Max(0, daysTotal - daysElapsed);
                    ProjectDurationText = $"Tổng: {daysTotal} ngày | Còn lại: {daysRemaining} ngày";
                }
                else
                {
                    ProjectDurationText = $"Thời gian dự kiến: {daysTotal} ngày";
                }
            }

            // Update ProjectProgressText và ProjectProgressPercentage
            if (!IsViewMode || PlannedEndDate == null)
            {
                ProjectProgressText = "";
                ProjectProgressPercentage = 0;
            }
            else
            {
                var totalDays = (PlannedEndDate.Value - StartDate).TotalDays;
                var elapsedDays = (DateTime.Now - StartDate).TotalDays;
                var progress = Math.Max(0, Math.Min(100, (elapsedDays / totalDays) * 100));

                ProjectProgressText = $"Tiến độ thời gian: {progress:F0}%";
                ProjectProgressPercentage = progress;
            }



        }

        #endregion

        #region Project Manager Management

        private System.Timers.Timer _searchTimer;

        private void OnModeChanged()
        {
            // Update computed properties khi mode thay đổi
            UpdateComputedProperties();

            // Update các property mode-dependent khác
            OnPropertyChanged(nameof(IsNewProject));
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(IsViewMode));
            OnPropertyChanged(nameof(IsEditableMode));
            OnPropertyChanged(nameof(SaveButtonText));
            OnPropertyChanged(nameof(CloseButtonText));
            OnPropertyChanged(nameof(CloseButtonIcon));
            OnPropertyChanged(nameof(ValidationMessage));
            OnPropertyChanged(nameof(HasValidationMesage));

            OnPropertyChanged(nameof(IsViewChildernProjects));
            UpdateProjectParentTitle();

            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] - Dialog mode changed to: {_dialogMode}");
        }

        #endregion

        #region Data Loading

        private async Task LoadProjectDataAsync()
        {
            if (_originalProject == null) return;

            try
            {
                IsLoadingProject = true;
                OnPropertyChanged(nameof(IsAnyLoading));

                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] Loading project data from API for ID: {_originalProject.Id}");

                var projectFromApi = await _projectApiService.GetProjectByIdAsync(_originalProject.Id);

                if (projectFromApi != null)
                {
                    await LoadDataFromProject(projectFromApi);
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] Project data loaded from API: {ProjectName} (Manager: {ProjectManagerId})");
                }
                else
                {
                    await LoadProjectDataFromOriginal();
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] API returned null, using original project data");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] ❌ Error loading project from API: {ex.Message}");
                await LoadProjectDataFromOriginal();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"⚠️ Không thể tải dữ liệu mới nhất từ server. Sử dụng dữ liệu cache.\n\nChi tiết lỗi: {ex.Message}",
                                   "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
            }
            finally
            {
                IsLoadingProject = false;
                OnPropertyChanged(nameof(IsAnyLoading));
            }
        }

        private async Task LoadDataFromProject(dynamic project)
        {
            ProjectParentId = project.ProjectParentId;
            ProjectName = project.ProjectName;
            ProjectCode = project.ProjectCode;
            Description = project.Description;
            Priority = project.Priority;
            ProjectStatus = project.Status;

            if (project.ProjectManagerId > 0)
            {
                SelectedProjectManager = await _userManagementService.GetUserByIdAsync(project.ProjectManagerId);
            }

            ClientId = project.ClientId?.ToString() ?? "";
            ClientName = project.ClientName;
            StartDate = project.StartDate ?? DateTime.Now;
            PlannedEndDate = project.PlannedEndDate;
            ActualEndDate = project.ActualEndDate;
            EstimatedHours = project.EstimatedHours ?? 0;
            ActualHours = project.ActualHours ?? 0;
            Progress = project.CompletionPercentage ?? 0;
            IsPublic = project.IsPublic ?? true;
            IsActive = project.IsActive ?? true;

            // Load tags
            //LoadProjectTags(project);
            TagsList = new ObservableCollection<string>(project.Tags);

            OnModeChanged();
        }

        private void UpdateProjectParentTitle()
        {
            if(ProjectParentId.HasValue && ProjectParentId >= 0)
            {
                ProjectParentTitle = $"Dự án cha: {ProjectParentId}";
            }
            else
                ProjectParentTitle = "";
        }

        private async Task LoadProjectDataFromOriginal()
        {
            if (_originalProject == null) return;

            await LoadDataFromProject(_originalProject);
        }

        #endregion


        #region Validation

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(ProjectName) &&
                   ValidateForm() &&
                   !IsAnyLoading &&
                   IsEditableMode;
        }

        private bool ValidateForm()
        {

            if (string.IsNullOrWhiteSpace(ProjectName?.Trim()))
                return false;

            if (ProjectManagerId <= 0)
                return false;

            if (PlannedEndDate.HasValue && StartDate >= PlannedEndDate)
                return false;

            if (EstimatedHours < 0)
                return false;

            return true;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public string ValidationMessage
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ProjectName?.Trim()))
                    return "Tên dự án là bắt buộc";

                if (ProjectManagerId <= 0)
                    return "Vui lòng chọn quản lý dự án";

                if (PlannedEndDate.HasValue && StartDate >= PlannedEndDate)
                    return "Ngày bắt đầu phải nhỏ hơn ngày kết thúc dự kiến";

                if (EstimatedHours < 0)
                    return "Số giờ ước tính phải >= 0";

                return "";
            }
        }

        public bool HasValidationMesage => IsEditableMode && (!string.IsNullOrWhiteSpace(ValidationMessage) && !IsViewMode);

        #endregion

        #region Save Operations

        private async Task ExecuteSaveAsync()
        {
            try
            {
                IsSaving = true;
                LoadingMessage = IsNewProject ? "Đang tạo dự án..." : "Đang cập nhật dự án...";

                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] starting save operation - IsNewProject: {IsNewProject}");

                if (IsNewProject)
                {
                    await CreateNewProjectAsync();
                }
                else
                {
                    await UpdateExistingProjectAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] Error in ExecuteSaveAsync: {ex.Message}");
                MessageBox.Show($"❌ Lỗi: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsSaving = false;
                LoadingMessage = "Đang xử lý...";
            }
        }

        private async Task CreateNewProjectAsync()
        {
            int projectManagerId = ProjectManagerId ?? -1;

            var createRequest = new CreateProjectRequest
            {
                ProjectParentId = ProjectParentId,
                ProjectName = ProjectName?.Trim(),
                Description = Description?.Trim(),
                Priority = Priority,
                ProjectStatus = ProjectStatus,
                ClientId = string.IsNullOrWhiteSpace(ClientId) ? (int?)null : int.Parse(ClientId.Trim()),
                ClientName = ClientName?.Trim(),
                PlannedEndDate = PlannedEndDate,
                ActualEndDate = ActualEndDate,
                EstimatedHours = EstimatedHours,
                Progress = Progress,
                IsPublic = IsPublic,
                Tags = TagsList?.Count > 0 ? new List<string>(TagsList) : new List<string>(),
                ProjectManagerId = projectManagerId,
                StartDate = StartDate
            };

            var result = await _projectApiService.CreateProjectAsync(createRequest);
            if (result != null)
            {
                HasUnsavedChanges = false;
                ShowSuccessMessage("Tạo dự án thành công!");

                await Task.Delay(1500);
                RequestClose?.Invoke(this, new DialogCloseEventArgs(true, "Project created successfully", result));
            }
            else
            {
                MessageBox.Show("❌ Lỗi tạo dự án. Vui lòng thử lại.", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task UpdateExistingProjectAsync()
        {
            var updateRequest = new UpdateProjectRequest
            {
                ProjectName = ProjectName?.Trim(),
                Description = Description?.Trim(),
                Priority = Priority,
                ProjectStatus = ProjectStatus,
                ClientId = string.IsNullOrWhiteSpace(ClientId) ? (int?)null : int.Parse(ClientId.Trim()),
                ClientName = ClientName?.Trim(),
                PlannedEndDate = PlannedEndDate,
                ActualEndDate = ActualEndDate,
                EstimatedHours = EstimatedHours,
                Progress = Progress,
                IsPublic = IsPublic,
                Tags = TagsList?.Count > 0 ? new List<string>(TagsList) : new List<string>(),
            };

            var result = await _projectApiService.UpdateProjectAsync(_originalProject.Id, updateRequest);
            if (result != null)
            {
                HasUnsavedChanges = false;
                ShowSuccessMessage("Cập nhật dự án thành công!");

                await Task.Delay(1500);
                RequestClose?.Invoke(this, new DialogCloseEventArgs(true, "Project updated successfully", result));
            }
            else
            {
                MessageBox.Show("❌ Lỗi cập nhật dự án. Vui lòng thử lại.", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Reset Functionality

        private async Task ResetToOriginalAsync()
        {
            try
            {
                if (_originalProject == null) return;

                var result = MessageBox.Show(
                    "🔄 Bạn có chắc chắn muốn khôi phục tất cả thay đổi về trạng thái ban đầu không?\n\nTất cả dữ liệu hiện tại sẽ bị mất.",
                    "Xác nhận khôi phục",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.No);

                if (result == MessageBoxResult.Yes)
                {
                    LoadingMessage = "Đang khôi phục dữ liệu...";

                    // Reload original data
                    LoadProjectDataFromOriginal();

                    // Clear unsaved changes flag
                    HasUnsavedChanges = false;

                    // ✅ UPDATE COMPUTED PROPERTIES SAU KHI KHỞI TẠO XONG
                    UpdateComputedProperties();

                    ShowTemporaryMessage("✅ Đã khôi phục về trạng thái ban đầu", MessageType.Success);

                    System.Diagnostics.Debug.WriteLine($"[2025-10-10 08:51:13] reset project data to original state");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[2025-10-10 08:51:13] Error resetting to original: {ex.Message}");
                MessageBox.Show($"❌ Lỗi khôi phục dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadingMessage = "Đang xử lý...";
            }
        }

        #endregion

        #region Message Management

        public void ShowTemporaryMessage(string message, MessageType type)
        {
            try
            {
                switch (type)
                {
                    case MessageType.Success:
                        SuccessMessage = message;
                        break;
                    case MessageType.Info:
                        InfoMessage = message;
                        break;
                    case MessageType.Warning:
                    case MessageType.Error:
                        // These would be handled through ValidationMessage or direct MessageBox
                        break;
                }

                // Auto-clear after 3 seconds
                var timer = new System.Timers.Timer(3000);
                timer.Elapsed += (s, e) =>
                {
                    timer.Stop();
                    timer.Dispose();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        switch (type)
                        {
                            case MessageType.Success:
                                SuccessMessage = "";
                                break;
                            case MessageType.Info:
                                InfoMessage = "";
                                break;
                        }
                    });
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] Error showing temporary message: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        private void ShowSuccessMessage(string message)
        {
            ShowTemporaryMessage(message, MessageType.Success);
        }

        protected override bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            var result = base.SetProperty(ref storage, value, propertyName);

            if (result && !_isInitializing)
            {
                var dataProperties = new[]
                {
                    nameof(ProjectName), nameof(Description),
                    nameof(Priority), nameof(ProjectManagerId),
                    nameof(ClientId), nameof(ClientName),
                    nameof(StartDate), nameof(PlannedEndDate), nameof(ActualEndDate),
                    nameof(EstimatedHours), nameof(ActualHours), nameof(Progress),
                    nameof(IsPublic), nameof(IsActive),
                    nameof(TagsList),
                    nameof(IsNewProject), nameof(IsEditMode), nameof(IsViewMode),
                };

                if (Array.Exists(dataProperties, prop => prop == propertyName))
                {
                    HasUnsavedChanges = true;
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}]  - Property changed: {propertyName}, HasUnsavedChanges: true");
                }

                var validationProperties = new[]
                {
                    nameof(ProjectName), nameof(ProjectManagerId),
                    nameof(StartDate), nameof(PlannedEndDate), nameof(ActualEndDate),
                    nameof(EstimatedHours), nameof(ActualHours), nameof(Progress) , nameof(SelectedProjectManager),
                };

                if (Array.Exists(validationProperties, prop => prop == propertyName))
                {
                    OnPropertyChanged(nameof(ValidationMessage));
                    OnPropertyChanged(nameof(HasValidationMesage));
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.UtcNow}] - ValidationMessage updated due to {propertyName} change");
                }

                // Update computed properties khi cần thiết
                switch (propertyName)
                {
                    case nameof(StartDate):
                    case nameof(PlannedEndDate):
                    case nameof(ActualEndDate):
                    case nameof(EstimatedHours):
                    case nameof(ActualHours):
                    case nameof(Progress):
                    case nameof(DialogMode):
                        UpdateComputedProperties();
                        break;
                }


                // Update computed properties
                switch (propertyName)
                {
                    case nameof(StartDate):
                    case nameof(PlannedEndDate):
                        OnPropertyChanged(nameof(ProjectDurationText));
                        OnPropertyChanged(nameof(ProjectProgressText));
                        OnPropertyChanged(nameof(ProjectProgressPercentage));
                        break;
                    case nameof(IsNewProject):
                    case nameof(IsEditMode):
                    case nameof(IsViewMode):
                        OnPropertyChanged(nameof(ValidationMessage));
                        OnPropertyChanged(nameof(HasValidationMesage));
                        break;
                }
            }

            return result;
        }

        public void ClearProjectManagerSelection()
        {
            ProjectManagerId = 0;
        }

        #endregion

        #region Disposal

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                try
                {
                    _searchTimer?.Stop();
                    _searchTimer?.Dispose();
                    _searchTimer = null;

                    // Clear collections
                    TagsList?.Clear();
                    PriorityItems?.Clear();

                    System.Diagnostics.Debug.WriteLine($"[2025-10-10 08:51:13] AddEditProjectDialogViewModel disposed successfully");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[2025-10-10 08:51:13] Error during disposal: {ex.Message}");
                }

                _disposed = true;
            }
        }

        ~AddEditProjectDialogViewModel()
        {
            Dispose(false);
        }

        #endregion
    }
}