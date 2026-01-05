using ManagementFile.App.Models;
using ManagementFile.App.Models.Projects;
using ManagementFile.App.Services;
using ManagementFile.App.Views.Project;
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
    public class ProjectMembersControlViewModel : BaseViewModel
    {
        #region Fields

        private readonly ProjectApiService _projectApiService;
        private readonly UserManagementService _userService;
        private readonly AdminService _adminService;

        // Loading states
        private bool _isLoading;
        private string _loadingMessage;


        private ObservableCollection<ProjectMemberModel> _projectMembers;
        private ProjectMemberModel _selectedMember;
        private ProjectModel _selectedProject;

        #endregion

        #region Constructor

        public ProjectMembersControlViewModel(
            ProjectApiService projectApiService,
            UserManagementService userService,
            AdminService adminService)
        {
            _projectApiService = projectApiService ?? throw new ArgumentNullException(nameof(projectApiService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));

            ProjectMembers = new ObservableCollection<ProjectMemberModel>();

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
        /// Project members
        /// </summary>
        public ObservableCollection<ProjectMemberModel> ProjectMembers
        {
            get => _projectMembers;
            set => SetProperty(ref _projectMembers, value);
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
                    // Load members for the selected project
                    Task.Run(async () => await LoadProjectMembersAsync());
                }
            }
        }

        /// <summary>
        /// Selected member
        /// </summary>
        public ProjectMemberModel SelectedMember
        {
            get => _selectedMember;
            set
            {
                if (SetProperty(ref _selectedMember, value))
                {
                    OnPropertyChanged(nameof(HasSelectedMember));
                    OnPropertyChanged(nameof(SelectedMemberInfo));
                }
            }
        }

        public bool HasSelectedProject => SelectedProject != null;
        public bool HasSelectedMember => SelectedMember != null;

        public string SelectedMemberInfo
        {
            get
            {
                if (SelectedMember == null)
                    return "Chưa chọn member nào";
                return $"{SelectedMember.FullName} ({SelectedMember.RoleDisplayName})";
            }
        }

        #endregion

        #region Commands

        public ICommand AddMemberCommand { get; private set; }
        public ICommand UpdateMemberCommand { get; private set; }
        public ICommand RemoveMemberCommand { get; private set; }


        private void InitializeCommands()
        {
            // Member commands
            AddMemberCommand = new RelayCommand(ExecuteAddMember, () => HasSelectedProject);
            UpdateMemberCommand = new RelayCommand(ExecuteUpdateMember, () => HasSelectedMember);
            RemoveMemberCommand = new RelayCommand(async () => await ExecuteRemoveMemberAsync(), () => HasSelectedMember);

        }



        #endregion

        #region Methods

        /// <summary>
        /// Load members của project hiện tại
        /// </summary>
        private async Task LoadProjectMembersAsync()
        {
            if (SelectedProject == null) return;

            try
            {
                IsLoading = true;
                LoadingMessage = "Đang tải danh sách members...";

                var members = await _projectApiService.GetProjectMembersAsync(SelectedProject.Id);

                if (members != null)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        ProjectMembers.Clear();
                        foreach (var memberDto in members)
                        {
                            ProjectMembers.Add(ProjectMemberModel.MapToProjectMemberModel(memberDto));
                        }


                        System.Diagnostics.Debug.WriteLine($"✅ Loaded {members.Count} members for project {SelectedProject.ProjectName}");
                    });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ No members found for project {SelectedProject.ProjectName}");

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        ProjectMembers.Clear();
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading members: {ex.Message}");

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Lỗi tải danh sách members: {ex.Message}", "Lỗi",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Command Implementations

        private void ExecuteAddMember()
        {
            if (SelectedProject == null) return;

            try
            {
                var dialog = new AddEditProjectMemberDialog(
                    _projectApiService,
                    _userService,
                    SelectedProject.Id);

                var result = dialog.ShowDialog();

                if (result == true)
                {
                    // Reload members after adding
                    Task.Run(async () => await LoadProjectMembersAsync());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi mở dialog thêm member: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteUpdateMember()
        {
            if (SelectedMember == null || SelectedProject == null) return;

            try
            {
                // Create ProjectMemberServiceDto from selected member
                var memberDto = new ProjectMemberModel
                {
                    Id = SelectedMember.Id,
                    ProjectId = SelectedMember.ProjectId,
                };

                var dialog = new AddEditProjectMemberDialog(
                    _projectApiService,
                    _userService,
                    SelectedProject.Id,
                    memberDto);

                var result = dialog.ShowDialog();

                if (result == true)
                {
                    // Reload members after updating
                    Task.Run(async () => await LoadProjectMembersAsync());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi mở dialog cập nhật member: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteRemoveMemberAsync()
        {
            if (SelectedMember == null || SelectedProject == null) return;

            try
            {
                IsLoading = true;
                LoadingMessage = "Đang xóa member...";

                var success = await _projectApiService.RemoveProjectMemberAsync(
                    SelectedProject.Id, SelectedMember.UserId);

                if (success)
                {
                    MessageBox.Show("Xóa member thành công!", "Thành công",
                                  MessageBoxButton.OK, MessageBoxImage.Information);

                    // Reload members
                    await LoadProjectMembersAsync();
                }
                else
                {
                    MessageBox.Show("Xóa member thất bại!", "Lỗi",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xóa member: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion
    }
}
