using ManagementFile.App.ViewModels.Advanced;
using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ManagementFile.App.Views.Advanced
{
    /// <summary>
    /// Interaction logic for AdvancedSearchView.xaml
    /// Advanced Search View for enhanced search capabilities
    /// Phase 5 Week 14 - UX Enhancement & Advanced Features
    /// </summary>
    public partial class AdvancedSearchView : UserControl
    {
        private AdvancedSearchViewModel _advancedSearchViewModel { get; }

        public AdvancedSearchView(
            AdvancedSearchViewModel advancedSearchViewModel)
        {
            InitializeComponent();
            _advancedSearchViewModel = advancedSearchViewModel ?? throw new ArgumentNullException(nameof(advancedSearchViewModel));
            DataContext = _advancedSearchViewModel;
        }

        /// <summary>
        /// Cleanup when view is unloaded
        /// </summary>
        private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            _advancedSearchViewModel?.Dispose();
        }

        /// <summary>
        /// Handle search textbox key events
        /// </summary>
        private void SearchTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                _advancedSearchViewModel.SearchCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                _advancedSearchViewModel.ClearSearchCommand.Execute(null);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handle search suggestion selection
        /// </summary>
        private void SuggestionsList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem != null)
            {
                _advancedSearchViewModel.SelectSuggestionCommand.Execute(listBox.SelectedItem);
            }
        }

        /// <summary>
        /// Handle search results double-click
        /// </summary>
        private void SearchResults_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem != null)
            {
                _advancedSearchViewModel.OpenSearchResultCommand.Execute(listBox.SelectedItem);
            }
        }
    }
}
