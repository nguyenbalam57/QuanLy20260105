using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ManagementFile.App.Services
{
    /// <summary>
    /// Service for managing keyboard shortcuts and accessibility features
    /// Phase 5 Week 14 - UX Enhancement & Advanced Features
    /// </summary>
    public sealed class KeyboardShortcutService : INotifyPropertyChanged
    {

        #region Fields

        private readonly Dictionary<KeyGesture, KeyboardShortcut> _registeredShortcuts;
        private readonly List<KeyboardShortcut> _defaultShortcuts;
        private readonly HashSet<string> _disabledShortcuts;
        
        private bool _isEnabled = true;
        private bool _showTooltips = true;
        private bool _enableAccessibilityMode;
        private string _lastTriggeredShortcut;
        private DateTime _lastShortcutTime;

        #endregion

        #region Constructor

        public KeyboardShortcutService()
        {
            _registeredShortcuts = new Dictionary<KeyGesture, KeyboardShortcut>();
            _defaultShortcuts = new List<KeyboardShortcut>();
            _disabledShortcuts = new HashSet<string>();

            InitializeDefaultShortcuts();
            RegisterDefaultShortcuts();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Is keyboard shortcuts enabled
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        /// <summary>
        /// Show shortcut tooltips
        /// </summary>
        public bool ShowTooltips
        {
            get => _showTooltips;
            set => SetProperty(ref _showTooltips, value);
        }

        /// <summary>
        /// Enable accessibility mode
        /// </summary>
        public bool EnableAccessibilityMode
        {
            get => _enableAccessibilityMode;
            set => SetProperty(ref _enableAccessibilityMode, value);
        }

        /// <summary>
        /// Last triggered shortcut
        /// </summary>
        public string LastTriggeredShortcut
        {
            get => _lastTriggeredShortcut;
            private set => SetProperty(ref _lastTriggeredShortcut, value);
        }

        /// <summary>
        /// Last shortcut activation time
        /// </summary>
        public DateTime LastShortcutTime
        {
            get => _lastShortcutTime;
            private set => SetProperty(ref _lastShortcutTime, value);
        }

        /// <summary>
        /// All registered shortcuts
        /// </summary>
        public IEnumerable<KeyboardShortcut> RegisteredShortcuts => _registeredShortcuts.Values;

        /// <summary>
        /// Available shortcut categories
        /// </summary>
        public IEnumerable<string> Categories => _registeredShortcuts.Values.Select(s => s.Category).Distinct();

        #endregion

        #region UI Helper Properties

        /// <summary>
        /// Shortcuts count text
        /// </summary>
        public string ShortcutsCountText => $"{_registeredShortcuts.Count} shortcuts registered";

        /// <summary>
        /// Last shortcut text
        /// </summary>
        public string LastShortcutText => !string.IsNullOrEmpty(LastTriggeredShortcut) 
            ? $"Last: {LastTriggeredShortcut} at {LastShortcutTime:HH:mm:ss}" 
            : "No shortcuts used recently";

        /// <summary>
        /// Accessibility status text
        /// </summary>
        public string AccessibilityStatusText => EnableAccessibilityMode ? "Accessibility Enhanced" : "Standard Mode";

        #endregion

        #region Methods

        /// <summary>
        /// Initialize default keyboard shortcuts
        /// </summary>
        private void InitializeDefaultShortcuts()
        {
            _defaultShortcuts.AddRange(new[]
            {
                // File Operations
                new KeyboardShortcut("Ctrl+N", "New Project", "File", "Create a new project", () => TriggerCommand("NewProject")),
                new KeyboardShortcut("Ctrl+O", "Open Project", "File", "Open existing project", () => TriggerCommand("OpenProject")),
                new KeyboardShortcut("Ctrl+S", "Save", "File", "Save current work", () => TriggerCommand("Save")),
                new KeyboardShortcut("Ctrl+Shift+S", "Save All", "File", "Save all changes", () => TriggerCommand("SaveAll")),
                new KeyboardShortcut("Ctrl+P", "Print", "File", "Print current view", () => TriggerCommand("Print")),
                
                // Edit Operations
                new KeyboardShortcut("Ctrl+Z", "Undo", "Edit", "Undo last action", () => TriggerCommand("Undo")),
                new KeyboardShortcut("Ctrl+Y", "Redo", "Edit", "Redo last undone action", () => TriggerCommand("Redo")),
                new KeyboardShortcut("Ctrl+C", "Copy", "Edit", "Copy selection", () => TriggerCommand("Copy")),
                new KeyboardShortcut("Ctrl+V", "Paste", "Edit", "Paste from clipboard", () => TriggerCommand("Paste")),
                new KeyboardShortcut("Ctrl+X", "Cut", "Edit", "Cut selection", () => TriggerCommand("Cut")),
                new KeyboardShortcut("Ctrl+A", "Select All", "Edit", "Select all items", () => TriggerCommand("SelectAll")),
                new KeyboardShortcut("Delete", "Delete", "Edit", "Delete selected items", () => TriggerCommand("Delete")),
                
                // Navigation
                new KeyboardShortcut("Ctrl+1", "Dashboard", "Navigation", "Go to dashboard", () => TriggerCommand("GoToDashboard")),
                new KeyboardShortcut("Ctrl+2", "Projects", "Navigation", "Go to projects view", () => TriggerCommand("GoToProjects")),
                new KeyboardShortcut("Ctrl+3", "Tasks", "Navigation", "Go to tasks view", () => TriggerCommand("GoToTasks")),
                new KeyboardShortcut("Ctrl+4", "Reports", "Navigation", "Go to reports view", () => TriggerCommand("GoToReports")),
                new KeyboardShortcut("Ctrl+5", "Settings", "Navigation", "Go to settings", () => TriggerCommand("GoToSettings")),
                new KeyboardShortcut("Alt+Left", "Back", "Navigation", "Navigate back", () => TriggerCommand("NavigateBack")),
                new KeyboardShortcut("Alt+Right", "Forward", "Navigation", "Navigate forward", () => TriggerCommand("NavigateForward")),
                new KeyboardShortcut("F5", "Refresh", "Navigation", "Refresh current view", () => TriggerCommand("Refresh")),
                
                // Search
                new KeyboardShortcut("Ctrl+F", "Find", "Search", "Open find dialog", () => TriggerCommand("Find")),
                new KeyboardShortcut("Ctrl+Shift+F", "Advanced Search", "Search", "Open advanced search", () => TriggerCommand("AdvancedSearch")),
                new KeyboardShortcut("F3", "Find Next", "Search", "Find next occurrence", () => TriggerCommand("FindNext")),
                new KeyboardShortcut("Shift+F3", "Find Previous", "Search", "Find previous occurrence", () => TriggerCommand("FindPrevious")),
                new KeyboardShortcut("Ctrl+H", "Replace", "Search", "Find and replace", () => TriggerCommand("Replace")),
                
                // View
                new KeyboardShortcut("F11", "Full Screen", "View", "Toggle full screen mode", () => TriggerCommand("ToggleFullScreen")),
                new KeyboardShortcut("Ctrl+Plus", "Zoom In", "View", "Zoom in", () => TriggerCommand("ZoomIn")),
                new KeyboardShortcut("Ctrl+Minus", "Zoom Out", "View", "Zoom out", () => TriggerCommand("ZoomOut")),
                new KeyboardShortcut("Ctrl+0", "Reset Zoom", "View", "Reset zoom to 100%", () => TriggerCommand("ResetZoom")),
                new KeyboardShortcut("Ctrl+Shift+T", "Toggle Theme", "View", "Switch between light/dark theme", () => TriggerCommand("ToggleTheme")),
                
                // Tasks
                new KeyboardShortcut("Ctrl+T", "New Task", "Tasks", "Create new task", () => TriggerCommand("NewTask")),
                new KeyboardShortcut("Ctrl+Shift+C", "Complete Task", "Tasks", "Mark task as completed", () => TriggerCommand("CompleteTask")),
                new KeyboardShortcut("Ctrl+D", "Duplicate", "Tasks", "Duplicate selected item", () => TriggerCommand("Duplicate")),
                new KeyboardShortcut("Ctrl+E", "Edit", "Tasks", "Edit selected item", () => TriggerCommand("Edit")),
                
                // Tools
                new KeyboardShortcut("Ctrl+Shift+P", "Command Palette", "Tools", "Open command palette", () => TriggerCommand("CommandPalette")),
                new KeyboardShortcut("Ctrl+Shift+K", "Shortcut Help", "Tools", "Show keyboard shortcuts help", () => ShowShortcutHelp()),
                new KeyboardShortcut("Ctrl+,", "Preferences", "Tools", "Open preferences", () => TriggerCommand("Preferences")),
                new KeyboardShortcut("Ctrl+Shift+I", "Developer Tools", "Tools", "Open developer tools", () => TriggerCommand("DevTools")),
                
                // Bulk Operations
                new KeyboardShortcut("Ctrl+Shift+A", "Select All Visible", "Bulk", "Select all visible items", () => TriggerCommand("SelectAllVisible")),
                new KeyboardShortcut("Ctrl+Shift+D", "Deselect All", "Bulk", "Deselect all items", () => TriggerCommand("DeselectAll")),
                new KeyboardShortcut("Ctrl+Shift+E", "Export Selected", "Bulk", "Export selected items", () => TriggerCommand("ExportSelected")),
                new KeyboardShortcut("Ctrl+Shift+Delete", "Delete Selected", "Bulk", "Delete all selected items", () => TriggerCommand("DeleteSelected")),
                
                // System
                new KeyboardShortcut("Alt+F4", "Exit", "System", "Exit application", () => TriggerCommand("Exit")),
                new KeyboardShortcut("F1", "Help", "System", "Show help", () => TriggerCommand("Help")),
                new KeyboardShortcut("Ctrl+Shift+R", "Restart", "System", "Restart application", () => TriggerCommand("Restart")),
                new KeyboardShortcut("Ctrl+Alt+L", "Lock Screen", "System", "Lock the application", () => TriggerCommand("Lock"))
            });
        }

        /// <summary>
        /// Register default shortcuts
        /// </summary>
        private void RegisterDefaultShortcuts()
        {
            foreach (var shortcut in _defaultShortcuts)
            {
                RegisterShortcut(shortcut);
            }
        }

        /// <summary>
        /// Register a keyboard shortcut
        /// </summary>
        public void RegisterShortcut(KeyboardShortcut shortcut)
        {
            if (shortcut == null || string.IsNullOrEmpty(shortcut.KeyCombination)) return;

            try
            {
                var keyGesture = ParseKeyGesture(shortcut.KeyCombination);
                if (keyGesture != null)
                {
                    _registeredShortcuts[keyGesture] = shortcut;
                    OnPropertyChanged(nameof(RegisteredShortcuts));
                    OnPropertyChanged(nameof(ShortcutsCountText));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error registering shortcut {shortcut.KeyCombination}: {ex.Message}");
            }
        }

        /// <summary>
        /// Unregister a keyboard shortcut
        /// </summary>
        public void UnregisterShortcut(string keyCombination)
        {
            try
            {
                var keyGesture = ParseKeyGesture(keyCombination);
                if (keyGesture != null && _registeredShortcuts.ContainsKey(keyGesture))
                {
                    _registeredShortcuts.Remove(keyGesture);
                    OnPropertyChanged(nameof(RegisteredShortcuts));
                    OnPropertyChanged(nameof(ShortcutsCountText));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error unregistering shortcut {keyCombination}: {ex.Message}");
            }
        }

        /// <summary>
        /// Parse key combination string to KeyGesture
        /// </summary>
        private KeyGesture ParseKeyGesture(string keyCombination)
        {
            try
            {
                return new KeyGesture((Key)Enum.Parse(typeof(Key), keyCombination.Replace("Ctrl+", "").Replace("Shift+", "").Replace("Alt+", "")),
                                    (keyCombination.Contains("Ctrl") ? ModifierKeys.Control : ModifierKeys.None) |
                                    (keyCombination.Contains("Shift") ? ModifierKeys.Shift : ModifierKeys.None) |
                                    (keyCombination.Contains("Alt") ? ModifierKeys.Alt : ModifierKeys.None));
            }
            catch
            {
                // Handle special cases
                if (keyCombination == "Ctrl+Plus") return new KeyGesture(Key.OemPlus, ModifierKeys.Control);
                if (keyCombination == "Ctrl+Minus") return new KeyGesture(Key.OemMinus, ModifierKeys.Control);
                if (keyCombination == "Ctrl+,") return new KeyGesture(Key.OemComma, ModifierKeys.Control);
                if (keyCombination == "Delete") return new KeyGesture(Key.Delete);
                if (keyCombination == "F1") return new KeyGesture(Key.F1);
                if (keyCombination == "F3") return new KeyGesture(Key.F3);
                if (keyCombination == "F5") return new KeyGesture(Key.F5);
                if (keyCombination == "F11") return new KeyGesture(Key.F11);
                if (keyCombination == "Shift+F3") return new KeyGesture(Key.F3, ModifierKeys.Shift);
                if (keyCombination == "Alt+F4") return new KeyGesture(Key.F4, ModifierKeys.Alt);
                if (keyCombination == "Alt+Left") return new KeyGesture(Key.Left, ModifierKeys.Alt);
                if (keyCombination == "Alt+Right") return new KeyGesture(Key.Right, ModifierKeys.Alt);
                
                return null;
            }
        }

        /// <summary>
        /// Process key input and trigger shortcuts
        /// </summary>
        public bool ProcessKeyInput(KeyEventArgs e)
        {
            if (!IsEnabled) return false;

            try
            {
                var modifierKeys = Keyboard.Modifiers;
                var key = e.Key;
                
                // Handle system key events
                if (e.Key == Key.System)
                {
                    key = e.SystemKey;
                }

                var keyGesture = new KeyGesture(key, modifierKeys);
                
                if (_registeredShortcuts.TryGetValue(keyGesture, out KeyboardShortcut shortcut))
                {
                    if (!_disabledShortcuts.Contains(shortcut.Name))
                    {
                        ExecuteShortcut(shortcut);
                        e.Handled = true;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing key input: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Execute keyboard shortcut
        /// </summary>
        private void ExecuteShortcut(KeyboardShortcut shortcut)
        {
            try
            {
                LastTriggeredShortcut = shortcut.Name;
                LastShortcutTime = DateTime.Now;
                
                // Show tooltip if enabled
                if (ShowTooltips && EnableAccessibilityMode)
                {
                    ShowShortcutTooltip(shortcut);
                }

                // Execute the shortcut action
                shortcut.Action?.Invoke();

                OnPropertyChanged(nameof(LastShortcutText));
                
                // Raise shortcut executed event
                ShortcutExecuted?.Invoke(shortcut);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error executing shortcut {shortcut.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Show shortcut tooltip for accessibility
        /// </summary>
        private void ShowShortcutTooltip(KeyboardShortcut shortcut)
        {
            // This would typically show a brief tooltip indicating the action
            // For now, just debug output
            System.Diagnostics.Debug.WriteLine($"Shortcut executed: {shortcut.Name} ({shortcut.KeyCombination})");
        }

        /// <summary>
        /// Trigger command by name
        /// </summary>
        private void TriggerCommand(string commandName)
        {
            // This would typically trigger the actual command
            // For now, we'll just fire an event that consumers can handle
            CommandTriggered?.Invoke(commandName);
            System.Diagnostics.Debug.WriteLine($"Command triggered: {commandName}");
        }

        /// <summary>
        /// Show shortcut help dialog
        /// </summary>
        private void ShowShortcutHelp()
        {
            var helpWindow = new ShortcutHelpWindow(this);
            helpWindow.ShowDialog();
        }

        /// <summary>
        /// Enable shortcut
        /// </summary>
        public void EnableShortcut(string shortcutName)
        {
            _disabledShortcuts.Remove(shortcutName);
        }

        /// <summary>
        /// Disable shortcut
        /// </summary>
        public void DisableShortcut(string shortcutName)
        {
            _disabledShortcuts.Add(shortcutName);
        }

        /// <summary>
        /// Get shortcuts by category
        /// </summary>
        public List<KeyboardShortcut> GetShortcutsByCategory(string category)
        {
            return _registeredShortcuts.Values
                .Where(s => s.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => s.Name)
                .ToList();
        }

        /// <summary>
        /// Find shortcut by key combination
        /// </summary>
        public KeyboardShortcut FindShortcut(string keyCombination)
        {
            var keyGesture = ParseKeyGesture(keyCombination);
            return keyGesture != null && _registeredShortcuts.TryGetValue(keyGesture, out KeyboardShortcut shortcut) 
                ? shortcut 
                : null;
        }

        /// <summary>
        /// Export shortcuts configuration
        /// </summary>
        public List<KeyboardShortcutConfig> ExportConfiguration()
        {
            return _registeredShortcuts.Values
                .Select(s => new KeyboardShortcutConfig
                {
                    Name = s.Name,
                    KeyCombination = s.KeyCombination,
                    Category = s.Category,
                    Description = s.Description,
                    IsEnabled = !_disabledShortcuts.Contains(s.Name)
                })
                .ToList();
        }

        /// <summary>
        /// Import shortcuts configuration
        /// </summary>
        public void ImportConfiguration(List<KeyboardShortcutConfig> config)
        {
            if (config == null) return;

            foreach (var item in config)
            {
                var existing = _registeredShortcuts.Values.FirstOrDefault(s => s.Name == item.Name);
                if (existing != null)
                {
                    // Update existing shortcut
                    if (existing.KeyCombination != item.KeyCombination)
                    {
                        UnregisterShortcut(existing.KeyCombination);
                        existing.KeyCombination = item.KeyCombination;
                        RegisterShortcut(existing);
                    }

                    // Update enabled state
                    if (item.IsEnabled)
                        EnableShortcut(item.Name);
                    else
                        DisableShortcut(item.Name);
                }
            }

            OnPropertyChanged(nameof(RegisteredShortcuts));
        }

        /// <summary>
        /// Reset to default shortcuts
        /// </summary>
        public void ResetToDefaults()
        {
            _registeredShortcuts.Clear();
            _disabledShortcuts.Clear();
            RegisterDefaultShortcuts();
        }

        #endregion

        #region Events

        /// <summary>
        /// Event raised when a shortcut is executed
        /// </summary>
        public event Action<KeyboardShortcut> ShortcutExecuted;

        /// <summary>
        /// Event raised when a command is triggered
        /// </summary>
        public event Action<string> CommandTriggered;

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void SetProperty<T>(ref T backingField, T value, [CallerMemberName] string propertyName = null)
        {
            if (!Equals(backingField, value))
            {
                backingField = value;
                OnPropertyChanged(propertyName);
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

    }

    #region Supporting Classes

    /// <summary>
    /// Keyboard shortcut model
    /// </summary>
    public class KeyboardShortcut
    {
        public string KeyCombination { get; set; } = "";
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public Action Action { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }

        public KeyboardShortcut() { }

        public KeyboardShortcut(string keyCombination, string name, string category, string description, Action action)
        {
            KeyCombination = keyCombination;
            Name = name;
            Category = category;
            Description = description;
            Action = action;
            IsDefault = true;
            CreatedAt = DateTime.Now;
        }
    }

    /// <summary>
    /// Keyboard shortcut configuration for import/export
    /// </summary>
    public class KeyboardShortcutConfig
    {
        public string Name { get; set; } = "";
        public string KeyCombination { get; set; } = "";
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// Simple shortcut help window
    /// </summary>
    public class ShortcutHelpWindow : Window
    {
        public ShortcutHelpWindow(KeyboardShortcutService shortcutService)
        {
            Title = "Keyboard Shortcuts";
            Width = 600;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            
            // This would typically contain a proper WPF layout with shortcuts list
            // For now, just a simple placeholder
            Content = new System.Windows.Controls.TextBlock
            {
                Text = $"Keyboard Shortcuts Help\n\n{shortcutService.ShortcutsCountText}\n\nPress Escape to close this window.",
                Margin = new Thickness(20),
                TextWrapping = TextWrapping.Wrap
            };

            // Close on Escape
            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                    Close();
            };
        }
    }

    #endregion
}