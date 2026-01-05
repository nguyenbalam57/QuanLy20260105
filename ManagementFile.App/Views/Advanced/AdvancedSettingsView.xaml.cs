using System.Windows.Controls;
using ManagementFile.App.ViewModels.Advanced;

namespace ManagementFile.App.Views.Advanced
{
    /// <summary>
    /// Advanced Settings View for power users
    /// Phase 5 - Polish & Optimization Implementation
    /// </summary>
    public partial class AdvancedSettingsView : UserControl
    {
        private AdvancedSettingsViewModel ViewModel { get; }

        public AdvancedSettingsView(
            AdvancedSettingsViewModel advancedSettingsViewModel)
        {
            InitializeComponent();
            ViewModel = advancedSettingsViewModel ?? throw new System.ArgumentNullException(nameof(advancedSettingsViewModel));
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