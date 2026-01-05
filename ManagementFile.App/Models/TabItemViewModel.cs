using ManagementFile.App.ViewModels;
using System;
using System.Windows.Input;

namespace ManagementFile.App.Models
{
    public class TabItemViewModel : BaseViewModel
    {
        private string _title;
        private object _content;
        private DateTime _createdTime;
        private bool _isCloseable = true;
        private bool _isDraggable = true;
        private bool _isPinned = false;
        private bool _isModified = false;
        private string _iconGlyph;
        private string _tooltip;

        public TabItemViewModel()
        {
            // Khởi tạo Commands
            CloseTabCommand = new RelayCommand(ExecuteCloseTab, CanCloseTab);
            PinTabCommand = new RelayCommand(ExecutePinTab);
        }

        #region Properties

        /// <summary>
        /// Tiêu đề của tab
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>
        /// Nội dung hiển thị trong tab
        /// </summary>
        public object Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }

        /// <summary>
        /// Thời gian tạo tab
        /// </summary>
        public DateTime CreatedTime
        {
            get => _createdTime;
            set => SetProperty(ref _createdTime, value);
        }

        /// <summary>
        /// Tab có thể đóng được không
        /// </summary>
        public bool IsCloseable
        {
            get => _isCloseable;
            set
            {
                if (SetProperty(ref _isCloseable, value))
                {
                    (CloseTabCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Tab có thể kéo được không
        /// </summary>
        public bool IsDraggable
        {
            get => _isDraggable;
            set => SetProperty(ref _isDraggable, value);
        }

        /// <summary>
        /// Tab có được ghim không (pinned tabs không thể đóng)
        /// </summary>
        public bool IsPinned
        {
            get => _isPinned;
            set
            {
                if (SetProperty(ref _isPinned, value))
                {
                    IsCloseable = !value; // Pinned tab không thể đóng
                    OnPropertyChanged(nameof(PinIconGlyph));
                }
            }
        }

        /// <summary>
        /// Tab có thay đổi chưa lưu không
        /// </summary>
        public bool IsModified
        {
            get => _isModified;
            set
            {
                if (SetProperty(ref _isModified, value))
                {
                    UpdateTitle();
                }
            }
        }

        /// <summary>
        /// Icon glyph cho tab (Unicode character)
        /// </summary>
        public string IconGlyph
        {
            get => _iconGlyph;
            set => SetProperty(ref _iconGlyph, value);
        }

        /// <summary>
        /// Tooltip hiển thị khi hover
        /// </summary>
        public string Tooltip
        {
            get => _tooltip ?? $"{Title}\nTạo lúc: {CreatedTime:dd/MM/yyyy HH:mm:ss}";
            set => SetProperty(ref _tooltip, value);
        }

        /// <summary>
        /// Icon cho pin/unpin
        /// </summary>
        public string PinIconGlyph => IsPinned ? "📌" : "📍";

        #endregion

        #region Commands

        public ICommand CloseTabCommand { get; }
        public ICommand PinTabCommand { get; }

        /// <summary>
        /// Event khi tab cần đóng
        /// </summary>
        public event EventHandler<TabItemViewModel> CloseRequested;

        #endregion

        #region Command Implementations

        private bool CanCloseTab(object parameter)
        {
            return IsCloseable && !IsPinned;
        }

        private void ExecuteCloseTab(object parameter)
        {
            CloseRequested?.Invoke(this, this);
        }

        private bool CanPinTab(object parameter)
        {
            return IsCloseable;
        }

        private void ExecutePinTab(object parameter)
        {
            IsPinned = !IsPinned;
        }

        #endregion

        #region Helper Methods

        private void UpdateTitle()
        {
            // Cập nhật title với dấu * nếu modified
            if (IsModified && !Title.EndsWith("*"))
            {
                Title = Title + " *";
            }
            else if (!IsModified && Title.EndsWith("*"))
            {
                Title = Title.TrimEnd(' ', '*');
            }
        }

        /// <summary>
        /// Set tab là tab chính (không thể đóng, không thể kéo)
        /// </summary>
        public void SetAsMainTab()
        {
            IsCloseable = false;
            IsDraggable = false;
            IsPinned = true;
        }

        #endregion
    }
}