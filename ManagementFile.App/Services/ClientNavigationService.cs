using System;
using System.Windows.Input;

namespace ManagementFile.App.Services
{
    /// <summary>
    /// Navigation event arguments
    /// </summary>
    public class NavigationEventArgs : EventArgs
    {
        public string Target { get; }
        public object Parameters { get; }

        public NavigationEventArgs(string target, object parameters)
        {
            Target = target;
            Parameters = parameters;
        }
    }

    /// <summary>
    /// Navigation service for client views
    /// Phase 3 - Client Interface & User Experience (Navigation System)
    /// </summary>
    public sealed class ClientNavigationService
    {
        #region Singleton

        private static readonly Lazy<ClientNavigationService> _instance =
        new Lazy<ClientNavigationService>(() => new ClientNavigationService());

        public static ClientNavigationService Instance => _instance.Value;

        public ClientNavigationService() { }

        #endregion

        #region Events

        /// <summary>
        /// Action raised when navigation is requested
        /// </summary>
        public Action<object, NavigationEventArgs> NavigationRequested { get; set; }

        #endregion

        #region Navigation Methods

        /// <summary>
        /// Navigate to Client Dashboard
        /// </summary>
        public void NavigateToClientDashboard()
        {
            OnNavigationRequested(new NavigationEventArgs("ClientDashboard", null));
        }

        /// <summary>
        /// Navigate to My Workspace with optional tab
        /// </summary>
        /// <param name="tabIndex">Optional tab index (0=Tasks, 1=Files, 2=TimeTracking)</param>
        public void NavigateToMyWorkspace(int? tabIndex = null)
        {
            var parameters = tabIndex.HasValue ? new { TabIndex = tabIndex.Value } : null;
            OnNavigationRequested(new NavigationEventArgs("MyWorkspace", parameters));
        }

        /// <summary>
        /// Navigate to My Workspace Tasks tab
        /// </summary>
        public void NavigateToMyTasks()
        {
            NavigateToMyWorkspace(0);
        }

        /// <summary>
        /// Navigate to My Workspace Files tab
        /// </summary>
        public void NavigateToMyFiles()
        {
            NavigateToMyWorkspace(1);
        }

        /// <summary>
        /// Navigate to My Workspace Time Tracking tab
        /// </summary>
        public void NavigateToTimeTracking()
        {
            NavigateToMyWorkspace(2);
        }

        /// <summary>
        /// Navigate to Collaboration view with optional tab
        /// </summary>
        /// <param name="tabIndex">Optional tab index (0=Team, 1=SharedFiles, 2=Activities, 3=Notifications)</param>
        public void NavigateToCollaboration(int? tabIndex = null)
        {
            var parameters = tabIndex.HasValue ? new { TabIndex = tabIndex.Value } : null;
            OnNavigationRequested(new NavigationEventArgs("Collaboration", parameters));
        }

        /// <summary>
        /// Navigate to Team Members
        /// </summary>
        public void NavigateToTeamMembers()
        {
            NavigateToCollaboration(0);
        }

        /// <summary>
        /// Navigate to Shared Files
        /// </summary>
        public void NavigateToSharedFiles()
        {
            NavigateToCollaboration(1);
        }

        /// <summary>
        /// Navigate to Team Activities
        /// </summary>
        public void NavigateToTeamActivities()
        {
            NavigateToCollaboration(2);
        }

        /// <summary>
        /// Navigate to Notification Center with optional tab
        /// </summary>
        /// <param name="tabIndex">Optional tab index (0=All, 1=Unread, 2=Settings)</param>
        public void NavigateToNotificationCenter(int? tabIndex = null)
        {
            var parameters = tabIndex.HasValue ? new { TabIndex = tabIndex.Value } : null;
            OnNavigationRequested(new NavigationEventArgs("NotificationCenter", parameters));
        }

        /// <summary>
        /// Navigate to All Notifications
        /// </summary>
        public void NavigateToAllNotifications()
        {
            NavigateToNotificationCenter(0);
        }

        /// <summary>
        /// Navigate to Unread Notifications
        /// </summary>
        public void NavigateToUnreadNotifications()
        {
            NavigateToNotificationCenter(1);
        }

        /// <summary>
        /// Navigate to Notification Settings
        /// </summary>
        public void NavigateToNotificationSettings()
        {
            NavigateToNotificationCenter(2);
        }

        /// <summary>
        /// Navigate back to previous view
        /// </summary>
        public void NavigateBack()
        {
            OnNavigationRequested(new NavigationEventArgs("Back", null));
        }

        /// <summary>
        /// Navigate to specific task
        /// </summary>
        /// <param name="taskId">Task ID to navigate to</param>
        public void NavigateToTask(string taskId)
        {
            var parameters = new { TaskId = taskId };
            OnNavigationRequested(new NavigationEventArgs("Task", parameters));
        }

