using Dragablz;
using ManagementFile.App.Controls.Projects;
using ManagementFile.App.Models;
using ManagementFile.App.Services;
using ManagementFile.App.ViewModels;
using ManagementFile.App.ViewModels.Project;
using ManagementFile.App.Views.Project;
using System;
using System.Linq;
using System.Windows;

namespace ManagementFile.App.DragablzUser
{
    public class CustomInterTabClient : IInterTabClient
    {
        public INewTabHost<Window> GetNewHost(IInterTabClient interTabClient, object partition, TabablzControl source)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 GetNewHost called - Creating new window");

                // Get dragged item
                var draggedItem = source.SelectedItem as TabItemViewModel;

                if (draggedItem == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ No item being dragged");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"📋 Dragging: {draggedItem.Title}");
                System.Diagnostics.Debug.WriteLine($"   IsDraggable: {draggedItem.IsDraggable}");
                System.Diagnostics.Debug.WriteLine($"   IsPinned: {draggedItem.IsPinned}");
                System.Diagnostics.Debug.WriteLine($"   IsCloseable: {draggedItem.IsCloseable}");

                // ✅ CRITICAL: Block dragging for non-draggable tabs
                if (!draggedItem.IsDraggable || draggedItem.IsPinned)
                {
                    System.Diagnostics.Debug.WriteLine("⛔ TAB IS NOT DRAGGABLE - BLOCKING");

                    // Show message
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        MessageBox.Show(
                            "Tab này không thể kéo ra cửa sổ mới!",
                            "Không thể di chuyển",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }));

                    // ✅ Return NULL để cancel drag operation
                    return null;
                }

                // Tạo window mới
                System.Diagnostics.Debug.WriteLine("🏗️ Creating new window instance");
                var newWindow = new TabWindowNew();

                // ✅ CRITICAL: Tạo ViewModel instance MỚI (không dùng singleton)
                System.Diagnostics.Debug.WriteLine("🔧 Creating new ViewModel instance");
                var controlFactory = App.GetRequiredService<IControlFactory>();
                var newViewModel = new ProjectManagentsDragablzViewViewModel(controlFactory);

                // Set DataContext
                newWindow.DataContext = newViewModel;

                // Cấu hình window
                newWindow.Title = $"Quản lý dự án - Cửa sổ mới ({DateTime.Now:HH:mm:ss})";
                newWindow.Width = 800;
                newWindow.Height = 600;
                newWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                System.Diagnostics.Debug.WriteLine($"✅ Created new window for tab: {draggedItem?.Title}");

                // Trả về NewTabHost
                return new NewTabHost<Window>(newWindow, newWindow.TabControl);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error creating new window: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                MessageBox.Show(
                    $"Lỗi tạo cửa sổ mới: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return null;
            }
        }

        public TabEmptiedResponse TabEmptiedHandler(TabablzControl tabControl, Window window)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔔 TabEmptiedHandler called for window: {window.GetType().Name}");

                bool isMainWindow = window == Application.Current.MainWindow;

                if (isMainWindow)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Main window - keeping open, recreating default tab");

                    var viewModel = window.DataContext as ProjectManagentsDragablzViewViewModel;
                    if (viewModel != null && viewModel.Tabs.Count == 0)
                    {
                        // Tạo lại tab chính
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            System.Diagnostics.Debug.WriteLine("🔨 Recreating main tab");
                            var mainTab = new TabItemViewModel
                            {
                                Title = "Quản lý dự án",
                                Content = App.GetRequiredService<ProjectsControl>(),
                                CreatedTime = DateTime.Now,
                                IconGlyph = "📋"
                            };
                            mainTab.SetAsMainTab();
                            viewModel.Tabs.Add(mainTab);
                            viewModel.SelectedTab = mainTab;
                        }));
                    }

                    return TabEmptiedResponse.DoNothing;
                }

                System.Diagnostics.Debug.WriteLine("✅ Closing secondary window - no tabs left");
                return TabEmptiedResponse.CloseWindowOrLayoutBranch;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error in TabEmptiedHandler: {ex.Message}");
                return TabEmptiedResponse.CloseWindowOrLayoutBranch;
            }
        }
    }
}