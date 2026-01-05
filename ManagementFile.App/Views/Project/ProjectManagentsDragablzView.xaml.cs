using ManagementFile.App.ViewModels.Project;
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

namespace ManagementFile.App.Views.Project
{
    /// <summary>
    /// Interaction logic for ProjectManagentsDragablzView.xaml
    /// </summary>
    public partial class ProjectManagentsDragablzView : UserControl
    {
        private readonly ProjectManagentsDragablzViewViewModel _viewModel;
        public ProjectManagentsDragablzView(
            ProjectManagentsDragablzViewViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;

            _viewModel.Initialize();
        }
    }
}
