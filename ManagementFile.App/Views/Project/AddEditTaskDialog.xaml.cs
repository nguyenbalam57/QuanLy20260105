using ManagementFile.App.Models;
using ManagementFile.App.Models.Projects;
using ManagementFile.App.Services;
using ManagementFile.App.ViewModels.Controls;
using ManagementFile.App.ViewModels.Project;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace ManagementFile.App.Views.Project
{
    /// <summary>
    /// Dialog để thêm/sửa/xem task với user search functionality
    /// </summary>
    public partial class AddEditTaskDialog : Window
    {
        public AddEditTaskDialogViewModel ViewModel { get; }

        /// <summary>
        /// Constructor for Add new task
        /// </summary>
        public AddEditTaskDialog(ProjectApiService projectApiService, UserManagementService userManagementService, AdminService adminService, int projectId)
            : this(projectApiService, userManagementService, adminService, projectId, null, DialogMode.Add, null)
        {
        }

        /// <summary>
        /// Constructor for Add new task with parentTaskId (for creating subtask)
        /// </summary>
        public AddEditTaskDialog(
            ProjectApiService projectApiService,
            UserManagementService userManagementService,
            AdminService adminService,
            int projectId,
            int? parentTaskId)
            : this(projectApiService, userManagementService, adminService, projectId, null, DialogMode.Add, parentTaskId)
        {
        }

        /// <summary>
        /// Main constructor with mode specification and optional parentTaskId
        /// Support parentTaskId for creating subtasks
        /// </summary>
        public AddEditTaskDialog(
            ProjectApiService projectApiService,
            UserManagementService userManagementService,
            AdminService adminService,
            int projectId,
            ProjectTaskModel task,
            DialogMode mode,
            int? parentTaskId = null)
        {
            InitializeComponent();

            ViewModel = App.GetRequiredService<AddEditTaskDialogViewModel>();
            DataContext = ViewModel;

            ViewModel.RequestClose += (sender, e) =>
            {
                DialogResult = e.Result;
                Close();
            };

            // Initialize with parentTaskId for subtask creation
            _ = ViewModel.InitProjectIdAsyns(projectId, task, mode, parentTaskId);

            // Set window title based on mode
            SetWindowProperties(mode, task, parentTaskId);

            // Enhanced property monitoring for user selections and UI updates
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void TaskComments_CommentActionCompleted(object sender, CommentActionEventArgs e)
        {
            if (e.Success)
            {
                System.Diagnostics.Debug.WriteLine("Thành công load TaskComments");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Lỗi load TaskComments");
            }
        }

        /// <summary>
        /// Enhanced property monitoring for user selections and UI updates
        /// </summary>
        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {

                case nameof(ViewModel.IsLoading):
                    System.Diagnostics.Debug.WriteLine($"IsLoading changed: {ViewModel.IsLoading}");
                    break;

                case nameof(ViewModel.IsLoadingUsers):
                    System.Diagnostics.Debug.WriteLine($"IsLoadingUsers changed: {ViewModel.IsLoadingUsers}");
                    break;
            }
        }

        /// <summary>
        /// Static factory methods for different modes
        /// </summary>

        /// <summary>
        /// Create dialog for adding new task
        /// </summary>
        public static AddEditTaskDialog CreateAddDialog(
            int projectId,
            Window owner = null)
        {
            try
            {
                Debug.WriteLine($"[{DateTime.Now}]- Creating AddEditProjectDialog for new project");

                var dialog = new AddEditTaskDialog(
                    App.GetRequiredService<ProjectApiService>(),
                    App.GetRequiredService<UserManagementService>(),
                    App.GetRequiredService<AdminService>(),
                    projectId,
                    null,
                    DialogMode.Add,
                    null);

                if (owner != null)
                {
                    dialog.Owner = owner;
                }


                return dialog;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now}] Error creating dialog for add: {ex.Message}");
                throw;
            }

        }

        /// <summary>
        /// Create dialog for adding subtask under a parent task
        /// </summary>
        public static AddEditTaskDialog CreateAddSubTaskDialog(
            int projectId,
            int parentTaskId,
            Window owner = null)
        {
            try
            {
                Debug.WriteLine($"[{DateTime.Now}]- Creating AddEditTaskDialog for new subtask");

                var dialog = new AddEditTaskDialog(
                    App.GetRequiredService<ProjectApiService>(),
                    App.GetRequiredService<UserManagementService>(),
                    App.GetRequiredService<AdminService>(),
                    projectId,
                    null,
                    DialogMode.Add,
                    parentTaskId);

                if (owner != null)
                {
                    dialog.Owner = owner;
                }

                return dialog;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now}] Error creating dialog for add Sub Task: {ex.Message}");
                throw;
            }


        }

        /// <summary>
        /// Create dialog for editing existing task
        /// </summary>
        public static AddEditTaskDialog CreateEditDialog(
            int projectId,
            ProjectTaskModel task,
            Window owner = null)
        {
            try
            {
                Debug.WriteLine($"[{DateTime.Now}]- Creating CreateEditDialog for edit");

                var dialog = new AddEditTaskDialog(
                    App.GetRequiredService<ProjectApiService>(),
                    App.GetRequiredService<UserManagementService>(),
                    App.GetRequiredService<AdminService>(),
                    projectId,
                    task,
                    DialogMode.Edit,
                    null);

                if (owner != null)
                {
                    dialog.Owner = owner;
                }

                return dialog;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now}] Error creating dialog for edit Task: {ex.Message}");
                throw;
            }

        }

        /// <summary>
        /// Create dialog for viewing task details
        /// </summary>
        public static AddEditTaskDialog CreateViewDialog(
            int projectId,
            ProjectTaskModel task,
            Window owner = null)
        {
            try
            {
                Debug.WriteLine($"[{DateTime.Now}]- Creating CreateViewDialog for View");

                var dialog = new AddEditTaskDialog(
                    App.GetRequiredService<ProjectApiService>(),
                    App.GetRequiredService<UserManagementService>(),
                    App.GetRequiredService<AdminService>(),
                    projectId,
                    task,
                    DialogMode.View,
                    null);

                if (owner != null)
                {
                    dialog.Owner = owner;
                }

                return dialog;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now}] Error creating dialog for View Task: {ex.Message}");
                throw;
            }

        }

        /// <summary>
        /// ✅ Enhanced: Set window properties with parentTaskId awareness
        /// </summary>
        private void SetWindowProperties(DialogMode mode, ProjectTaskModel task, int? parentTaskId)
        {
            switch (mode)
            {
                case DialogMode.Add:
                    if (parentTaskId.HasValue && parentTaskId.Value > 0)
                    {
                        Title = "Thêm Subtask Mới";
                    }
                    else
                    {
                        Title = "Thêm Task Mới";
                    }
                    break;
                case DialogMode.Edit:
                    Title = $"Chỉnh Sửa Task - {task?.Title ?? ""}";
                    break;
                case DialogMode.View:
                    Title = $"Chi Tiết Task - {task?.Title ?? ""}";
                    break;
            }

            // Adjust window size for enhanced user display and comments section
            if (mode == DialogMode.View)
            {
                Height = 1000; // Increased height to accommodate comments section
                Width = 1400;  // Increased width for better layout
            }
            else
            {
                Height = 950; // Maintain current size for add/edit modes
                Width = 1300;
            }
        }
    }
}