using ManagementFile.App.Models.Projects;
using ManagementFile.App.ViewModels.Controls.Projects;
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

namespace ManagementFile.App.Controls.Projects
{
    /// <summary>
    /// Interaction logic for ProjectMembersControl.xaml
    /// </summary>
    public partial class ProjectMembersControl : UserControl
    {
        private readonly ProjectMembersControlViewModel _viewModel;

        public ProjectMembersControl(ProjectMembersControlViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel ?? throw new ArgumentException(nameof(viewModel));
            DataContext = _viewModel;

            MembersList.MouseRightButtonUp += MembersList_MouseRightButtonUp;
            MembersList.MouseDoubleClick += MembersList_MouseDoubleClick;
            MembersList.KeyDown += MembersList_KeyDown;
        }

        public void SetMember(ProjectModel project)
        {
            _viewModel.SelectedProject = project;
        }

        private void MembersList_KeyDown(object sender, KeyEventArgs e)
        {
            
        }

        private void MembersList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            
        }

        private void MembersList_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var dataGrid = sender as DataGrid;
                if (dataGrid == null) return;

                var hitTest = VisualTreeHelper.HitTest(dataGrid, e.GetPosition(dataGrid));
                var row = FindVisualParent<DataGridRow>(hitTest.VisualHit);

                if (row?.Item is ProjectMemberModel projectMemberModel)
                {
                    // Select the row
                    dataGrid.SelectedItem = projectMemberModel;
                    _viewModel.SelectedMember = projectMemberModel;

                    // Show enhanced context menu
                    ShowEnhancedContextMenu(projectMemberModel, row);
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[projectMemberModel] Error in CommentsDataGrid_MouseRightButtonUp: {ex.Message}");
            }
        }

        /// <summary>
        /// Find visual parent of specific type
        /// </summary>
        private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parentObject = VisualTreeHelper.GetParent(child);

            if (parentObject == null)
                return null;

            if (parentObject is T parent)
                return parent;

            return FindVisualParent<T>(parentObject);
        }

        private void ShowEnhancedContextMenu(ProjectMemberModel projectMemberModel, FrameworkElement target)
        {
            try
            {
                var contextMenu = new ContextMenu();
                //Basic actions
                contextMenu.Items.Add(
                    new MenuItem
                    {
                        Header = "Thêm mới thành viên",
                        Tag = projectMemberModel,
                        Command = _viewModel?.AddMemberCommand,
                        CommandParameter = projectMemberModel,
                        ToolTip = "Thêm mới thành viên dự án"
                    });
                contextMenu.Items.Add(
                    new MenuItem
                    {
                        Header = "✏️ Chỉnh sửa thành viên",
                        Tag = projectMemberModel,
                        Command = _viewModel?.UpdateMemberCommand,
                        CommandParameter = projectMemberModel,
                        ToolTip = "Chỉnh sửa thông tin thành viên dự án",
                        //Visibility = GetMenuItemVisibility(CanCurrentUserEditComment(projectModel)),
                    });

                // Show context menu 
                contextMenu.PlacementTarget = target;
                contextMenu.IsOpen = true;

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ProjectMemberModel] Error showing enhanced context menu: {ex.Message}");

                // Fallback to simple context menu
                //ShowSimpleContextMenu(comment, target);
            }
        }


    }
}