        /// <summary>
        /// Navigate to specific file
        /// </summary>
        /// <param name="fileId">File ID to navigate to</param>
        public void NavigateToFile(string fileId)
        {
            var parameters = new { FileId = fileId };
            OnNavigationRequested(new NavigationEventArgs("File", parameters));
        }

        /// <summary>
        /// Navigate to specific notification
        /// </summary>
        /// <param name="notificationId">Notification ID to navigate to</param>
        public void NavigateToNotification(string notificationId)
        {
            var parameters = new { NotificationId = notificationId };
            OnNavigationRequested(new NavigationEventArgs("Notification", parameters));
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Raise navigation requested action safely
        /// </summary>
        /// <param name="args">Navigation arguments</param>
        private void OnNavigationRequested(NavigationEventArgs args)
        {
            try
            {
                NavigationRequested?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error in NavigationRequested action: {ex.Message}");
            }
        }

        #endregion
    }

    /// <summary>
    /// Navigation command for XAML binding
    /// </summary>
    public class NavigationCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public NavigationCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }

    /// <summary>
    /// Navigation helper class with pre-defined commands
    /// </summary>
    public static class NavigationCommands
    {

        /// <summary>
        /// Navigate to Client Dashboard command
        /// </summary>
        public static ICommand NavigateToClientDashboard => new NavigationCommand(
            _ => ClientNavigationService.Instance.NavigateToClientDashboard());

        /// <summary>
        /// Navigate to My Workspace command
        /// </summary>
        public static ICommand NavigateToMyWorkspace => new NavigationCommand(
            parameter =>
            {
                if (parameter is int tabIndex)
                {
                    ClientNavigationService.Instance.NavigateToMyWorkspace(tabIndex);
                }
                else
                {
                    ClientNavigationService.Instance.NavigateToMyWorkspace();
                }
            });

        /// <summary>
        /// Navigate to My Tasks command
        /// </summary>
        public static ICommand NavigateToMyTasks => new NavigationCommand(
            _ => ClientNavigationService.Instance.NavigateToMyTasks());

        /// <summary>
        /// Navigate to My Files command
        /// </summary>
        public static ICommand NavigateToMyFiles => new NavigationCommand(
            _ => ClientNavigationService.Instance.NavigateToMyFiles());

        /// <summary>
        /// Navigate to Time Tracking command
        /// </summary>
        public static ICommand NavigateToTimeTracking => new NavigationCommand(
            _ => ClientNavigationService.Instance.NavigateToTimeTracking());

        /// <summary>
        /// Navigate to Collaboration command
        /// </summary>
        public static ICommand NavigateToCollaboration => new NavigationCommand(
            parameter =>
            {
                if (parameter is int tabIndex)
                {
                    ClientNavigationService.Instance.NavigateToCollaboration(tabIndex);
                }
                else
                {
                    ClientNavigationService.Instance.NavigateToCollaboration();
                }
            });

        /// <summary>
        /// Navigate to Team Members command
        /// </summary>
        public static ICommand NavigateToTeamMembers => new NavigationCommand(
            _ => ClientNavigationService.Instance.NavigateToTeamMembers());

        /// <summary>
        /// Navigate to Shared Files command
        /// </summary>
        public static ICommand NavigateToSharedFiles => new NavigationCommand(
            _ => ClientNavigationService.Instance.NavigateToSharedFiles());

        /// <summary>
        /// Navigate to Team Activities command
        /// </summary>
        public static ICommand NavigateToTeamActivities => new NavigationCommand(
            _ => ClientNavigationService.Instance.NavigateToTeamActivities());

        /// <summary>
        /// Navigate to Notification Center command
        /// </summary>
        public static ICommand NavigateToNotificationCenter => new NavigationCommand(
            parameter =>
            {
                if (parameter is int tabIndex)
                {
                    ClientNavigationService.Instance.NavigateToNotificationCenter(tabIndex);
                }
                else
                {
                    ClientNavigationService.Instance.NavigateToNotificationCenter();
                }
            });

        /// <summary>
        /// Navigate to All Notifications command
        /// </summary>
        public static ICommand NavigateToAllNotifications => new NavigationCommand(
            _ => ClientNavigationService.Instance.NavigateToAllNotifications());

        /// <summary>
        /// Navigate to Unread Notifications command
        /// </summary>
        public static ICommand NavigateToUnreadNotifications => new NavigationCommand(
            _ => ClientNavigationService.Instance.NavigateToUnreadNotifications());

        /// <summary>
        /// Navigate to Notification Settings command
        /// </summary>
        public static ICommand NavigateToNotificationSettings => new NavigationCommand(
            _ => ClientNavigationService.Instance.NavigateToNotificationSettings());

        /// <summary>
        /// Navigate back command
        /// </summary>
        public static ICommand NavigateBack => new NavigationCommand(
            _ => ClientNavigationService.Instance.NavigateBack());
    }
}