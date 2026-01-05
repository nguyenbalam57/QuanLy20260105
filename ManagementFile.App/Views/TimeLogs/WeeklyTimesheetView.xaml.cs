using ManagementFile.App.Models.Projects;
using ManagementFile.App.ViewModels.TimeLogs;
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

namespace ManagementFile.App.Views.TimeLogs
{
    /// <summary>
    /// Interaction logic for WeeklyTimesheetView.xaml
    /// </summary>
    public partial class WeeklyTimesheetView : UserControl
    {
        private readonly WeeklyTimesheetViewModel _viewModel;

        public WeeklyTimesheetView(WeeklyTimesheetViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;
        }

        public void SetTimeTracking(ProjectModel project)
        {
            _viewModel.SelectProjectModel = project;
        }
    }
}
