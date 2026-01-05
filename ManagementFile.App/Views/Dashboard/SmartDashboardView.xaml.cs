using ManagementFile.App.ViewModels.Dashboard;
using System.Windows.Controls;

namespace ManagementFile.App.Views.Dashboard
{
    /// <summary>
    /// Smart Dashboard View - Phase 7 Advanced Integration
    /// Unified dashboard tích hợp tất cả metrics từ 5 phases
    /// </summary>
    public partial class SmartDashboardView : UserControl
    {
        public SmartDashboardViewModel ViewModel { get; }

        public SmartDashboardView(
            SmartDashboardViewModel smartDashboardViewModel)
        {
            InitializeComponent();
            ViewModel = smartDashboardViewModel ?? throw new System.ArgumentNullException(nameof(smartDashboardViewModel));
            DataContext = ViewModel;
        }

        /// <summary>
        /// Cleanup when view is unloaded
        /// </summary>
        private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel?.Dispose();
        }
    }
}