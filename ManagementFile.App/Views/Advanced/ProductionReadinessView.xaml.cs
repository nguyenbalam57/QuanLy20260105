using System.Windows.Controls;
using ManagementFile.App.ViewModels.Advanced;

namespace ManagementFile.App.Views.Advanced
{
    /// <summary>
    /// Production Readiness View for deployment preparation and monitoring
    /// Phase 5 Week 15 - Production Readiness & Final Polish
    /// </summary>
    public partial class ProductionReadinessView : UserControl
    {
        private ProductionReadinessViewModel ViewModel { get; }

        public ProductionReadinessView(
            ProductionReadinessViewModel productionReadinessViewModel)
        {
            InitializeComponent();
            ViewModel = productionReadinessViewModel ?? throw new System.ArgumentNullException(nameof(productionReadinessViewModel));
            DataContext = ViewModel;
        }

        /// <summary>
        /// Cleanup when view is unloaded
        /// </summary>
        private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel?.Dispose();
        }

        /// <summary>
        /// Handle monitoring toggle
        /// </summary>
        private void MonitoringToggle_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Primitives.ToggleButton toggle)
            {
                if (toggle.IsChecked == true)
                {
                    ViewModel.StartMonitoringCommand.Execute(null);
                }
                else
                {
                    ViewModel.StopMonitoringCommand.Execute(null);
                }
            }
        }

        /// <summary>
        /// Handle health check refresh
        /// </summary>
        private void RefreshHealthChecks_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.PerformHealthCheckCommand.Execute(null);
        }

        /// <summary>
        /// Handle security scan
        /// </summary>
        private void SecurityScan_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.PerformSecurityScanCommand.Execute(null);
        }
    }
}