using ManagementFile.App.Controls;
using ManagementFile.App.Models;
using ManagementFile.App.Models.Projects;
using ManagementFile.App.Models.Users;
using ManagementFile.App.Services;
using ManagementFile.App.ViewModels.Project;
using ManagementFile.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ManagementFile.App.Views.Project
{
    /// <summary>
    /// Enhanced AddEditProjectDialog with 3 modes: Add, Edit, View
    /// Features comprehensive project management with Department extensions,
    /// TagInputControl, copy-to-clipboard functionality and adaptive UI
    /// </summary>
    public partial class AddEditProjectDialog : Window
    {
        public AddEditProjectDialogViewModel _viewModel { get; }
        private readonly ProjectApiService _projectApiService;
        private readonly UserManagementService _userManagementService;
        private readonly AdminService _adminService;
        private bool _isClosing = false;

        #region Constructors

        public AddEditProjectDialog(
            ProjectApiService projectApiService,
            UserManagementService userManagementService,
            AdminService adminService,
            ProjectModel project = null,
            DialogMode mode = DialogMode.Add,
            int? projectParentId = null)
        {
            InitializeComponent();

            _projectApiService = projectApiService ?? throw new ArgumentNullException(nameof(projectApiService));
            _userManagementService = userManagementService ?? throw new ArgumentNullException(nameof(userManagementService));
            _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));

            Debug.WriteLine($"[{DateTime.Now}] - opening AddEditProjectDialog - Mode: {mode}, Project: {project?.ProjectName ?? "New"}");

            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                try
                {
                    _viewModel = App.GetRequiredService<AddEditProjectDialogViewModel>();

                    // Initialize with project data and mode
                    _ = InitializeViewModelAsync(project, mode, projectParentId);

                    DataContext = _viewModel;
                    _viewModel.RequestClose += OnViewModelRequestClose;
                    _viewModel.PropertyChanged += OnViewModelPropertyChanged;

                    // Initialize UI components
                    InitializeUIComponents();

                    Debug.WriteLine($"[{DateTime.Now}] - ViewModel initialized successfully with mode: {mode}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[{DateTime.Now}] Error initializing ViewModel: {ex.Message}");
                    MessageBox.Show($"❌ Lỗi khởi tạo dialog: {ex.Message}", "Lỗi",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                }
            }
        }

        /// <summary>
        /// Constructor overload for DI container compatibility
        /// </summary>
        public AddEditProjectDialog(ProjectApiService projectApiService, ProjectModel project = null, DialogMode mode = DialogMode.Add)
            : this(projectApiService,
                   App.GetRequiredService<UserManagementService>(),
                   App.GetRequiredService<AdminService>(),
                   project, mode)
        {
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize UI components and animations
        /// </summary>
        private void InitializeUIComponents()
        {
            try
            {
                // Set window animations
                SetupWindowAnimations();

                // Initialize tooltips
                SetupTooltips();

                // Setup theme
                SetupTheme();

                Debug.WriteLine($"[{DateTime.Now}] - UI components initialized");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now}] Error initializing UI components: {ex.Message}");
            }
        }

        /// <summary>
        /// Setup window entrance and exit animations
        /// </summary>
        private void SetupWindowAnimations()
        {
            try
            {
                // Entrance animation
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
                var scaleIn = new DoubleAnimation(0.95, 1, TimeSpan.FromMilliseconds(300));

                fadeIn.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
                scaleIn.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };

                var scaleTransform = new ScaleTransform(0.95, 0.95, ActualWidth / 2, ActualHeight / 2);
                RenderTransform = scaleTransform;
                Opacity = 0;

                BeginAnimation(OpacityProperty, fadeIn);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleIn);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleIn);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now}] Error setting up animations: {ex.Message}");
            }
        }

        /// <summary>
        /// Setup enhanced tooltips
        /// </summary>
        private void SetupTooltips()
        {
            try
            {

                if (ProjectNameTextBox != null)
                {
                    ProjectNameTextBox.ToolTip = "Tên dự án phải rõ ràng và mô tả đúng mục đích của dự án.";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now}] Error setting up tooltips: {ex.Message}");
            }
        }

        /// <summary>
        /// Setup theme based on system settings
        /// </summary>
        private void SetupTheme()
        {
            try
            {
                // Apply theme based on system preferences
                var isDarkMode = SystemParameters.HighContrast;
                if (isDarkMode)
                {
                    // Apply dark theme resources if available
                    // Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/Themes/DarkTheme.xaml", UriKind.Relative) });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now}] Error setting up theme: {ex.Message}");
            }
        }

        /// <summary>
        /// Async initialization of ViewModel to prevent blocking UI
        /// </summary>
        private async Task InitializeViewModelAsync(ProjectModel project, DialogMode mode, int? projectParentId)
        {
            try
            {
                await _viewModel.InitializeAsync(project, mode, projectParentId);
                Debug.WriteLine($"[{DateTime.Now}] - ViewModel initialization completed with mode: {mode}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now}] Error in ViewModel initialization: {ex.Message}");
                Debug.WriteLine($"[{DateTime.Now}] Stack trace: {ex.StackTrace}");

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        var result = MessageBox.Show(
                            $"⚠️ Có lỗi khi tải dữ liệu dự án.\n\nLý do: {ex.Message}\n\nBạn vẫn có thể tiếp tục nhưng một số dữ liệu có thể không được hiển thị chính xác.",
                            "Cảnh báo",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        Debug.WriteLine($"[{DateTime.Now}] - Warning dialog result: {result}");
                    }
                    catch (Exception dialogEx)
                    {
                        Debug.WriteLine($"[{DateTime.Now}] Error showing warning dialog: {dialogEx.Message}");
                    }
                }), System.Windows.Threading.DispatcherPriority.Normal);
            }
        }

        /// <summary>
        /// Handle ViewModel property changes for UI updates
        /// </summary>
        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                switch (e.PropertyName)
                {
                    case nameof(_viewModel.DialogMode):
                        UpdateUIForModeChange();
                        break;
                    case nameof(_viewModel.IsAnyLoading):
                        UpdateLoadingState();
                        break;
                    case nameof(_viewModel.ValidationMessage):
                        if (!string.IsNullOrEmpty(_viewModel.ValidationMessage))
                        {
                            //ShowValidationAnimation();
                        }
                        break;
                    case nameof(_viewModel.SuccessMessage):
                        if (!string.IsNullOrEmpty(_viewModel.SuccessMessage))
                        {
                            ShowSuccessAnimation();
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now}] Error handling property change: {ex.Message}");
            }
        }

        /// <summary>
        /// Update UI when dialog mode changes
        /// </summary>
        private void UpdateUIForModeChange()
        {
            try
            {
                // Apply mode-specific UI changes
                switch (_viewModel.DialogMode)
                {
                    case DialogMode.Add:
                        Title = "Thêm dự án mới";
                        break;
                    case DialogMode.Edit:
                        Title = $"Chỉnh sửa dự án: {_viewModel.ProjectName}";
                        break;
                    case DialogMode.View:
                        Title = $"Xem dự án: {_viewModel.ProjectName}";
                        break;
                }

                Debug.WriteLine($"[{DateTime.Now}] UI updated for mode: {_viewModel.DialogMode}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now}] Error updating UI for mode change: {ex.Message}");
            }
        }

        /// <summary>
        /// Update loading state indicators
        /// </summary>
        private void UpdateLoadingState()
        {
            try
            {
                IsEnabled = !_viewModel.IsAnyLoading;
                Cursor = _viewModel.IsAnyLoading ? Cursors.Wait : Cursors.Arrow;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now}] Error updating loading state: {ex.Message}");
            }
        }

        /// <summary>
        /// Show validation error animation
        /// </summary>
        private void ShowValidationAnimation()
        {
            try
            {
                var shakeAnimation = new DoubleAnimation(0, 10, TimeSpan.FromMilliseconds(50));
                shakeAnimation.AutoReverse = true;
                shakeAnimation.RepeatBehavior = new RepeatBehavior(3);

                var transform = new TranslateTransform();
                RenderTransform = transform;
                transform.BeginAnimation(TranslateTransform.XProperty, shakeAnimation);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now}] Error showing validation animation: {ex.Message}");
            }
        }

        /// <summary>
        /// Show success message animation
        /// </summary>
        private void ShowSuccessAnimation()
        {
            try
            {
                var pulseAnimation = new DoubleAnimation(1, 1.05, TimeSpan.FromMilliseconds(200));
                pulseAnimation.AutoReverse = true;

                var scaleTransform = new ScaleTransform(1, 1, ActualWidth / 2, ActualHeight / 2);
                RenderTransform = scaleTransform;
                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, pulseAnimation);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, pulseAnimation);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now}] Error showing success animation: {ex.Message}");
            }
        }

        /// <summary>
        /// Enhanced dialog close event handler with proper cleanup
        /// </summary>
        private void OnViewModelRequestClose(object sender, DialogCloseEventArgs e)
        {
            if (_isClosing) return;
            _isClosing = true;

            try
            {
                Debug.WriteLine($"[{DateTime.Now}] User 'nguyenbalam57' - Dialog close requested with result: {e.Result}");

                // Set dialog result before closing
                DialogResult = e.Result;

                // Log the action result
                if (e.Result == true)
                {
                    string action;
                    switch( _viewModel.DialogMode)
                    {
                        case DialogMode.Add:
                            action = "created";
                            break;
                        case DialogMode.Edit:
                            action = "updated";
                            break;
                        case DialogMode.View:
                            action = "viewed";
                            break;
                        default:
                            action = "processed";
                            break;
                    };

                    Debug.WriteLine($"[{DateTime.Now}] successfully {action} project: {_viewModel.ProjectName}");
                }
                else
                {
                    Debug.WriteLine($"[{DateTime.Now}] cancelled project dialog");
                }

                // Animate exit
                AnimateExit(() =>
                {
                    // Cleanup and close
                    _viewModel.RequestClose -= OnViewModelRequestClose;
                    _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                    _viewModel.Dispose();
                    Close();
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now}] Error closing dialog: {ex.Message}");
                // Force close even if there's an error
                Close();
            }
        }

        /// <summary>
        /// Animate window exit
        /// </summary>
        private void AnimateExit(Action onComplete)
        {
            try
            {
                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
                var scaleOut = new DoubleAnimation(1, 0.95, TimeSpan.FromMilliseconds(200));

                fadeOut.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn };
                scaleOut.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn };

                fadeOut.Completed += (s, e) => onComplete?.Invoke();

                var scaleTransform = new ScaleTransform(1, 1, ActualWidth / 2, ActualHeight / 2);
                RenderTransform = scaleTransform;

                BeginAnimation(OpacityProperty, fadeOut);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleOut);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleOut);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now}] Error animating exit: {ex.Message}");
                onComplete?.Invoke();
            }
        }

        #endregion

        #region Project Manager Selection Event Handlers

        /// <summary>
        /// Handle UserSelectorControl loaded event for project manager selection
        /// </summary>
        private void ProjectManagerSelector_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var managerSelector = sender as UserSelectorControl;
                if (managerSelector != null)
                {
                    Debug.WriteLine($"[{DateTime.Now}] - ProjectManagerSelector loaded");

                    // Subscribe to property changes manually for better control
                    var dpd = DependencyPropertyDescriptor.FromProperty(
                        UserSelectorControl.SelectedUserProperty,
                        typeof(UserSelectorControl));

                    dpd?.AddValueChanged(managerSelector, ProjectManagerSelector_SelectedUserChanged);

                    // Set initial values if needed
                    UpdateProjectManagerSelector(managerSelector);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now}] Error in ProjectManagerSelector_Loaded: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle project manager selection changes with enhanced validation
        /// </summary>
        private void ProjectManagerSelector_SelectedUserChanged(object sender, EventArgs e)
        {
            try
            {
                var managerSelector = sender as UserSelectorControl;
                var selectedUser = managerSelector?.SelectedUser;

                Debug.WriteLine($"[{DateTime.Now}] - Project manager selected: {selectedUser?.FullName ?? "NULL"}");

                // Update ViewModel with validation
                if (_viewModel != null)
                {
                    _viewModel.SelectedProjectManager = selectedUser;

                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now}] Error in ProjectManagerSelector_SelectedUserChanged: {ex.Message}");
            }
        }

        private void TagInputControl_SelectedTagChanged(object sender, EventArgs e)
        {
            try
            {
                var tagInput = sender as TagInputControl;
                var selectedTag = tagInput?.Tags;

                if (_viewModel != null)
                {
                    _viewModel.TagsList = new ObservableCollection<string>(selectedTag) ?? new ObservableCollection<string>();
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now}] Error in TagInputControl_SelectedTagChanged: {ex.Message}");
            }
        }

        /// <summary>
        /// Update project manager selector with current ViewModel data
        /// </summary>
        private void UpdateProjectManagerSelector(UserSelectorControl selector)
        {
            try
            {
                if (_viewModel?.SelectedProjectManager != null && selector.SelectedUser == null)
                {
                    selector.SelectedUser = _viewModel.SelectedProjectManager;
                    Debug.WriteLine($"[{DateTime.Now}] Updated selector with existing project manager: {_viewModel.SelectedProjectManager.FullName}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now}] Error updating project manager selector: {ex.Message}");
            }
        }

        #endregion

        #region Window Event Overrides

        /// <summary>
        /// Override window closing to ensure proper cleanup and confirmation
        /// </summary>
        protected override void OnClosing(CancelEventArgs e)
        {
            if (_isClosing) return;

            try
            {
                // Check for unsaved changes if user tries to close via X button
                if (DialogResult != true && _viewModel != null && _viewModel.HasUnsavedChanges && _viewModel.DialogMode != DialogMode.View)
                {
                    var result = MessageBox.Show(
                        "⚠️ Bạn có những thay đổi chưa được lưu.\n\nBạn có muốn lưu trước khi đóng không?",
                        "Xác nhận đóng",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question,
                        MessageBoxResult.Cancel);

                    switch (result)
                    {
                        case MessageBoxResult.Yes:
                            // Try to save before closing
                            if (_viewModel.SaveCommand.CanExecute(null))
                            {
                                _viewModel.SaveCommand.Execute(null);
                                e.Cancel = true; // Cancel closing for now, will close after save
                                return;
                            }
                            break;
                        case MessageBoxResult.Cancel:
                            e.Cancel = true;
                            return;
                        case MessageBoxResult.No:
                            // Continue with closing without saving
                            break;
                    }
                }

                Debug.WriteLine($"[{DateTime.Now}] - AddEditProjectDialog closing");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now}] Error in OnClosing: {ex.Message}");
            }

            base.OnClosing(e);
        }

        /// <summary>
        /// Handle window loaded event for final initialization and focus management
        /// </summary>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            try
            {
                Debug.WriteLine($"[{DateTime.Now}] - AddEditProjectDialog fully loaded");

                // Set focus based on dialog mode
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, new Action(() =>
                {
                    try
                    {
                        SetInitialFocus();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[{DateTime.Now}] Error setting focus: {ex.Message}");
                    }
                }));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now}] Error in OnSourceInitialized: {ex.Message}");
            }
        }

        /// <summary>
        /// Set initial focus based on dialog mode
        /// </summary>
        private void SetInitialFocus()
        {
            try
            {
                switch (_viewModel?.DialogMode)
                {
                    case DialogMode.Add:
                        // Focus on project code field for new projects if empty, otherwise project name

                            ProjectNameTextBox?.Focus();
                            Debug.WriteLine($"[{DateTime.Now}] - Focused on ProjectName TextBox in Add mode");
                        break;

                    case DialogMode.Edit:
                        // Focus on project name for editing
                        ProjectNameTextBox?.Focus();
                        ProjectNameTextBox?.SelectAll();
                        Debug.WriteLine($"[{DateTime.Now}] - Focused on ProjectName TextBox in Edit mode");
                        break;

                    case DialogMode.View:
                        // In view mode, focus on first interactive element (Edit button)
                        var editButton = FindVisualChild<Button>(this, btn => btn.Command == _viewModel.EditCommand);
                        editButton?.Focus();
                        Debug.WriteLine($"[{DateTime.Now}] - Focused on Edit button in View mode");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now}] Error setting initial focus: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle window state changes for responsive design
        /// </summary>
        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);

            try
            {
                if (WindowState == WindowState.Maximized)
                {
                    // Adjust layout for maximized window
                    MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
                    MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now}] Error handling state change: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle key down events for enhanced keyboard navigation
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            try
            {
                // Handle special key combinations
                if (e.Key == Key.Tab && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    // Ctrl+Tab cycles through sections
                    CycleToNextSection();
                    e.Handled = true;
                    return;
                }

                if (e.Key == Key.F1)
                {
                    // Show help
                    ShowHelp();
                    e.Handled = true;
                    return;
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now}] Error handling key down: {ex.Message}");
            }

            base.OnKeyDown(e);
        }

        /// <summary>
        /// Cycle focus to next section
        /// </summary>
        private void CycleToNextSection()
        {
            try
            {
                var sections = new List<FrameworkElement>
                {
                    ProjectNameTextBox,
                    ManagerSelector,
                    //TagInputControl
                };

                var currentFocused = Keyboard.FocusedElement as FrameworkElement;
                int currentIndex = sections.IndexOf(currentFocused);
                int nextIndex = (currentIndex + 1) % sections.Count;

                sections[nextIndex]?.Focus();
                Debug.WriteLine($"[{DateTime.Now}] Cycled focus to section {nextIndex}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now}] Error cycling sections: {ex.Message}");
            }
        }

        /// <summary>
        /// Show context-sensitive help
        /// </summary>
        private void ShowHelp()
        {
            try
            {
                string helpMessage = string.Empty;

                switch(_viewModel.DialogMode)
                {
                    case DialogMode.Add:
                        helpMessage = "💡 Hướng dẫn tạo dự án mới:\n\n" +
                                      "• Mã dự án: Chỉ chứa chữ hoa, số, _ và -\n" +
                                      "• Tên dự án: Mô tả rõ ràng mục đích\n" +
                                      "• Chọn quản lý dự án có kinh nghiệm\n" +
                                      "• Ước tính thời gian và ngân sách thực tế\n\n" +
                                      "Phím tắt:\n" +
                                      "• Ctrl+S: Lưu\n" +
                                      "• Esc: Hủy\n" +
                                      "• Ctrl+Tab: Chuyển section";
                        break;
                        case DialogMode.Edit:
                        helpMessage = "✏️ Hướng dẫn chỉnh sửa dự án:\n\n" +
                                      "• Cập nhật thông tin cần thiết\n" +
                                      "• Kiểm tra ảnh hưởng đến team\n" +
                                      "• F5: Khôi phục giá trị ban đầu\n\n" +
                                      "Phím tắt:\n" +
                                      "• Ctrl+S: Lưu thay đổi\n" +
                                      "• F5: Reset\n" +
                                      "• Esc: Hủy";
                        break;
                        case DialogMode.View:
                        helpMessage = "👁️ Hướng dẫn xem dự án:\n\n" +
                                      "• Click vào các icon 📋 để copy\n" +
                                      "• Click chuột phải để xem menu\n" +
                                      "• F2: Chuyển sang chế độ sửa\n\n" +
                                      "Phím tắt:\n" +
                                      "• F2: Chỉnh sửa\n" +
                                      "• F12: Xuất nhanh";
                        break;
                        default:
                        helpMessage = "❓ Trợ giúp ManagementFile";
                        break;
                }

                MessageBox.Show(helpMessage, "Trợ giúp", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now}] Error showing help: {ex.Message}");
            }
        }

        /// <summary>
        /// Enhanced helper method to find visual child controls with predicate
        /// </summary>
        private T FindVisualChild<T>(DependencyObject parent, Func<T, bool> predicate = null) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result && (predicate == null || predicate(result)))
                    return result;

                var childResult = FindVisualChild<T>(child, predicate);
                if (childResult != null)
                    return childResult;
            }
            return null;
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Create dialog for adding a new project with enhanced initialization
        /// </summary>
        public static AddEditProjectDialog CreateForAdd(Window owner = null)
        {
            try
            {
                Debug.WriteLine($"[{DateTime.Now}]- Creating AddEditProjectDialog for new project");

                var dialog = new AddEditProjectDialog(
                    App.GetRequiredService<ProjectApiService>(),
                    App.GetRequiredService<UserManagementService>(),
                    App.GetRequiredService<AdminService>(),
                    null,
                    DialogMode.Add);

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
        /// Create dialog for editing an existing project with validation
        /// </summary>
        public static AddEditProjectDialog CreateForEdit(ProjectModel project, Window owner = null)
        {
            try
            {
                if (project == null)
                    throw new ArgumentNullException(nameof(project), "Project cannot be null for edit mode");

                Debug.WriteLine($"[{DateTime.Now}] - Creating AddEditProjectDialog for editing project: {project.ProjectName}");

                var dialog = new AddEditProjectDialog(
                    App.GetRequiredService<ProjectApiService>(),
                    App.GetRequiredService<UserManagementService>(),
                    App.GetRequiredService<AdminService>(),
                    project,
                    DialogMode.Edit);

                if (owner != null)
                {
                    dialog.Owner = owner;
                }

                return dialog;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now}] Error creating dialog for edit: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Create dialog for viewing an existing project with read-only mode
        /// </summary>
        public static AddEditProjectDialog CreateForView(ProjectModel project, Window owner = null)
        {
            try
            {
                if (project == null)
                    throw new ArgumentNullException(nameof(project), "Project cannot be null for view mode");

                Debug.WriteLine($"[{DateTime.Now}] - Creating AddEditProjectDialog for viewing project: {project.ProjectName}");

                var dialog = new AddEditProjectDialog(
                    App.GetRequiredService<ProjectApiService>(),
                    App.GetRequiredService<UserManagementService>(),
                    App.GetRequiredService<AdminService>(),
                    project,
                    DialogMode.View);

                if (owner != null)
                {
                    dialog.Owner = owner;
                }

                return dialog;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now}] Error creating dialog for view: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectPatentId"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        public static AddEditProjectDialog CreateForChildern(
            int projectParentId,
            Window owner = null)
        {
            try
            {
                Debug.WriteLine($"[{DateTime.Now}] - Creating Childer AddEditProjectDialog for new project, project parent {projectParentId}");

                var dialog = new AddEditProjectDialog(
                    App.GetRequiredService<ProjectApiService>(),
                    App.GetRequiredService<UserManagementService>(),
                    App.GetRequiredService<AdminService>(),
                    null,
                    DialogMode.Add,
                    projectParentId: projectParentId);

                if (owner != null)
                {
                    dialog.Owner = owner;
                }

                    return dialog;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now}] Error creating childer dialog for add: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Create dialog with automatic mode detection based on user permissions
        /// </summary>
        public static AddEditProjectDialog CreateWithAutoMode(ProjectModel project, Window owner = null)
        {
            try
            {
                var userManagementService = App.GetRequiredService<UserManagementService>();
                var currentUser = userManagementService.GetCurrentUser();
                var dialogMode = DialogMode.View; // Default to view mode

                var currentUserProject = new UserModel { Id = currentUser.Id, Role = currentUser.Role, };

                if (project == null)
                {
                    dialogMode = DialogMode.Add;
                }
                else if (currentUser != null)
                {
                    // Check permissions
                    if (currentUserProject.CanEditProject(project))
                    {
                        dialogMode = DialogMode.Edit;
                    }
                }

                Debug.WriteLine($"[{DateTime.Now}]- Auto-detected mode: {dialogMode}");

                return project == null
                    ? CreateForAdd(owner)
                    : dialogMode == DialogMode.Edit
                        ? CreateForEdit(project, owner)
                        : CreateForView(project, owner);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now}] Error creating dialog with auto mode: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Disposal and Cleanup

        /// <summary>
        /// Clean up resources
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Cleanup subscriptions
                if (_viewModel != null)
                {
                    _viewModel.RequestClose -= OnViewModelRequestClose;
                    _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                    _viewModel.Dispose();
                }

                Debug.WriteLine($"[{DateTime.Now}] - AddEditProjectDialog resources cleaned up");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{DateTime.Now}] Error cleaning up resources: {ex.Message}");
            }

            base.OnClosed(e);
        }

        #endregion
    }

    /// <summary>
    /// Message types for temporary notifications
    /// </summary>
    public enum MessageType
    {
        Info,
        Success,
        Warning,
        Error
    }
}