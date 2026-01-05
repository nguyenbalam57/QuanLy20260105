using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
    /// Interaction logic for TagInputControl.xaml
    /// </summary>
    public partial class TagInputControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Dependency Properties

        public static readonly DependencyProperty TagsProperty =
            DependencyProperty.Register("Tags", typeof(ObservableCollection<string>),
                typeof(TagInputControl),
                new FrameworkPropertyMetadata(new ObservableCollection<string>(),
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTagsChanged));

        private static void OnTagsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TagInputControl)d;
            if (e.NewValue != null && !(e.NewValue is ObservableCollection<string>))
            {
                control.Tags = new ObservableCollection<string>();
            }
            control.OnPropertyChanged(nameof(Tags));
            control.OnPropertyChanged(nameof(HasTags));
        }

        public ObservableCollection<string> Tags
        {
            get => (ObservableCollection<string>)GetValue(TagsProperty);
            set => SetValue(TagsProperty, value);
        }

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(TagInputControl),
                new PropertyMetadata(false));

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register("Placeholder", typeof(string), typeof(TagInputControl),
                new PropertyMetadata("Nhập tag và nhấn Enter..."));

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public static readonly DependencyProperty MaxTagsProperty =
            DependencyProperty.Register("MaxTags", typeof(int), typeof(TagInputControl),
                new PropertyMetadata(10));

        public int MaxTags
        {
            get => (int)GetValue(MaxTagsProperty);
            set => SetValue(MaxTagsProperty, value);
        }

        public static readonly DependencyProperty AllowDuplicatesProperty =
            DependencyProperty.Register("AllowDuplicates", typeof(bool), typeof(TagInputControl),
                new PropertyMetadata(false));

        public bool AllowDuplicates
        {
            get => (bool)GetValue(AllowDuplicatesProperty);
            set => SetValue(AllowDuplicatesProperty, value);
        }

        #region Observable Properties

        private string _tagText = "";
        public string TagText
        {
            get => _tagText;
            set
            {
                if (_tagText != value)
                {
                    _tagText = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanAddTag));
                    OnPropertyChanged(nameof(HasTagText));
                }
            }
        }

        public bool HasTagText => string.IsNullOrWhiteSpace(TagText);

        public bool CanAddTag => !string.IsNullOrWhiteSpace(TagText) &&
                                 !IsReadOnly &&
                                 (Tags?.Count ?? 0) < MaxTags &&
                                 (AllowDuplicates || !Tags?.Contains(TagText.Trim(), StringComparer.OrdinalIgnoreCase) == true);

        public bool HasTags => Tags?.Count > 0;

        #endregion

        #endregion
        public TagInputControl()
        {
            InitializeComponent();

            DataContext = this;

            // Initialize Tags if null
            if (Tags == null)
            {
                Tags = new ObservableCollection<string>();
            }
        }

        #region Event Handlers

        private void TagInputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddCurrentTag();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                TagText = "";
                e.Handled = true;
            }
            else if (e.Key == Key.Back && string.IsNullOrEmpty(TagText) && Tags.Count > 0)
            {
                // Remove last tag when backspace is pressed on empty input
                RemoveTag(Tags.Last());
                e.Handled = true;
            }
        }

        private void TagInputTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            // Select all text when focused
            TagInputTextBox.SelectAll();
        }

        private void AddTagButton_Click(object sender, RoutedEventArgs e)
        {
            AddCurrentTag();
        }

        private void RemoveTagButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                RemoveTag(tag);
            }
        }

        #endregion

        #region Private Methods

        private void AddCurrentTag()
        {
            if (!CanAddTag) return;

            var tag = TagText.Trim();
            if (string.IsNullOrWhiteSpace(tag)) return;

            // Validate tag format
            if (!IsValidTag(tag))
            {
                ShowTagError("Tag không hợp lệ. Tag chỉ được chứa chữ cái, số, dấu gạch ngang và gạch dưới.");
                return;
            }

            // Check for duplicates (case-insensitive)
            if (!AllowDuplicates && Tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
            {
                ShowTagError("Tag này đã tồn tại.");
                return;
            }

            // Check max tags limit
            if (Tags.Count >= MaxTags)
            {
                ShowTagError($"Không thể thêm quá {MaxTags} tags.");
                return;
            }

            // Add tag
            Tags.Add(tag);
            TagText = "";
            OnPropertyChanged(nameof(HasTags));

            // Focus back to input
            TagInputTextBox.Focus();
        }

        private void RemoveTag(string tag)
        {
            if (Tags.Contains(tag))
            {
                Tags.Remove(tag);
                OnPropertyChanged(nameof(HasTags));
            }
        }

        private bool IsValidTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return false;
            if (tag.Length > 50) return false; // Max tag length

            // Allow alphanumeric, hyphens, underscores, and Vietnamese characters
            return System.Text.RegularExpressions.Regex.IsMatch(tag, @"^[\w\-\u00C0-\u024F\u1E00-\u1EFF]+$");
        }

        private void ShowTagError(string message)
        {
            // Simple tooltip or status message - could be enhanced
            ToolTip = message;
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            timer.Tick += (s, e) =>
            {
                ToolTip = null;
                timer.Stop();
            };
            timer.Start();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Thêm tag từ code
        /// </summary>
        public void AddTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return;

            tag = tag.Trim();
            if (!IsValidTag(tag)) return;

            if (!AllowDuplicates && Tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
                return;

            if (Tags.Count >= MaxTags) return;

            Tags.Add(tag);
            OnPropertyChanged(nameof(HasTags));
        }

        /// <summary>
        /// Xóa tất cả tags
        /// </summary>
        public void ClearTags()
        {
            Tags.Clear();
            OnPropertyChanged(nameof(HasTags));
        }

        /// <summary>
        /// Thiết lập danh sách tags từ mảng string
        /// </summary>
        public void SetTags(string[] tags)
        {
            Tags.Clear();
            if (tags != null)
            {
                foreach (var tag in tags.Take(MaxTags))
                {
                    if (IsValidTag(tag))
                    {
                        Tags.Add(tag.Trim());
                    }
                }
            }
            OnPropertyChanged(nameof(HasTags));
        }

        /// <summary>
        /// Lấy danh sách tags dưới dạng mảng
        /// </summary>
        public string[] GetTags()
        {
            return Tags?.ToArray() ?? new string[0];
        }

        #endregion
    }
}
