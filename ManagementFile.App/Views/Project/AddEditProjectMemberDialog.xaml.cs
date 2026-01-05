using ManagementFile.App.Models;
using ManagementFile.App.Services;
using ManagementFile.App.ViewModels.Project;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.Generic;
using ManagementFile.Contracts.Enums;
using ManagementFile.Contracts.DTOs.ProjectManagement;
using ManagementFile.App.Models.Projects;

namespace ManagementFile.App.Views.Project
{
    /// <summary>
    /// AddEditProjectMemberDialog - Dialog để thêm/sửa thành viên dự án
    /// Enhanced với SearchableUserComboBox cho user selection
    /// </summary>
    public partial class AddEditProjectMemberDialog : Window
    {
        public AddEditProjectMemberViewModel ViewModel { get; }

        public AddEditProjectMemberDialog(
            ProjectApiService projectApiService,
            UserManagementService userManagementService,
            int projectId,
            ProjectMemberModel member = null)
        {
            InitializeComponent();

            // Initialize ViewModel
            ViewModel = new AddEditProjectMemberViewModel(
                projectApiService,
                userManagementService,
                projectId,
                member);

            DataContext = ViewModel;

            // Subscribe to close event
            ViewModel.RequestClose += (sender, e) =>
            {
                DialogResult = e.DialogResult;
                Close();
            };

            // Subscribe to window events
            Loaded += Window_Loaded;
            Closing += Window_Closing;

            // Set up keyboard shortcuts
            SetupKeyboardShortcuts();
        }


        #region Helper Methods

        /// <summary>
        /// Helper method to show/hide search indicator
        /// </summary>
        private void IsSearching(bool isSearching)
        {
            // Could update a loading indicator in the UI
            System.Diagnostics.Debug.WriteLine($"Search in progress: {isSearching}");
        }

        /// <summary>
        /// Gợi ý project role dựa trên user system role
        /// </summary>
        private void SuggestProjectRole(UserRole systemRole)
        {
            try
            {
                ViewModel.ProjectRole = systemRole;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error suggesting role: {ex.Message}");
            }
        }

        /// <summary>
        /// Thiết lập keyboard shortcuts
        /// </summary>
        private void SetupKeyboardShortcuts()
        {
            try
            {
                // Ctrl+S = Save
                var saveGesture = new System.Windows.Input.KeyGesture(
                    System.Windows.Input.Key.S,
                    System.Windows.Input.ModifierKeys.Control);
                var saveCommand = new System.Windows.Input.RoutedCommand();
                saveCommand.InputGestures.Add(saveGesture);
                CommandBindings.Add(new System.Windows.Input.CommandBinding(
                    saveCommand,
                    (s, e) => { if (ViewModel.SaveCommand.CanExecute(null)) ViewModel.SaveCommand.Execute(null); }));

                // Escape = Cancel  
                var cancelGesture = new System.Windows.Input.KeyGesture(System.Windows.Input.Key.Escape);
                var cancelCommand = new System.Windows.Input.RoutedCommand();
                cancelCommand.InputGestures.Add(cancelGesture);
                CommandBindings.Add(new System.Windows.Input.CommandBinding(
                    cancelCommand,
                    (s, e) => { if (ViewModel.CancelCommand.CanExecute(null)) ViewModel.CancelCommand.Execute(null); }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting up keyboard shortcuts: {ex.Message}");
            }
        }

        #endregion

        #region Window Events

        /// <summary>
        /// Xử lý khi window được load
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        /// <summary>
        /// Xử lý khi window đang đóng
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // Cleanup nếu cần
                System.Diagnostics.Debug.WriteLine("AddEditProjectMemberDialog closing");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Window_Closing: {ex.Message}");
            }
        }

        /// <summary>
        /// Xử lý các phím mũi tên và phím Enter trong ProjectManagerComboBox
        /// </summary>
        private void HandleUpArrow(System.Windows.Controls.ComboBox comboBox)
        {
            try
            {
                if (!comboBox.IsDropDownOpen)
                {
                    comboBox.IsDropDownOpen = true;
                    // Đợi dropdown mở xong mới chọn item đầu tiên
                    comboBox.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (comboBox.Items.Count > 0)
                        {
                            comboBox.SelectedIndex = 0;
                            ScrollToSelectedItem(comboBox);
                        }
                    }), System.Windows.Threading.DispatcherPriority.Background);
                    return;
                }

