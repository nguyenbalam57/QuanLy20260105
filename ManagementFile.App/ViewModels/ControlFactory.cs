using ManagementFile.App.Controls;
using ManagementFile.App.Controls.Projects;
using ManagementFile.App.Models.Projects;
using ManagementFile.App.ViewModels.Controls;
using ManagementFile.App.ViewModels.Controls.Projects;
using ManagementFile.App.ViewModels.TimeLogs;
using ManagementFile.App.Views.TimeLogs;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagementFile.App.ViewModels
{

    public  interface IControlFactory
    {
        ProjectTasksControl CreateProjectTask(ProjectModel projectModel);

        TaskCommentsControl CreateTaskComment(ProjectTaskModel projectTaskModel);

        ProjectMembersControl CreateProjectMember(ProjectModel projectModel);

        WeeklyTimesheetView CreateTimeTracking(ProjectModel projectModel);
    }

    public class ControlFactory : IControlFactory
    {

        private readonly IServiceProvider _serviceProvider;

        public ControlFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public ProjectTasksControl CreateProjectTask(ProjectModel projectModel)
        {
            var viewModel= _serviceProvider.GetRequiredService<ProjectTasksControlViewModel>();
            var control = new ProjectTasksControl(viewModel);
            control.SetProject(projectModel);
            return control;
        }

        public TaskCommentsControl CreateTaskComment(ProjectTaskModel projectTaskModel)
        {
            var viewModel = _serviceProvider.GetRequiredService<TaskCommentsControlViewModel>();
            var control = new TaskCommentsControl(viewModel);
            control.SetTask(projectTaskModel);
            return control;
        }

        public ProjectMembersControl CreateProjectMember(ProjectModel projectModel)
        {
            var viewModel = _serviceProvider.GetRequiredService<ProjectMembersControlViewModel>();
            var control = new ProjectMembersControl(viewModel);
            control.SetMember(projectModel);
            return control;
        }

        public WeeklyTimesheetView CreateTimeTracking(ProjectModel projectModel)
        {
            var viewModel = _serviceProvider.GetRequiredService<WeeklyTimesheetViewModel>();
            var control = new WeeklyTimesheetView(viewModel);
            control.SetTimeTracking(projectModel);
            return control;
        }

    }
}
