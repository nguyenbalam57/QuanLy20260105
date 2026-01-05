using ManagementFile.App.Models;
using ManagementFile.App.Models.Projects;
using ManagementFile.App.Models.Users;
using ManagementFile.App.Services;
using ManagementFile.App.ViewModels;
using ManagementFile.Contracts.DTOs.ProjectManagement;
using ManagementFile.Contracts.Enums;
using ManagementFile.Contracts.Requests.ProjectManagement.ProjectMembers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ManagementFile.App.ViewModels.Project
{
    /// <summary>
    /// ViewModel cho AddEditProjectMemberDialog
    /// Hỗ trợ thêm mới và chỉnh sửa thông tin project member với SearchableUserComboBox
    /// </summary>
    public class AddEditProjectMemberViewModel : BaseViewModel
    {
        #region Private Fields

        private readonly ProjectApiService _projectApiService;
        private readonly UserManagementService _userService;
        private readonly ProjectMemberModel _originalMember;
        private readonly int _projectId;
        private DialogMode _dialogMode;

        // Form fields
        private string _dialogTitle = "Thêm thành viên";
        private UserModel _selectedUser;
        private int _selectedUserId = 0;
        private UserRole _projectRole = UserRole.Staff;
        private decimal _allocationPercentage = 100;
        private decimal? _hourlyRate;
        private string _notes = "";
        private DateTime _joinedAt = DateTime.Now;
        private DateTime? _leftAt;
        private byte[] _rowVersion;

        // UI State
        private bool _isLoading = false;
        private bool _isSaving = false;
        private string _validationMessage = "";
        private bool _hasValidationErrors = false;

        // Collections for dynamic loading
        private ObservableCollection<UserRoleItem> _availableRoles;

        // Search - DEPRECATED với dynamic loading
        private int? _projectManagerId;

        #endregion

        #region Constructor

        public AddEditProjectMemberViewModel(
            ProjectApiService projectApiService,
            UserManagementService userService,
            int projectId,
            ProjectMemberModel member = null)
        {
            _projectApiService = projectApiService ?? throw new ArgumentNullException(nameof(projectApiService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _projectId = projectId;
            _originalMember = member;
            _dialogMode = member == null ? DialogMode.Add : DialogMode.Edit;

            InitializeCollections();
            InitializeCommands();
            _ = InitializeData();
        }

        #endregion

        #region Public Properties for Dialog Access

        /// <summary>
        /// Expose UserService cho dialog sử dụng
        /// </summary>
        public UserManagementService UserService => _userService;

        /// <summary>
        /// Expose ProjectApiService cho dialog sử dụng
        /// </summary>
        public ProjectApiService ProjectApiService => _projectApiService;

        /// <summary>
        /// Project ID hiện tại
        /// </summary>
        public int CurrentProjectId => _projectId;

        #endregion

        #region Properties

        public string DialogTitle
        {
            get => _dialogTitle;
            set => SetProperty(ref _dialogTitle, value);
        }

        public DialogMode DialogMode
        {
            get => _dialogMode;
            set
            {
                if (SetProperty(ref _dialogMode, value))
                {
                    UpdateDialogTitle();
                }
            }
        }

        public bool IsAddMode => DialogMode == DialogMode.Add;
        public bool IsEditMode => DialogMode == DialogMode.Edit;
        public bool IsViewMode => DialogMode == DialogMode.View;
        public bool IsReadOnly => IsViewMode || IsEditMode;

        public UserModel SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (SetProperty(ref _selectedUser, value))
                {
                    if (value != null)
                    {
                        SelectedUserId = value.Id;
                        UpdateRoleMember(value.Role);
                    }
                    else
                    {
                        SelectedUserId = 0;
                    }
                    OnPropertyChanged(nameof(CanSave));
                }
            }
        }

        [Required(ErrorMessage = "Vui lòng chọn thành viên dự án")]
        public int SelectedUserId
        {
            get => _selectedUserId;
            set
            {
                if (SetProperty(ref _selectedUserId, value))
                { }
            }
        }

        [Required(ErrorMessage = "Vai trò trong dự án là bắt buộc")]
        public UserRole ProjectRole
        {
            get => _projectRole;
            set
            {
                if (SetProperty(ref _projectRole, value))
                {
                    System.Diagnostics.Debug.WriteLine($"ProjectRole changed to: {value}");
                    ValidateProperty(value);
                    OnPropertyChanged(nameof(CanSave));
                }
            }
        }

        [Range(0, 100, ErrorMessage = "Phần trăm phân bổ phải từ 0 đến 100")]
        public decimal AllocationPercentage
        {
            get => _allocationPercentage;
            set
            {
                if (SetProperty(ref _allocationPercentage, value))
                {
                    ValidateProperty(value);
                }
            }
        }

        [Range(0, double.MaxValue, ErrorMessage = "Mức lương theo giờ phải lớn hơn hoặc bằng 0")]
        public decimal? HourlyRate
        {
            get => _hourlyRate;
            set
            {
                if (SetProperty(ref _hourlyRate, value))
                {
                    ValidateProperty(value);
                }
            }
        }

        [StringLength(1000, ErrorMessage = "Ghi chú không được quá 1000 ký tự")]
        public string Notes
        {
            get => _notes;
            set
            {
                if (SetProperty(ref _notes, value))
                {
                    ValidateProperty(value);
                }
            }
        }

        public DateTime JoinedAt
        {
            get => _joinedAt;
            set => SetProperty(ref _joinedAt, value);
        }

        public DateTime? LeftAt
        {
            get => _leftAt;
            set => SetProperty(ref _leftAt, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    OnPropertyChanged(nameof(CanSave));
                    OnPropertyChanged(nameof(CanCancel));
                }
            }
        }

        public bool IsSaving
        {
            get => _isSaving;
            set
            {
                if (SetProperty(ref _isSaving, value))
                {
                    OnPropertyChanged(nameof(CanSave));
                    OnPropertyChanged(nameof(CanCancel));
                }
            }
        }

        public string ValidationMessage
        {
            get => _validationMessage;
            set => SetProperty(ref _validationMessage, value);
        }

        public bool HasValidationErrors
        {
            get => _hasValidationErrors;
            set => SetProperty(ref _hasValidationErrors, value);
        }

        public ObservableCollection<UserRoleItem> AvailableRoles
        {
            get => _availableRoles;
            set => SetProperty(ref _availableRoles, value);
        }

        public int? ProjectManagerId
        {
            get => _projectManagerId;
            set
            {
                if (SetProperty(ref _projectManagerId, value))
                {
                    if (value.HasValue && value.Value > 0)
                    {
                        SelectedUserId = value.Value;
                    }
                    OnPropertyChanged(nameof(CanSave));
                }
            }
        }

        // UI Helper Properties
        public bool CanSave => !IsLoading && !IsSaving && SelectedUserId > 0;
        public bool CanCancel => !IsSaving;

        public string SaveButtonText => IsSaving ? "Đang lưu..." : (IsAddMode ? "Thêm thành viên" : "Cập nhật");
        public string CancelButtonText => "Hủy";

        public string MemberSearchScope => "AllUsers"; // Có thể mở rộng nếu cần
        
        public string MemberSearchPlaceholder => "Tìm kiếm thành viên theo tên, email hoặc vai trò...";

        #endregion

        #region Commands

        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand ClearValidationCommand { get; private set; }

        private void InitializeCommands()
        {
            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel, CanExecuteCancel);
            ClearValidationCommand = new RelayCommand(ExecuteClearValidation);
        }

        #endregion

        #region Command Implementations

        private bool CanExecuteSave()
        {
            return CanSave;
        }

        private bool CanExecuteCancel()
        {
            return CanCancel;
        }

        private async void ExecuteSave()
        {
            await ExecuteSaveAsync();
        }

        private async Task ExecuteSaveAsync()
        {
            try
            {
                IsSaving = true;
                ClearValidation();

                // Validate form
                if (!ValidateForm())
                {
                    return;
                }

                // Validate business rules
                await ValidateBusinessRules();

                ProjectMemberDto result = null;

                if (IsAddMode)
                {
                    // Create new member
                    var model = new CreateProjectMemberRequest
                    {
                        UserId = SelectedUserId,
                        ProjectRole = ProjectRole,
                        AllocationPercentage = AllocationPercentage,
                        HourlyRate = HourlyRate,
                        JoinedAt = JoinedAt,
                        Notes = Notes
                    };

                    result = await _projectApiService.AddProjectMemberAsync(_projectId, model);
                }
                else
                {
                    // Update existing member
                    var model = new UpdateProjectMemberRequest
                    {
                        ProjectRole = ProjectRole,
                        AllocationPercentage = AllocationPercentage,
                        HourlyRate = HourlyRate,
                        Notes = Notes,
                        JoinedAt = JoinedAt,
                        LeftAt = LeftAt,
                        RowVersion = _rowVersion,
                    };

                    result = await _projectApiService.UpdateProjectMemberAsync(_projectId, _originalMember.Id, model);
                }

                if (result != null)
                {
                    // Success - close dialog
                    RequestClose?.Invoke(this, new DialogCloseEventArgs(true,"", result));
                }
                else
                {
                    ValidationMessage = "Không thể lưu thông tin thành viên. Vui lòng thử lại.";
                    HasValidationErrors = true;
                }
            }
            catch (Exception ex)
            {
                ValidationMessage = $"Lỗi: {ex.Message}";
                HasValidationErrors = true;
            }
            finally
            {
                IsSaving = false;
            }
        }

        private void ExecuteCancel()
        {
            // Check for unsaved changes
            if (HasUnsavedChanges())
            {
                var result = MessageBox.Show(
                    "Bạn có những thay đổi chưa được lưu. Bạn có chắc chắn muốn hủy không?",
                    "Xác nhận hủy",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                    return;
            }

            RequestClose?.Invoke(this, new DialogCloseEventArgs(false));
        }

        private void ExecuteClearValidation()
        {
            ClearValidation();
        }

        #endregion

        #region Events

        public event System.EventHandler<DialogCloseEventArgs> RequestClose;

        #endregion

        #region Private Methods

        private void InitializeCollections()
        {
            
            _availableRoles = UserRoleExtensions.GetUserRoleMemberItems();
        }

        private async Task InitializeData()
        {
            UpdateDialogTitle();

            if (_originalMember != null)
            {

                var projectMembers = await _projectApiService.GetProjectMembersAsync(_originalMember.ProjectId);

                var member = projectMembers.FirstOrDefault(m => m.Id == _originalMember.Id);

                if(member == null)
                {
                    ValidationMessage = "Không thể tải thông tin thành viên. Vui lòng thử lại.";
                    HasValidationErrors = true;
                    return;
                }
                // Populate form with existing member data
                SelectedUserId = member.UserId;
                SelectedUser = await _userService.GetUserByIdAsync(member.UserId);
                ProjectManagerId = member.ProjectId;

                ProjectRole = member.ProjectRole;

                System.Diagnostics.Debug.WriteLine($"Setting ProjectRole to: {ProjectRole}");

                JoinedAt = member.JoinedAt;
                LeftAt = member.LeftAt;

                // These might not be available in the DTO, so set defaults if needed
                AllocationPercentage = member.AllocationPercentage;
                HourlyRate = member.HourlyRate;
                Notes = member.Notes ?? "";
                _rowVersion = member.RowVersion;

            }
        }

        private void UpdateDialogTitle()
        {
            switch (DialogMode)
            {
                case DialogMode.Add:
                    DialogTitle = "Thêm thành viên mới";
                    break;
                case DialogMode.Edit:
                    DialogTitle = "Chỉnh sửa thành viên";
                    break;
                case DialogMode.View:
                    DialogTitle = "Xem thông tin thành viên";
                    break;
                default:
                    DialogTitle = "Quản lý thành viên";
                    break;
            }
        }

        /// <summary>
        /// cập nhật role cùng với chức vụ thực tế của user
        /// </summary>
        /// <param name="role"></param>
        private void UpdateRoleMember(UserRole role)
        {
            if(role != null && role != UserRole.All)
            {
                ProjectRole = role;
            }    
        }

        private bool ValidateForm()
        {
            ClearValidation();

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(this);

            bool isValid = Validator.TryValidateObject(this, validationContext, validationResults, true);

            if (!isValid)
            {
                var errors = validationResults.Select(r => r.ErrorMessage).ToList();
                ValidationMessage = string.Join(Environment.NewLine, errors);
                HasValidationErrors = true;
                return false;
            }

            return true;
        }

        private async Task ValidateBusinessRules()
        {
            // Check if user is already a member of the project (for Add mode)
            if (IsAddMode)
            {
                try
                {
                    var existingMembers = await _projectApiService.GetProjectMembersAsync(_projectId);
                    if (existingMembers?.Any(m => m.UserId == SelectedUserId && m.IsActive) == true)
                    {
                        throw new InvalidOperationException("Người dùng này đã là thành viên của dự án");
                    }
                }
                catch (InvalidOperationException)
                {
                    throw;
                }
                catch (Exception)
                {
                    // If we can't check, proceed anyway
                }
            }

            // Validate allocation percentage
            if (AllocationPercentage < 0 || AllocationPercentage > 100)
            {
                throw new InvalidOperationException("Phần trăm phân bổ phải từ 0 đến 100");
            }

            // Validate dates
            if (LeftAt.HasValue && LeftAt.Value <= JoinedAt)
            {
                throw new InvalidOperationException("Ngày rời dự án phải sau ngày tham gia");
            }

            // Validate hourly rate
            if (HourlyRate.HasValue && HourlyRate.Value < 0)
            {
                throw new InvalidOperationException("Mức lương theo giờ không thể âm");
            }
        }

        private bool HasUnsavedChanges()
        {
            if (_originalMember == null)
            {
                // Add mode - check if any data entered
                return SelectedUserId > 0 ||
                       ProjectRole != UserRole.All ||
                       AllocationPercentage != 100 ||
                       HourlyRate.HasValue ||
                       !string.IsNullOrEmpty(Notes);
            }
            else
            {
                // Edit mode - check if data changed
                return SelectedUserId != _originalMember.UserId ||
                       ProjectRole != _originalMember.ProjectRole ||
                       AllocationPercentage != _originalMember.AllocationPercentage ||
                       HourlyRate != _originalMember.HourlyRate ||
                       Notes != _originalMember.Notes ||
                       JoinedAt != _originalMember.JoinedAt ||
                       LeftAt != _originalMember.LeftAt;
            }
        }

        private void ClearValidation()
        {
            ValidationMessage = "";
            HasValidationErrors = false;
        }

        private void ValidateProperty(object value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(this) { MemberName = propertyName };

            Validator.TryValidateProperty(value, validationContext, validationResults);

            // Clear any existing validation errors for this property
            if (!validationResults.Any())
            {
                ClearValidation();
            }
        }

        #endregion
    }
}