                int currentIndex = comboBox.SelectedIndex;
                if (currentIndex > 0)
                {
                    comboBox.SelectedIndex = currentIndex - 1;
                }
                else if (comboBox.Items.Count > 0)
                {
                    comboBox.SelectedIndex = comboBox.Items.Count - 1; // Wrap to last item
                }

                // Scroll to selected item với delay nhỏ
                ScrollToSelectedItem(comboBox);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in HandleUpArrow: {ex.Message}");
            }
        }

        private void HandleDownArrow(System.Windows.Controls.ComboBox comboBox)
        {
            try
            {
                if (!comboBox.IsDropDownOpen)
                {
                    comboBox.IsDropDownOpen = true;
                    // Đợi dropdown mở xong mới chọn item đầu tiên
                    comboBox.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (comboBox.Items.Count > 0)
                        {
                            comboBox.SelectedIndex = 0;
                            ScrollToSelectedItem(comboBox);
                        }
                    }), System.Windows.Threading.DispatcherPriority.Background);
                    return;
                }

                int currentIndex = comboBox.SelectedIndex;
                if (currentIndex < comboBox.Items.Count - 1)
                {
                    comboBox.SelectedIndex = currentIndex + 1;
                }
                else if (comboBox.Items.Count > 0)
                {
                    comboBox.SelectedIndex = 0; // Wrap to first item
                }

                // Scroll to selected item
                ScrollToSelectedItem(comboBox);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in HandleDownArrow: {ex.Message}");
            }
        }

        private void ScrollToSelectedItem(System.Windows.Controls.ComboBox comboBox)
        {
            try
            {
                if (comboBox.SelectedItem == null) return;

                // Đơn giản: đóng và mở lại dropdown để refresh vị trí
                if (comboBox.IsDropDownOpen)
                {
                    // Đảm bảo UI update trước khi scroll
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(
                        System.Windows.Threading.DispatcherPriority.Render,
                        new Action(() =>
                        {
                            // Thử scroll bằng cách set focus vào selected item
                            var container = comboBox.ItemContainerGenerator.ContainerFromItem(comboBox.SelectedItem);
                            if (container is System.Windows.Controls.ComboBoxItem item)
                            {
                                item.BringIntoView();
                                item.Focus();
                            }
                        }));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error scrolling to selected item: {ex.Message}");
            }
        }

        #endregion

        #region Value Converters

        /// <summary>
        /// Converter cho Save button tooltip
        /// </summary>
        public class SaveButtonTooltipConverter : System.Windows.Data.IValueConverter
        {
            public static SaveButtonTooltipConverter Instance { get; } = new SaveButtonTooltipConverter();

            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if (value is bool isAddMode)
                    return isAddMode ? "Thêm thành viên mới vào dự án" : "Cập nhật thông tin thành viên";
                return "Lưu thay đổi";
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Converter để convert AddMode thành FilterMode
        /// </summary>
        public class AddModeToFilterModeConverter : System.Windows.Data.IValueConverter
        {
            public static AddModeToFilterModeConverter Instance { get; } = new AddModeToFilterModeConverter();

            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if (value is bool isAddMode)
                    return isAddMode ? UserFilterMode.AvailableUsersForProject : UserFilterMode.AllUsers;
                return UserFilterMode.AllUsers;
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Converter để convert AddMode thành PlaceholderText
        /// </summary>
        public class AddModeToPlaceholderConverter : System.Windows.Data.IValueConverter
        {
            public static AddModeToPlaceholderConverter Instance { get; } = new AddModeToPlaceholderConverter();

            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if (value is bool isAddMode)
                    return isAddMode ? "Tìm kiếm người dùng để thêm vào dự án..." : "Thông tin người dùng (không thể thay đổi)";
                return "Chọn hoặc tìm kiếm người dùng...";
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        #endregion
    }
}
