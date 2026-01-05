using ManagementFile.App.Controls;
using ManagementFile.App.Models;
using ManagementFile.App.Services;
using ManagementFile.App.ViewModels;
using ManagementFile.Contracts.DTOs.ProjectManagement;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ManagementFile.App.Views.Project
{
    /// <summary>
    /// Interaction logic for TaskCommentDetailDialog.xaml
    /// </summary>
    public partial class TaskCommentDetailDialog : Window
    {
        private readonly TaskCommentDetailViewModel _viewModel;
        private bool _hasUnsavedChanges = false;

        public TaskCommentDetailDialog(
            int taskId,
            int projectId,
            int parentCommentId,
            TaskCommentService taskCommentService,
            ProjectApiService projectService,
            UserManagementService userService)
        {
            InitializeComponent();
            _viewModel = new TaskCommentDetailViewModel(
                taskCommentService,
                projectService,
                userService,
                this);

            DataContext = _viewModel;

            // Initialize for Add mode
            _viewModel.Initialize(taskId, projectId, DialogMode.Add, null, parentCommentId);

            SetupEventHandlers();
            SetupWindowProperties();
        }

        /// <summary>
        /// Constructor for Edit/View mode
        /// </summary>
        public TaskCommentDetailDialog(
            int projectId,
            TaskCommentDto comment,
            DialogMode mode,
            TaskCommentService taskCommentService,
            ProjectApiService projectService,
            UserManagementService userService)
        {
            InitializeComponent();

            _viewModel = new TaskCommentDetailViewModel(
                taskCommentService,
                projectService,
                userService,
                this);

            DataContext = _viewModel;



            // Initialize with existing comment
            _viewModel.Initialize(comment.TaskId, projectId, mode, comment);

            SetupEventHandlers();
            SetupWindowProperties();
        }


        #region Private Methods

        private void SetupEventHandlers()
        {
            // Subscribe to ViewModel property changes to track unsaved changes
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            _viewModel.RequestClose += (sender, e) =>
            {
                DialogResult = e.DialogResult;
                Close();
            };

            // Window event handlers
            Loaded += OnWindowLoaded;
            Closing += OnWindowClosing;
        }

        private void SetupWindowProperties()
        {
            // Set initial focus based on mode
            if (_viewModel.IsAddMode || _viewModel.IsEditMode)
            {
                Loaded += (s, e) => ContentTextBox?.Focus();
            }
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Track unsaved changes for properties that affect data
            var dataProperties = new[]
            {
                nameof(_viewModel.Content),
                nameof(_viewModel.IssueTitle),
                nameof(_viewModel.SuggestedFix),
                nameof(_viewModel.RelatedModule),
                nameof(_viewModel.CommentType),
                nameof(_viewModel.Priority),
                nameof(_viewModel.SelectedReviewer),
                nameof(_viewModel.SelectedAssignee),
                nameof(_viewModel.EstimatedFixTime),
                nameof(_viewModel.DueDate),
                nameof(_viewModel.IsBlocking),
                nameof(_viewModel.RequiresDiscussion)
            };

            if (Array.Exists(dataProperties, prop => prop == e.PropertyName))
            {
                _hasUnsavedChanges = !_viewModel.IsViewMode;
            }
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // Debug binding values
            System.Diagnostics.Debug.WriteLine($"[Dialog] Window Loaded - Debug Info:");
            System.Diagnostics.Debug.WriteLine($"- ViewModel.ProjectId: {_viewModel.ProjectId}");
            System.Diagnostics.Debug.WriteLine($"- ViewModel.IsReadOnly: {_viewModel.IsReadOnly}");
            System.Diagnostics.Debug.WriteLine($"- ReviewerSelector.ProjectId: {ReviewerSelector.ProjectId}");
            System.Diagnostics.Debug.WriteLine($"- ReviewerSelector.SearchScope: {ReviewerSelector.SearchScope}");
            System.Diagnostics.Debug.WriteLine($"- ReviewerSelector.IsReadOnly: {ReviewerSelector.IsReadOnly}");
            System.Diagnostics.Debug.WriteLine($"- ReviewerSelector.DataContext: {ReviewerSelector.DataContext?.GetType().Name}");

            // Test binding manually
            Dispatcher.BeginInvoke(new Action(() =>
            {
                // Force refresh after UI is fully loaded
                System.Diagnostics.Debug.WriteLine($"[Dialog] After UI loaded - ReviewerSelector.ProjectId: {ReviewerSelector.ProjectId}");

                // Manual check
                var binding = BindingOperations.GetBinding(ReviewerSelector, UserSelectorControl.ProjectIdProperty);
                System.Diagnostics.Debug.WriteLine($"[Dialog] ProjectId Binding: {binding?.Path?.Path} | Mode: {binding?.Mode}");

                var bindingExpr = BindingOperations.GetBindingExpression(ReviewerSelector, UserSelectorControl.ProjectIdProperty);
                System.Diagnostics.Debug.WriteLine($"[Dialog] ProjectId Binding Status: {bindingExpr?.Status}");
                if (bindingExpr?.Status == BindingStatus.PathError)
                {
                    System.Diagnostics.Debug.WriteLine($"[Dialog] Binding Error: Path '{binding?.Path?.Path}' not found on '{DataContext?.GetType().Name}'");
                }
            }), DispatcherPriority.Loaded);

            // Set initial focus
            if (_viewModel.IsAddMode || _viewModel.IsEditMode)
            {
                ContentTextBox?.Focus();
                ContentTextBox?.SelectAll();
            }

            // Reset unsaved changes flag after initial load
            _hasUnsavedChanges = false;
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            // Check for unsaved changes
            if (_hasUnsavedChanges && !_viewModel.IsViewMode)
            {
                var result = MessageBox.Show(
                    "Bạn có thay đổi chưa được lưu. Bạn có muốn lưu trước khi đóng không?",
                    "Xác nhận đóng",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question,
                    MessageBoxResult.Cancel);

                switch (result)
                {
                    case MessageBoxResult.Yes:
                        // Try to save
                        if (_viewModel.SaveCommand.CanExecute(null))
                        {
                            _viewModel.SaveCommand.Execute(null);
                            // If save is successful, the dialog will close automatically
                            // If save fails, cancel the close operation
                            if (_viewModel.HasStatusMessage && _viewModel.StatusMessage.Contains("Lỗi"))
                            {
                                e.Cancel = true;
                            }
                        }
                        else
                        {
                            e.Cancel = true;
                        }
                        break;

                    case MessageBoxResult.No:
                        // Don't save, just close
                        break;

                    case MessageBoxResult.Cancel:
                        // Cancel the close operation
                        e.Cancel = true;
                        break;
                }
            }
        }

        private void ReviewerSelector_Loaded(object sender, RoutedEventArgs e)
        {
            var reviewerSelector = sender as UserSelectorControl;
            if (reviewerSelector != null)
            {
                // Subscribe to property changes manually
                var dpd = DependencyPropertyDescriptor.FromProperty(UserSelectorControl.SelectedUserProperty, typeof(UserSelectorControl));
                dpd.AddValueChanged(reviewerSelector, ReviewerSelector_SelectedUserChanged);
            }
        }

        private void ReviewerSelector_SelectedUserChanged(object sender, EventArgs e)
        {
            var reviewerSelector = sender as UserSelectorControl;
            var selectedUser = reviewerSelector?.SelectedUser;

            System.Diagnostics.Debug.WriteLine($"[Dialog] Manual event - Selected user: {selectedUser?.FullName ?? "NULL"}");

            // Manually update ViewModel if binding isn't working

            _viewModel.SelectedReviewer = selectedUser;

        }

        private void AssignSelector_Loaded(object sender, RoutedEventArgs e)
        {
            var assignSelector = sender as UserSelectorControl;
            if (assignSelector != null)
            {
                // Subscribe to property changes manually
                var dpd = DependencyPropertyDescriptor.FromProperty(UserSelectorControl.SelectedUserProperty, typeof(UserSelectorControl));
                dpd.AddValueChanged(assignSelector, AssignSelector_SelectedUserChanged);
            }
        }

        private void AssignSelector_SelectedUserChanged(object sender, EventArgs e)
        {
            var assignSelector = sender as UserSelectorControl;
            var selectedUser = assignSelector?.SelectedUser;

            System.Diagnostics.Debug.WriteLine($"[Dialog] Manual event - Selected user: {selectedUser?.FullName ?? "NULL"}");

            // Manually update ViewModel if binding isn't working

            _viewModel.SelectedAssignee = selectedUser;

        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Get the result data after dialog closes
        /// </summary>
        public TaskCommentDto GetResult()
        {
            return _viewModel.OriginalComment;
        }

        /// <summary>
        /// Check if the dialog has unsaved changes
        /// </summary>
        public bool HasUnsavedChanges()
        {
            return _hasUnsavedChanges && !_viewModel.IsViewMode;
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Create dialog for adding new comment
        /// </summary>
        public static TaskCommentDetailDialog CreateForAdd(
            int taskId,
            int projectId,
            Window owner = null)
        {
            var dialog = new TaskCommentDetailDialog(
                taskId,
                projectId,
                parentCommentId: 0,
                App.GetService<TaskCommentService>(),
                App.GetService<ProjectApiService>(),
                App.GetService<UserManagementService>());

            // Safely set owner
            try
            {
                if (owner != null && owner.IsLoaded)
                {
                    dialog.Owner = owner;
                }
                else if (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsLoaded)
                {
                    dialog.Owner = Application.Current.MainWindow;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CommentLine] Could not set owner: {ex.Message}");
                // Continue without owner
            }

            return dialog;
        }

        /// <summary>
        /// Create dialog for viewing comment
        /// </summary>
        public static TaskCommentDetailDialog CreateForView(
            int projectId,
            TaskCommentDto comment,
            Window owner = null)
        {
            var dialog = new TaskCommentDetailDialog(
                projectId,
                comment,
                DialogMode.View,
                App.GetService<TaskCommentService>(),
                App.GetService<ProjectApiService>(),
                App.GetService<UserManagementService>());

            // Safely set owner
            try
            {
                if (owner != null && owner.IsLoaded)
                {
                    dialog.Owner = owner;
                }
                else if (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsLoaded)
                {
                    dialog.Owner = Application.Current.MainWindow;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CommentLine] Could not set owner: {ex.Message}");
                // Continue without owner
            }

            return dialog;
        }

        /// <summary>
        /// Create dialog for editing comment
        /// </summary>
        public static TaskCommentDetailDialog CreateForEdit(
            int projectId,
            TaskCommentDto comment,
            Window owner = null)
        {
            var dialog = new TaskCommentDetailDialog(
                projectId,
                comment,
                DialogMode.Edit,
                App.GetService<TaskCommentService>(),
                App.GetService<ProjectApiService>(),
                App.GetService<UserManagementService>());

            // Safely set owner
            try
            {
                if (owner != null && owner.IsLoaded)
                {
                    dialog.Owner = owner;
                }
                else if (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsLoaded)
                {
                    dialog.Owner = Application.Current.MainWindow;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CommentLine] Could not set owner: {ex.Message}");
                // Continue without owner
            }

            return dialog;
        }

        /// <summary>
        /// Create dialog for replying to a comment
        /// </summary>
        public static TaskCommentDetailDialog CreateForReply(
            int taskId,
            int projectId,
            int parentCommentId,
            Window owner = null)
        {
            var dialog = new TaskCommentDetailDialog(
                taskId,
                projectId,
                parentCommentId,
                App.GetService<TaskCommentService>(),
                App.GetService<ProjectApiService>(),
                App.GetService<UserManagementService>());

            // Safely set owner
            try
            {
                if (owner != null && owner.IsLoaded)
                {
                    dialog.Owner = owner;
                }
                else if (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsLoaded)
                {
                    dialog.Owner = Application.Current.MainWindow;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CommentLine] Could not set owner: {ex.Message}");
                // Continue without owner
            }

            return dialog;
        }

        #endregion
    }
}
