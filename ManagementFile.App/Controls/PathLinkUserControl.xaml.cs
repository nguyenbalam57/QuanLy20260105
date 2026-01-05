using ManagementFile.App.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace ManagementFile.App.Controls
{
    /// <summary>
    /// UserControl để hiển thị text với các đường dẫn có thể click
    /// </summary>
    public partial class PathLinkUserControl : UserControl
    {
        private List<PathInfo> _extractedPaths;

        #region Dependency Properties

        /// <summary>
        /// Dependency Property cho text mô tả cần phân tích
        /// </summary>
        public static readonly DependencyProperty TextDescriptionProperty =
            DependencyProperty.Register(
                "TextDescription",
                typeof(string),
                typeof(PathLinkUserControl),
                new PropertyMetadata(string.Empty, OnTextDescriptionChanged));

        /// <summary>
        /// Dependency Property cho màu nền header
        /// </summary>
        public static readonly DependencyProperty HeaderBackgroundProperty =
            DependencyProperty.Register(
                "HeaderBackground",
                typeof(Brush),
                typeof(PathLinkUserControl),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(245, 245, 245))));

        /// <summary>
        /// Dependency Property cho màu viền header
        /// </summary>
        public static readonly DependencyProperty HeaderBorderBrushProperty =
            DependencyProperty.Register(
                "HeaderBorderBrush",
                typeof(Brush),
                typeof(PathLinkUserControl),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(221, 221, 221))));

        /// <summary>
        /// Dependency Property cho màu link hợp lệ
        /// </summary>
        public static readonly DependencyProperty ValidLinkColorProperty =
            DependencyProperty.Register(
                "ValidLinkColor",
                typeof(Brush),
                typeof(PathLinkUserControl),
                new PropertyMetadata(Brushes.Blue));

        /// <summary>
        /// Dependency Property cho màu link không hợp lệ
        /// </summary>
        public static readonly DependencyProperty InvalidLinkColorProperty =
            DependencyProperty.Register(
                "InvalidLinkColor",
                typeof(Brush),
                typeof(PathLinkUserControl),
                new PropertyMetadata(Brushes.Gray));

        /// <summary>
        /// Dependency Property cho màu link file
        /// </summary>
        public static readonly DependencyProperty FileLinkColorProperty =
            DependencyProperty.Register(
                "FileLinkColor",
                typeof(Brush),
                typeof(PathLinkUserControl),
                new PropertyMetadata(Brushes.Green));

        /// <summary>
        /// Dependency Property cho việc hiển thị danh sách đường dẫn
        /// </summary>
        public static readonly DependencyProperty ShowPathListProperty =
            DependencyProperty.Register(
                "ShowPathList",
                typeof(bool),
                typeof(PathLinkUserControl),
                new PropertyMetadata(true, OnShowPathListChanged));

        /// <summary>
        /// Dependency Property cho font size của text
        /// </summary>
        public static readonly DependencyProperty TextFontSizeProperty =
            DependencyProperty.Register(
                "TextFontSize",
                typeof(double),
                typeof(PathLinkUserControl),
                new PropertyMetadata(14.0));

        /// <summary>
        /// Dependency Property cho việc tự động mở đường dẫn
        /// </summary>
        public static readonly DependencyProperty AutoOpenProperty =
            DependencyProperty.Register(
                "AutoOpen",
                typeof(bool),
                typeof(PathLinkUserControl),
                new PropertyMetadata(true));

        #endregion

        #region CLR Properties

        /// <summary>
        /// Text mô tả cần phân tích
        /// </summary>
        public string TextDescription
        {
            get => (string)GetValue(TextDescriptionProperty);
            set => SetValue(TextDescriptionProperty, value);
        }

        /// <summary>
        /// Màu nền header
        /// </summary>
        public Brush HeaderBackground
        {
            get => (Brush)GetValue(HeaderBackgroundProperty);
            set => SetValue(HeaderBackgroundProperty, value);
        }

        /// <summary>
        /// Màu viền header
        /// </summary>
        public Brush HeaderBorderBrush
        {
            get => (Brush)GetValue(HeaderBorderBrushProperty);
            set => SetValue(HeaderBorderBrushProperty, value);
        }

        /// <summary>
        /// Màu link hợp lệ
        /// </summary>
        public Brush ValidLinkColor
        {
            get => (Brush)GetValue(ValidLinkColorProperty);
            set => SetValue(ValidLinkColorProperty, value);
        }

        /// <summary>
        /// Màu link không hợp lệ
        /// </summary>
        public Brush InvalidLinkColor
        {
            get => (Brush)GetValue(InvalidLinkColorProperty);
            set => SetValue(InvalidLinkColorProperty, value);
        }

        /// <summary>
        /// Màu link file
        /// </summary>
        public Brush FileLinkColor
        {
            get => (Brush)GetValue(FileLinkColorProperty);
            set => SetValue(FileLinkColorProperty, value);
        }

        /// <summary>
        /// Hiển thị danh sách đường dẫn
        /// </summary>
        public bool ShowPathList
        {
            get => (bool)GetValue(ShowPathListProperty);
            set => SetValue(ShowPathListProperty, value);
        }

        /// <summary>
        /// Font size của text
        /// </summary>
        public double TextFontSize
        {
            get => (double)GetValue(TextFontSizeProperty);
            set => SetValue(TextFontSizeProperty, value);
        }

        /// <summary>
        /// Tự động mở đường dẫn
        /// </summary>
        public bool AutoOpen
        {
            get => (bool)GetValue(AutoOpenProperty);
            set => SetValue(AutoOpenProperty, value);
        }

        #endregion

        #region Property Changed Callbacks

        private static void OnTextDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PathLinkUserControl control && e.NewValue is string newText)
            {
                control.SetText(newText);
            }
        }

        private static void OnShowPathListChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PathLinkUserControl control)
            {
                control.RefreshDisplay();
            }
        }

        #endregion

        #region Routed Events

        /// <summary>
        /// Routed Event khi đường dẫn được click
        /// </summary>
        public static readonly RoutedEvent PathClickedEvent =
            EventManager.RegisterRoutedEvent(
                "PathClicked",
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(PathLinkUserControl));

        public event RoutedEventHandler PathClicked
        {
            add { AddHandler(PathClickedEvent, value); }
            remove { RemoveHandler(PathClickedEvent, value); }
        }

        /// <summary>
        /// Routed Event khi đường dẫn được mở
        /// </summary>
        public static readonly RoutedEvent PathOpenedEvent =
            EventManager.RegisterRoutedEvent(
                "PathOpened",
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(PathLinkUserControl));

        public event RoutedEventHandler PathOpened
        {
            add { AddHandler(PathOpenedEvent, value); }
            remove { RemoveHandler(PathOpenedEvent, value); }
        }

        /// <summary>
        /// Routed Event khi có lỗi mở đường dẫn
        /// </summary>
        public static readonly RoutedEvent PathOpenErrorEvent =
            EventManager.RegisterRoutedEvent(
                "PathOpenError",
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(PathLinkUserControl));

        public event RoutedEventHandler PathOpenError
        {
            add { AddHandler(PathOpenErrorEvent, value); }
            remove { RemoveHandler(PathOpenErrorEvent, value); }
        }

        #endregion

        #region Constructor

        public PathLinkUserControl()
        {
            InitializeComponent();

            _extractedPaths = new List<PathInfo>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Thiết lập text và phân tích các đường dẫn
        /// </summary>
        public void SetText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                contentPanel.Children.Clear();
                contentPanel.Children.Add(CreateEmptyMessage());
                txtPathCount.Text = "0 đường dẫn được tìm thấy";
                return;
            }

            // Trích xuất các đường dẫn
            _extractedPaths = PathExtractor.ExtractPathsWithDetails(text);

            // Cập nhật số lượng đường dẫn
            UpdatePathCount();

            // Hiển thị nội dung
            RenderContent(text);
        }

        /// <summary>
        /// Làm mới hiển thị
        /// </summary>
        public void RefreshDisplay()
        {
            if (!string.IsNullOrWhiteSpace(TextDescription))
            {
                SetText(TextDescription);
            }
        }

        /// <summary>
        /// Lấy danh sách các đường dẫn đã trích xuất
        /// </summary>
        public List<PathInfo> GetExtractedPaths()
        {
            return new List<PathInfo>(_extractedPaths);
        }

        /// <summary>
        /// Xóa nội dung
        /// </summary>
        public void Clear()
        {
            TextDescription = string.Empty;
            _extractedPaths.Clear();
            contentPanel.Children.Clear();
            txtPathCount.Text = "0 đường dẫn được tìm thấy";
        }

        #endregion

        #region Private Methods

        private void UpdatePathCount()
        {
            int validCount = _extractedPaths.Count(p => p.IsValid);
            int totalCount = _extractedPaths.Count;

            if (totalCount == 0)
            {
                txtPathCount.Text = "0 đường dẫn được tìm thấy";
            }
            else if (validCount == totalCount)
            {
                txtPathCount.Text = $"{totalCount} đường dẫn được tìm thấy (Tất cả hợp lệ)";
            }
            else
            {
                txtPathCount.Text = $"{totalCount} đường dẫn được tìm thấy ({validCount} hợp lệ, {totalCount - validCount} không hợp lệ)";
            }
        }

        /// <summary>
        /// Render nội dung với các đường dẫn có thể click
        /// </summary>
        private void RenderContent(string text)
        {
            contentPanel.Children.Clear();

            if (_extractedPaths.Count == 0)
            {
                // Không có đường dẫn, hiển thị text thông thường
                var textBlock = new TextBlock
                {
                    Text = text,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = TextFontSize,
                    LineHeight = 22
                };
                contentPanel.Children.Add(textBlock);
                return;
            }

            // Tạo RichTextBox để hiển thị text với link
            var richTextBox = new RichTextBox
            {
                IsReadOnly = true,
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                FontSize = TextFontSize,
                Padding = new Thickness(0)
            };

            var document = new FlowDocument();
            var paragraph = new Paragraph();

            // Sắp xếp các đường dẫn theo vị trí xuất hiện
            var sortedPaths = _extractedPaths
                .Select(p => new { Path = p, Index = text.IndexOf(p.OriginalPath) })
                .Where(x => x.Index >= 0)
                .OrderBy(x => x.Index)
                .ToList();

            int lastIndex = 0;

            foreach (var item in sortedPaths)
            {
                // Thêm text trước đường dẫn
                if (item.Index > lastIndex)
                {
                    string beforeText = text.Substring(lastIndex, item.Index - lastIndex);
                    paragraph.Inlines.Add(new Run(beforeText));
                }

                // Thêm hyperlink cho đường dẫn
                var hyperlink = CreateHyperlink(item.Path);
                paragraph.Inlines.Add(hyperlink);

                lastIndex = item.Index + item.Path.OriginalPath.Length;
            }

            // Thêm text còn lại
            if (lastIndex < text.Length)
            {
                string remainingText = text.Substring(lastIndex);
                paragraph.Inlines.Add(new Run(remainingText));
            }

            document.Blocks.Add(paragraph);
            richTextBox.Document = document;
            contentPanel.Children.Add(richTextBox);

            // Thêm danh sách đường dẫn nếu được bật
            if (ShowPathList)
            {
                contentPanel.Children.Add(CreatePathListPanel());
            }
        }

        /// <summary>
        /// Tạo hyperlink cho đường dẫn
        /// </summary>
        private Hyperlink CreateHyperlink(PathInfo pathInfo)
        {
            var hyperlink = new Hyperlink(new Run(pathInfo.OriginalPath))
            {
                Tag = pathInfo,
                ToolTip = CreateTooltip(pathInfo)
            };

            // Đổi màu theo trạng thái
            if (pathInfo.IsValid)
            {
                if (pathInfo.IsDirectory)
                    hyperlink.Foreground = ValidLinkColor;
                else if (pathInfo.IsFile)
                    hyperlink.Foreground = FileLinkColor;

                hyperlink.Cursor = System.Windows.Input.Cursors.Hand;
            }
            else
            {
                hyperlink.Foreground = InvalidLinkColor;
                hyperlink.TextDecorations = TextDecorations.Strikethrough;
            }

            hyperlink.Click += Hyperlink_Click;

            return hyperlink;
        }

        /// <summary>
        /// Xử lý sự kiện click vào link
        /// </summary>
        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Hyperlink hyperlink && hyperlink.Tag is PathInfo pathInfo)
            {
                // Raise PathClicked event
                RaiseEvent(new RoutedEventArgs(PathClickedEvent, pathInfo));

                if (AutoOpen)
                {
                    OpenPath(pathInfo);
                }
            }
        }

        /// <summary>
        /// Mở đường dẫn (file hoặc thư mục)
        /// </summary>
        private void OpenPath(PathInfo pathInfo)
        {
            if (!pathInfo.IsValid)
            {
                var errorArgs = new PathErrorEventArgs(PathOpenErrorEvent, pathInfo,
                    $"Đường dẫn không tồn tại hoặc không thể truy cập:\n{pathInfo.OriginalPath}");
                RaiseEvent(errorArgs);

                if (!errorArgs.Handled)
                {
                    MessageBox.Show(
                        errorArgs.ErrorMessage,
                        "Lỗi",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                return;
            }

            try
            {
                if (pathInfo.IsDirectory)
                {
                    // Mở thư mục trong File Explorer
                    Process.Start("explorer.exe", pathInfo.OriginalPath);
                }
                else if (pathInfo.IsFile)
                {
                    // Mở file với ứng dụng mặc định
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = pathInfo.OriginalPath,
                        UseShellExecute = true
                    });
                }

                // Raise PathOpened event
                RaiseEvent(new RoutedEventArgs(PathOpenedEvent, pathInfo));
            }
            catch (Exception ex)
            {
                var errorArgs = new PathErrorEventArgs(PathOpenErrorEvent, pathInfo,
                    $"Không thể mở đường dẫn:\n{ex.Message}");
                RaiseEvent(errorArgs);

                if (!errorArgs.Handled)
                {
                    MessageBox.Show(
                        errorArgs.ErrorMessage,
                        "Lỗi",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Tạo tooltip cho đường dẫn
        /// </summary>
        private ToolTip CreateTooltip(PathInfo pathInfo)
        {
            var stackPanel = new StackPanel();

            stackPanel.Children.Add(new TextBlock
            {
                Text = pathInfo.IsValid ? "✓ Đường dẫn hợp lệ" : "✗ Đường dẫn không tồn tại",
                FontWeight = FontWeights.Bold,
                Foreground = pathInfo.IsValid ? Brushes.Green : Brushes.Red
            });

            if (pathInfo.IsValid)
            {
                stackPanel.Children.Add(new TextBlock
                {
                    Text = $"Loại: {(pathInfo.IsDirectory ? "📁 Thư mục" : "📄 Tệp tin")}",
                    Margin = new Thickness(0, 5, 0, 0)
                });

                stackPanel.Children.Add(new TextBlock
                {
                    Text = $"Tên: {pathInfo.Name}",
                    Margin = new Thickness(0, 2, 0, 0)
                });

                if (AutoOpen)
                {
                    stackPanel.Children.Add(new TextBlock
                    {
                        Text = "Click để mở",
                        Margin = new Thickness(0, 5, 0, 0),
                        FontStyle = FontStyles.Italic,
                        Foreground = Brushes.Gray
                    });
                }
            }

            return new ToolTip { Content = stackPanel };
        }

        /// <summary>
        /// Tạo panel danh sách đường dẫn
        /// </summary>
        private Border CreatePathListPanel()
        {
            var border = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(221, 221, 221)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 20, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(249, 249, 249))
            };

            var stackPanel = new StackPanel();

            // Header
            stackPanel.Children.Add(new TextBlock
            {
                Text = "📁 Danh sách đường dẫn",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 10)
            });

            // Danh sách đường dẫn
            foreach (var pathInfo in _extractedPaths)
            {
                stackPanel.Children.Add(CreatePathItem(pathInfo));
            }

            border.Child = stackPanel;
            return border;
        }

        /// <summary>
        /// Tạo item cho mỗi đường dẫn
        /// </summary>
        private Border CreatePathItem(PathInfo pathInfo)
        {
            var border = new Border
            {
                Background = Brushes.White,
                BorderBrush = pathInfo.IsValid ? Brushes.LightGray : Brushes.LightCoral,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 5, 0, 5),
                Cursor = pathInfo.IsValid ? System.Windows.Input.Cursors.Hand : System.Windows.Input.Cursors.Arrow
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Icon
            var icon = new TextBlock
            {
                Text = pathInfo.IsDirectory ? "📁" : pathInfo.IsFile ? "📄" : "❓",
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(icon, 0);
            grid.Children.Add(icon);

            // Path info
            var infoPanel = new StackPanel { Margin = new Thickness(5, 0, 0, 0) };

            var pathText = new TextBlock
            {
                Text = pathInfo.OriginalPath,
                FontWeight = FontWeights.Medium,
                TextWrapping = TextWrapping.Wrap
            };
            infoPanel.Children.Add(pathText);

            if (pathInfo.IsValid)
            {
                var detailText = new TextBlock
                {
                    Text = $"{(pathInfo.IsDirectory ? "Thư mục" : "Tệp tin")} • {pathInfo.Name}",
                    FontSize = 11,
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(0, 2, 0, 0)
                };
                infoPanel.Children.Add(detailText);
            }
            else
            {
                var errorText = new TextBlock
                {
                    Text = "Không tồn tại",
                    FontSize = 11,
                    Foreground = Brushes.Red,
                    Margin = new Thickness(0, 2, 0, 0)
                };
                infoPanel.Children.Add(errorText);
            }

            Grid.SetColumn(infoPanel, 1);
            grid.Children.Add(infoPanel);

            // Button
            if (pathInfo.IsValid)
            {
                var button = new Button
                {
                    Content = "Mở",
                    Padding = new Thickness(15, 5, 15, 5),
                    Background = new SolidColorBrush(Color.FromRgb(0, 120, 212)),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                button.Click += (s, e) => OpenPath(pathInfo);
                Grid.SetColumn(button, 2);
                grid.Children.Add(button);
            }

            border.Child = grid;

            // Thêm sự kiện click vào border
            if (pathInfo.IsValid)
            {
                border.MouseLeftButtonUp += (s, e) => OpenPath(pathInfo);
            }

            return border;
        }

        /// <summary>
        /// Tạo thông báo rỗng
        /// </summary>
        private Border CreateEmptyMessage()
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(249, 249, 249)),
                Padding = new Thickness(20),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var stackPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };

            stackPanel.Children.Add(new TextBlock
            {
                Text = "📭",
                FontSize = 48,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            stackPanel.Children.Add(new TextBlock
            {
                Text = "Không có nội dung để hiển thị",
                FontSize = 14,
                Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            });

            border.Child = stackPanel;
            return border;
        }

        #endregion
    }

    #region Custom EventArgs

    /// <summary>
    /// EventArgs cho sự kiện lỗi mở đường dẫn
    /// </summary>
    public class PathErrorEventArgs : RoutedEventArgs
    {
        public PathInfo PathInfo { get; }
        public string ErrorMessage { get; }

        public PathErrorEventArgs(RoutedEvent routedEvent, PathInfo pathInfo, string errorMessage)
            : base(routedEvent)
        {
            PathInfo = pathInfo;
            ErrorMessage = errorMessage;
        }
    }

    #endregion
}
