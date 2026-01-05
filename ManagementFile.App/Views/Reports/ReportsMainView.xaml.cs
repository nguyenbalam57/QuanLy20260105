using ManagementFile.App.ViewModels.Reports;
using System.Windows.Controls;

namespace ManagementFile.App.Views.Reports
{
    /// <summary>
    /// Reports Main View - comprehensive reporting system
    /// Phase 4 - Reporting & Analytics Implementation
    /// </summary>
    public partial class ReportsMainView : UserControl
    {
        public ReportsMainViewModel ViewModel { get; }

        public ReportsMainView(
            ReportsMainViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel ?? throw new System.ArgumentNullException(nameof(viewModel));
            DataContext = ViewModel;
        }

        /// <summary>
        /// Cleanup khi view được dispose
        /// </summary>
        private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel?.Dispose();
        }
    }
}