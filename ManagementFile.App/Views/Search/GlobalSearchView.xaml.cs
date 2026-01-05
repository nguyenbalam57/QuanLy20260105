using ManagementFile.App.ViewModels.Search;
using System.Windows.Controls;

namespace ManagementFile.App.Views.Search
{
    /// <summary>
    /// Global Search View - Phase 7 Advanced Integration
    /// Universal search interface across all phases
    /// </summary>
    public partial class GlobalSearchView : UserControl
    {
        public GlobalSearchViewModel ViewModel { get; }

        public GlobalSearchView(
            GlobalSearchViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel ?? throw new System.ArgumentNullException(nameof(viewModel));
            DataContext = ViewModel;
        }

        /// <summary>
        /// Focus search box when view loads
        /// </summary>
        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (SearchBox != null)
            {
                SearchBox.Focus();
            }
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