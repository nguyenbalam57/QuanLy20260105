using ManagementFile.App.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace ManagementFile.App.Controls
{
    /// <summary>
    /// FileListControl - Complete implementation for file management
    /// </summary>
    public partial class FileListControl : UserControl, INotifyPropertyChanged
    {
        private DispatcherTimer _statusTimer;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] User 'nguyenbalam57' - Property changed: {propertyName}");
        }

        #region Dependency Properties

        public static readonly DependencyProperty FileListProperty =
            DependencyProperty.Register("FileList", typeof(ObservableCollection<string>),
                typeof(FileListControl),
                new FrameworkPropertyMetadata(new ObservableCollection<string>(),
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnFileListChanged));

        private static void OnFileListChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FileListControl)d;

            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] User 'nguyenbalam57' - FileList changed from {(e.OldValue as ObservableCollection<string>)?.Count ?? 0} to {(e.NewValue as ObservableCollection<string>)?.Count ?? 0} items");

            if (e.NewValue != null && !(e.NewValue is ObservableCollection<string>))
            {
                control.FileList = new ObservableCollection<string>();
            }

            // Unsubscribe from old collection
            if (e.OldValue is ObservableCollection<string> oldCollection)
            {
                oldCollection.CollectionChanged -= control.OnFileListCollectionChanged;
            }

            // Subscribe to new collection
            if (e.NewValue is ObservableCollection<string> newCollection)
            {
                newCollection.CollectionChanged += control.OnFileListCollectionChanged;
            }

            control.RefreshFileListItems();
        }

        public ObservableCollection<string> FileList
        {
            get => (ObservableCollection<string>)GetValue(FileListProperty);
            set => SetValue(FileListProperty, value);
        }

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(FileListControl),
                new PropertyMetadata(false, OnIsReadOnlyChanged));

        private static void OnIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FileListControl)d;

            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] User 'nguyenbalam57' - IsReadOnly changed from {e.OldValue} to {e.NewValue}");

            control.AllowDrop = !(bool)e.NewValue;
            control.UpdateReadOnlyState();
        }

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public static readonly DependencyProperty AllowMultipleProperty =
            DependencyProperty.Register("AllowMultiple", typeof(bool), typeof(FileListControl),
                new PropertyMetadata(true));

        public bool AllowMultiple
        {
            get => (bool)GetValue(AllowMultipleProperty);
            set => SetValue(AllowMultipleProperty, value);
        }

        public static readonly DependencyProperty FileFilterProperty =
            DependencyProperty.Register("FileFilter", typeof(string), typeof(FileListControl),
                new PropertyMetadata("All files (*.*)|*.*"));

        public string FileFilter
        {
            get => (string)GetValue(FileFilterProperty);
            set => SetValue(FileFilterProperty, value);
        }

        public static readonly DependencyProperty MaxFileSizeProperty =
            DependencyProperty.Register("MaxFileSize", typeof(long), typeof(FileListControl),
                new PropertyMetadata(50L * 1024 * 1024)); // 50MB default

        public long MaxFileSize
        {
            get => (long)GetValue(MaxFileSizeProperty);
            set => SetValue(MaxFileSizeProperty, value);
        }

        public static readonly DependencyProperty MaxFilesProperty =
            DependencyProperty.Register("MaxFiles", typeof(int), typeof(FileListControl),
                new PropertyMetadata(10));

        public int MaxFiles
        {
            get => (int)GetValue(MaxFilesProperty);
            set => SetValue(MaxFilesProperty, value);
        }

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register("Placeholder", typeof(string), typeof(FileListControl),
                new PropertyMetadata("Nhập đường dẫn file hoặc chọn file...", OnPlaceholderChanged));

        private static void OnPlaceholderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FileListControl)d;
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] User 'nguyenbalam57' - Placeholder changed to: {e.NewValue}");
            control.UpdatePlaceholderText();
        }

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        #endregion

        #region Internal Properties (Manual UI Management)

        private string _filePath = "";
        public string FilePath
        {
            get => _filePath;
            set
            {
                if (_filePath != value)
                {
                    _filePath = value;
                    if (FilePathTextBox != null)
                        FilePathTextBox.Text = value;

                    OnPropertyChanged();
                    UpdateCanAddFile();
                    UpdatePlaceholderVisibility();

                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] User 'nguyenbalam57' - FilePath set to: {value}");
                }
            }
        }

        public bool CanAddFile => !string.IsNullOrWhiteSpace(FilePath) &&
                                  !IsReadOnly &&
                                  (FileList?.Count ?? 0) < MaxFiles;

        public bool HasFiles => FileListItems?.Count > 0;

        private ObservableCollection<FileListItem> _fileListItems = new ObservableCollection<FileListItem>();
        public ObservableCollection<FileListItem> FileListItems
        {
            get => _fileListItems;
            set
            {
                _fileListItems = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasFiles));
                UpdateFileListDisplay();
            }
        }

        #endregion

        #region Status Management Properties

        private string _statusMessage = "";
        private string _statusIcon = "";
        private Brush _statusBackground = Brushes.Transparent;
        private Brush _statusForeground = Brushes.Black;
        private Brush _statusBorderBrush = Brushes.Transparent;

        public bool HasStatusMessage => !string.IsNullOrEmpty(_statusMessage);

        #endregion

        public FileListControl()
        {
            InitializeComponent();

            // ✅ DON'T set DataContext = this - Let parent handle DataContext

            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] User 'nguyenbalam57' - Initializing FileListControl");

            // Initialize timer for status messages
            _statusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _statusTimer.Tick += (s, e) =>
            {
                ClearStatus();
                _statusTimer.Stop();
            };

            // Initialize FileList if null
            if (FileList == null)
            {
                FileList = new ObservableCollection<string>();
            }

            // Setup UI after load
            Loaded += OnControlLoaded;
        }

        private void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] User 'nguyenbalam57' - FileListControl loaded");
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] - DataContext: {DataContext?.GetType().Name ?? "NULL"}");
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] - FileList Count: {FileList?.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] - IsReadOnly: {IsReadOnly}");
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] - FileFilter: {FileFilter}");
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] - MaxFiles: {MaxFiles}");
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] - MaxFileSize: {FormatFileSize(MaxFileSize)}");

            InitializeUI();
            RefreshFileListItems();
        }

        private void OnFileListCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] User 'nguyenbalam57' - FileList collection changed - Action: {e.Action}");
            RefreshFileListItems();
        }

        #region UI Management Methods

        private void InitializeUI()
        {
            // Setup file list ItemsSource
            if (FileListPanel != null)
                FileListPanel.ItemsSource = FileListItems;

            UpdatePlaceholderText();
            UpdateReadOnlyState();
            UpdateCanAddFile();
            UpdateFileListDisplay();
        }

        private void UpdatePlaceholderText()
        {
            if (PlaceholderTextBlock != null)
            {
                PlaceholderTextBlock.Text = Placeholder ?? "Nhập đường dẫn file hoặc chọn file...";
            }
        }

        private void UpdatePlaceholderVisibility()
        {
            if (PlaceholderTextBlock != null)
            {
                PlaceholderTextBlock.Visibility = string.IsNullOrEmpty(FilePath) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void UpdateReadOnlyState()
        {
            // Update Add File Section visibility
            if (AddFileSection != null)
                AddFileSection.Visibility = IsReadOnly ? Visibility.Collapsed : Visibility.Visible;

            // Update individual controls
            if (FilePathTextBox != null)
                FilePathTextBox.IsEnabled = !IsReadOnly;
            if (BrowseButton != null)
                BrowseButton.IsEnabled = !IsReadOnly;
            if (AddButton != null)
                AddButton.IsEnabled = CanAddFile;

            // Update empty state hint
            if (EmptyStateHint != null)
                EmptyStateHint.Visibility = IsReadOnly ? Visibility.Collapsed : Visibility.Visible;

            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] User 'nguyenbalam57' - ReadOnly state updated: {IsReadOnly}");
        }

        private void UpdateCanAddFile()
        {
            OnPropertyChanged(nameof(CanAddFile));

            if (AddButton != null)
                AddButton.IsEnabled = CanAddFile;

            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] User 'nguyenbalam57' - CanAddFile: {CanAddFile}");
        }

        private void UpdateFileListDisplay()
        {
            var hasFiles = HasFiles;

            // Update ScrollViewer visibility
            if (FileListScrollViewer != null)
                FileListScrollViewer.Visibility = hasFiles ? Visibility.Visible : Visibility.Collapsed;

            // Update EmptyState visibility
            if (EmptyState != null)
                EmptyState.Visibility = hasFiles ? Visibility.Collapsed : Visibility.Visible;

            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] User 'nguyenbalam57' - File list display updated - HasFiles: {hasFiles}");
        }

        #endregion

        #region Event Handlers

        private void FilePathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilePath = FilePathTextBox.Text;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            BrowseFiles();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddCurrentFile();
        }

        private void FilePathTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddCurrentFile();
                e.Handled = true;
            }
        }

        private void RemoveFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string filePath)
            {
                RemoveFile(filePath);
            }
        }

        private void RemoveFileContextMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is string filePath)
            {
                RemoveFile(filePath);
            }
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is string filePath)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] User 'nguyenbalam57' opened file: {Path.GetFileName(filePath)}");
                    }
                    else
                    {
                        ShowError("File không tồn tại", "❌");
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"Không thể mở file: {ex.Message}", "❌");
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] Error opening file: {ex.Message}");
                }
            }
        }

        private void CopyPath_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is string filePath)
            {
                try
                {
                    Clipboard.SetText(filePath);
                    ShowSuccess("Đã copy đường dẫn vào clipboard", "📋");
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] copied path: {filePath}");
                }
                catch (Exception ex)
                {
                    ShowError($"Không thể copy: {ex.Message}", "❌");
                }
            }
        }

        private void FileItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && sender is Border border && border.DataContext is FileListItem item)
            {
                try
                {
                    if (File.Exists(item.FullPath))
                    {
                        Process.Start(new ProcessStartInfo(item.FullPath) { UseShellExecute = true });
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] double-clicked file: {item.FileName}");
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"Không thể mở file: {ex.Message}", "❌");
                }
            }
        }

        #endregion

        #region Drag & Drop Events

        private void FileListControl_DragEnter(object sender, DragEventArgs e)
        {
            if (IsReadOnly) return;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                ShowDropZone(true);
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] User 'nguyenbalam57' drag enter with files");
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void FileListControl_DragOver(object sender, DragEventArgs e)
        {
            if (IsReadOnly) return;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void FileListControl_DragLeave(object sender, DragEventArgs e)
        {
            if (IsReadOnly) return;
            ShowDropZone(false);
        }

        private void FileListControl_Drop(object sender, DragEventArgs e)
        {
            if (IsReadOnly) return;

            ShowDropZone(false);

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files != null && files.Length > 0)
                {
                    var filesToAdd = new List<string>();

                    foreach (string file in files)
                    {
                        if (File.Exists(file))
                        {
                            filesToAdd.Add(file);
                        }
                        else if (Directory.Exists(file))
                        {
                            try
                            {
                                var dirFiles = Directory.GetFiles(file, "*.*", SearchOption.TopDirectoryOnly);
                                filesToAdd.AddRange(dirFiles);
                            }
                            catch (Exception ex)
                            {
                                ShowError($"Lỗi khi đọc thư mục {file}: {ex.Message}", "❌");
                            }
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] User 'nguyenbalam57' dropped {filesToAdd.Count} files");
                    AddFilesInternal(filesToAdd);
                }
            }
        }

        private void ShowDropZone(bool show)
        {
            if (DropZone == null) return;

            if (show)
            {
                DropZone.Visibility = Visibility.Visible;
                var fadeIn = new DoubleAnimation(0, 0.8, TimeSpan.FromMilliseconds(200));
                DropZone.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            }
            else
            {
                var fadeOut = new DoubleAnimation(DropZone.Opacity, 0, TimeSpan.FromMilliseconds(200));
                fadeOut.Completed += (s, e) => DropZone.Visibility = Visibility.Collapsed;
                DropZone.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            }
        }

        #endregion

        #region File Management Methods

        private void BrowseFiles()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = FileFilter,
                    Multiselect = AllowMultiple,
                    Title = "Chọn file",
                    CheckFileExists = true
                };

                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] User 'nguyenbalam57' opening file dialog with filter: {FileFilter}");

                if (dialog.ShowDialog() == true)
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] User 'nguyenbalam57' selected {dialog.FileNames.Length} files via browse dialog");

                    if (dialog.FileNames.Length == 1)
                    {
                        FilePath = dialog.FileName;
                    }
                    else if (dialog.FileNames.Length > 1)
                    {
                        AddFilesInternal(dialog.FileNames);
                        FilePath = "";
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError($"Lỗi khi chọn file: {ex.Message}", "❌");
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] Error in BrowseFiles: {ex.Message}");
            }
        }

        private void AddCurrentFile()
        {
            if (!CanAddFile)
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] Cannot add file - CanAddFile: {CanAddFile}");
                return;
            }

            if (File.Exists(FilePath))
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] User 'nguyenbalam57' adding current file: {Path.GetFileName(FilePath)}");
                AddFilesInternal(new[] { FilePath });
                FilePath = "";
            }
            else
            {
                ShowError("File không tồn tại", "❌");
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] File not found: {FilePath}");
            }
        }

        private void AddFilesInternal(IEnumerable<string> files)
        {
            if (IsReadOnly)
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] Cannot add files - control is read-only");
                return;
            }

            int addedCount = 0;
            var errors = new List<string>();

            foreach (string file in files)
            {
                try
                {
                    if (FileList.Count >= MaxFiles)
                    {
                        errors.Add($"Đã đạt giới hạn {MaxFiles} file");
                        break;
                    }

                    if (FileList.Contains(file))
                    {
                        errors.Add($"File đã tồn tại: {Path.GetFileName(file)}");
                        continue;
                    }

                    if (!ValidateFile(file, out string validationError))
                    {
                        errors.Add(validationError);
                        continue;
                    }

                    FileList.Add(file);
                    addedCount++;

                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] User 'nguyenbalam57' added file: {Path.GetFileName(file)}");
                }
                catch (Exception ex)
                {
                    var errorMsg = $"Lỗi thêm file {Path.GetFileName(file)}: {ex.Message}";
                    errors.Add(errorMsg);
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] Error adding file: {errorMsg}");
                }
            }

            // Show result
            if (addedCount > 0)
            {
                ShowSuccess($"Đã thêm {addedCount} file", "✅");
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] User 'nguyenbalam57' successfully added {addedCount} files");
            }

            if (errors.Any())
            {
                var warningMsg = $"Có {errors.Count} lỗi: {string.Join(", ", errors.Take(3))}" +
                                (errors.Count > 3 ? "..." : "");
                ShowWarning(warningMsg, "⚠️");
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] User 'nguyenbalam57' encountered {errors.Count} errors while adding files");
            }
        }

        private void RemoveFile(string filePath)
        {
            if (IsReadOnly) return;

            if (FileList.Contains(filePath))
            {
                FileList.Remove(filePath);
                ShowSuccess("Đã xóa file", "🗑️");
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] User 'nguyenbalam57' removed file: {Path.GetFileName(filePath)}");
            }
        }

        private void RefreshFileListItems()
        {
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] User 'nguyenbalam57' refreshing file list items");

            FileListItems.Clear();

            if (FileList != null)
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] Processing {FileList.Count} files from FileList");

                foreach (var file in FileList)
                {
                    FileListItems.Add(new FileListItem(file));
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] Added file item: {Path.GetFileName(file)}");
                }
            }

            UpdateFileListDisplay();
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] File list refreshed - HasFiles: {HasFiles}, Count: {FileListItems.Count}");
        }

        #endregion

        #region Validation Methods

        private bool ValidateFile(string filePath, out string error)
        {
            error = "";

            if (!File.Exists(filePath))
            {
                error = $"File không tồn tại: {Path.GetFileName(filePath)}";
                return false;
            }

            // Check file size
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length > MaxFileSize)
            {
                error = $"File quá lớn: {Path.GetFileName(filePath)} ({FormatFileSize(fileInfo.Length)} > {FormatFileSize(MaxFileSize)})";
                return false;
            }

            // Check file filter
            if (!IsFileMatchFilter(filePath, FileFilter))
            {
                error = $"Loại file không được hỗ trợ: {Path.GetFileName(filePath)}";
                return false;
            }

            return true;
        }

        private bool IsFileMatchFilter(string filePath, string filter)
        {
            if (filter == "All files (*.*)|*.*" || string.IsNullOrEmpty(filter))
                return true;

            var extensions = new List<string>();
            var filterParts = filter.Split('|');

            for (int i = 1; i < filterParts.Length; i += 2)
            {
                var patterns = filterParts[i].Split(';');
                foreach (var pattern in patterns)
                {
                    if (pattern.Contains("*"))
                    {
                        var ext = pattern.Replace("*", "").Trim();
                        extensions.Add(ext);
                    }
                }
            }

            var fileExt = Path.GetExtension(filePath);
            return extensions.Any(ext => string.Equals(ext, fileExt, StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        #region Status Methods

        private void ShowSuccess(string message, string icon)
        {
            ShowStatus(message, icon, "#D4EDDA", "#155724", "#C3E6CB");
        }

        private void ShowError(string message, string icon)
        {
            ShowStatus(message, icon, "#F8D7DA", "#721C24", "#F5C6CB");
        }

        private void ShowWarning(string message, string icon)
        {
            ShowStatus(message, icon, "#FFF3CD", "#856404", "#FFEAA7");
        }

        private void ShowInfo(string message, string icon)
        {
            ShowStatus(message, icon, "#D1ECF1", "#0C5460", "#BEE5EB");
        }

        private void ShowStatus(string message, string icon, string bgColor, string fgColor, string borderColor)
        {
            _statusMessage = message;
            _statusIcon = icon;
            _statusBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgColor));
            _statusForeground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fgColor));
            _statusBorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(borderColor));

            // Update UI elements
            if (StatusBar != null)
                StatusBar.Visibility = Visibility.Visible;
            if (StatusBorder != null)
            {
                StatusBorder.Background = _statusBackground;
                StatusBorder.BorderBrush = _statusBorderBrush;
            }
            if (StatusIcon != null)
                StatusIcon.Text = icon;
            if (StatusText != null)
            {
                StatusText.Text = message;
                StatusText.Foreground = _statusForeground;
            }

            _statusTimer.Stop();
            _statusTimer.Start();

            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] User 'nguyenbalam57' - Status: {icon} {message}");
        }

        private void ClearStatus()
        {
            _statusMessage = "";
            _statusIcon = "";

            if (StatusBar != null)
                StatusBar.Visibility = Visibility.Collapsed;

            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] User 'nguyenbalam57' - Status cleared");
        }

        #endregion

        #region Utility Methods

        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int suffixIndex = 0;
            double size = bytes;

            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }

            return $"{size:F1} {suffixes[suffixIndex]}";
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Thêm file từ code
        /// </summary>
        public void AddFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] User 'nguyenbalam57' adding single file via API: {Path.GetFileName(filePath)}");
            AddFilesInternal(new[] { filePath });
        }

        /// <summary>
        /// Thêm nhiều file từ code
        /// </summary>
        public void AddFiles(string[] filePaths)
        {
            if (filePaths == null || !filePaths.Any()) return;

            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] User 'nguyenbalam57' adding {filePaths.Length} files via API");
            AddFilesInternal(filePaths);
        }

        /// <summary>
        /// Xóa tất cả file
        /// </summary>
        public void ClearFiles()
        {
            if (IsReadOnly) return;

            var count = FileList?.Count ?? 0;
            FileList?.Clear();
            ShowInfo("Đã xóa tất cả file", "🗑️");

            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] User 'nguyenbalam57' cleared {count} files");
        }

        /// <summary>
        /// Lấy danh sách file paths
        /// </summary>
        public string[] GetFilePaths()
        {
            var paths = FileList?.ToArray() ?? new string[0];
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] User 'nguyenbalam57' retrieved {paths.Length} file paths");
            return paths;
        }

        /// <summary>
        /// Thiết lập danh sách file từ mảng string
        /// </summary>
        public void SetFiles(string[] filePaths)
        {
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] User 'nguyenbalam57' setting {filePaths?.Length ?? 0} files via API");

            FileList?.Clear();
            if (filePaths != null)
            {
                foreach (var file in filePaths.Where(f => !string.IsNullOrWhiteSpace(f)))
                {
                    FileList?.Add(file);
                }
            }
        }

        /// <summary>
        /// Validate tất cả file trong danh sách
        /// </summary>
        public bool ValidateAllFiles(out List<string> errors)
        {
            errors = new List<string>();

            if (FileList == null) return true;

            foreach (var file in FileList)
            {
                if (!ValidateFile(file, out string error))
                {
                    errors.Add(error);
                }
            }

            var isValid = !errors.Any();
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] User 'nguyenbalam57' validated {FileList.Count} files - Valid: {isValid}, Errors: {errors.Count}");

            return isValid;
        }

        #endregion

        #region Static Commands for InputBindings

        public static readonly ICommand BrowseCommand = new RelayCommand<object>(_ =>
        {
            var control = Application.Current.Windows.OfType<Window>()
                .SelectMany(w => FindVisualChildren<FileListControl>(w))
                .FirstOrDefault(c => c.IsKeyboardFocusWithin);
            control?.BrowseFiles();
        });

        public static readonly ICommand ClearCommand = new RelayCommand<object>(_ =>
        {
            var control = Application.Current.Windows.OfType<Window>()
                .SelectMany(w => FindVisualChildren<FileListControl>(w))
                .FirstOrDefault(c => c.IsKeyboardFocusWithin);
            control?.ClearFiles();
        });

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Item trong danh sách file với thông tin chi tiết
    /// </summary>
    public class FileListItem
    {
        public string FullPath { get; }
        public string FileName { get; }
        public string Extension { get; }
        public string FileSize { get; }
        public DateTime LastModified { get; }
        public bool IsValid { get; }

        public FileListItem(string fullPath)
        {
            FullPath = fullPath ?? "";
            FileName = Path.GetFileName(fullPath ?? "");
            Extension = Path.GetExtension(fullPath ?? "");

            try
            {
                if (File.Exists(fullPath))
                {
                    var fileInfo = new FileInfo(fullPath);
                    FileSize = FormatFileSize(fileInfo.Length);
                    LastModified = fileInfo.LastWriteTime;
                    IsValid = true;
                }
                else
                {
                    FileSize = "N/A";
                    LastModified = DateTime.MinValue;
                    IsValid = false;
                }
            }
            catch
            {
                FileSize = "N/A";
                LastModified = DateTime.MinValue;
                IsValid = false;
            }
        }

        private static string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int suffixIndex = 0;
            double size = bytes;

            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }

            return $"{size:F1} {suffixes[suffixIndex]}";
        }
    }
}