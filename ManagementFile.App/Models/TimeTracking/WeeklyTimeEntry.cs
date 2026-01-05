using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ManagementFile.App.Models.TimeTracking
{
    /// <summary>
    /// Model đại diện cho 1 dòng trong timesheet tuần
    /// Chứa thời gian làm việc của 1 task trong 7 ngày
    /// </summary>
    public class WeeklyTimeEntry : INotifyPropertyChanged
    {
        private int _taskId;
        private string _taskTitle;
        private string _taskCode;
        private decimal _hourlyRate;
        private bool _isBillable;

        // 7 ngày trong tuần (0=Monday, 6=Sunday)
        private decimal[] _hours = new decimal[7];
        private string[] _notes = new string[7];
        private bool[] _hasWarning = new bool[7];
        private string[] _warningMessages = new string[7];

        public int TaskId
        {
            get => _taskId;
            set => SetProperty(ref _taskId, value);
        }

        public string TaskTitle
        {
            get => _taskTitle;
            set => SetProperty(ref _taskTitle, value);
        }

        public string TaskCode
        {
            get => _taskCode;
            set => SetProperty(ref _taskCode, value);
        }

        public decimal HourlyRate
        {
            get => _hourlyRate;
            set
            {
                if (SetProperty(ref _hourlyRate, value))
                {
                }
            }
        }
        // ✅ Expose individual day properties cho binding
        public decimal Hours0
        {
            get => _hours[0];
            set
            {
                if (_hours[0] != value)
                {
                    _hours[0] = value;
                    OnPropertyChanged(nameof(Hours0));
                    OnPropertyChanged(nameof(TotalHours));

                    System.Diagnostics.Debug.WriteLine($"Hours0 set to {value}");
                }
            }
        }

        public decimal Hours1
        {
            get => _hours[1];
            set
            {
                if (_hours[1] != value)
                {
                    _hours[1] = value;
                    OnPropertyChanged(nameof(Hours1));
                    OnPropertyChanged(nameof(TotalHours));
                }
            }
        }

        public decimal Hours2
        {
            get => _hours[2];
            set
            {
                if (_hours[2] != value)
                {
                    _hours[2] = value;
                    OnPropertyChanged(nameof(Hours2));
                    OnPropertyChanged(nameof(TotalHours));
                }
            }
        }

        public decimal Hours3
        {
            get => _hours[3];
            set
            {
                if (_hours[3] != value)
                {
                    _hours[3] = value;
                    OnPropertyChanged(nameof(Hours3));
                    OnPropertyChanged(nameof(TotalHours));
                }
            }
        }

        public decimal Hours4
        {
            get => _hours[4];
            set
            {
                if (_hours[4] != value)
                {
                    _hours[4] = value;
                    OnPropertyChanged(nameof(Hours4));
                    OnPropertyChanged(nameof(TotalHours));
                }
            }
        }

        public decimal Hours5
        {
            get => _hours[5];
            set
            {
                if (_hours[5] != value)
                {
                    _hours[5] = value;
                    OnPropertyChanged(nameof(Hours5));
                    OnPropertyChanged(nameof(TotalHours));
                }
            }
        }

        public decimal Hours6
        {
            get => _hours[6];
            set
            {
                if (_hours[6] != value)
                {
                    _hours[6] = value;
                    OnPropertyChanged(nameof(Hours6));
                    OnPropertyChanged(nameof(TotalHours));
                }
            }
        }

        // ✅ Warning properties
        public bool HasWarning0 => _hasWarning[0];
        public bool HasWarning1 => _hasWarning[1];
        public bool HasWarning2 => _hasWarning[2];
        public bool HasWarning3 => _hasWarning[3];
        public bool HasWarning4 => _hasWarning[4];
        public bool HasWarning5 => _hasWarning[5];
        public bool HasWarning6 => _hasWarning[6];

        public string WarningMessage0 => _warningMessages[0];
        public string WarningMessage1 => _warningMessages[1];
        public string WarningMessage2 => _warningMessages[2];
        public string WarningMessage3 => _warningMessages[3];
        public string WarningMessage4 => _warningMessages[4];
        public string WarningMessage5 => _warningMessages[5];
        public string WarningMessage6 => _warningMessages[6];


        /// <summary>
        /// Lấy/set số giờ cho ngày cụ thể (0=Monday, 6=Sunday)
        /// </summary>
        public decimal GetHours(int dayIndex)
        {
            ValidateDayIndex(dayIndex);
            return _hours[dayIndex];
        }

        public void SetHours(int dayIndex, decimal hours)
        {
            ValidateDayIndex(dayIndex);
            if (_hours[dayIndex] != hours)
            {
                _hours[dayIndex] = hours;
                OnPropertyChanged($"Hours{dayIndex}");
                OnPropertyChanged(nameof(TotalHours));
            }
        }

        public string GetNote(int dayIndex)
        {
            ValidateDayIndex(dayIndex);
            return _notes[dayIndex];
        }

        public void SetNote(int dayIndex, string note)
        {
            ValidateDayIndex(dayIndex);
            _notes[dayIndex] = note;
            OnPropertyChanged($"Note{dayIndex}");
        }

        public bool GetHasWarning(int dayIndex)
        {
            ValidateDayIndex(dayIndex);
            return _hasWarning[dayIndex];
        }

        public void SetWarning(int dayIndex, bool hasWarning, string message = null)
        {
            ValidateDayIndex(dayIndex);
            _hasWarning[dayIndex] = hasWarning;
            _warningMessages[dayIndex] = message;
            OnPropertyChanged($"HasWarning{dayIndex}");
            OnPropertyChanged($"WarningMessage{dayIndex}");
        }

        public string GetWarningMessage(int dayIndex)
        {
            ValidateDayIndex(dayIndex);
            return _warningMessages[dayIndex];
        }

        // Calculated Properties
        public decimal TotalHours =>
            _hours[0] + _hours[1] + _hours[2] + _hours[3] +
            _hours[4] + _hours[5] + _hours[6];

        public string DisplayText =>
            $"{(!string.IsNullOrEmpty(TaskCode) ? TaskCode + " - " : "")}{TaskTitle}";

        private void ValidateDayIndex(int dayIndex)
        {
            if (dayIndex < 0 || dayIndex > 6)
                throw new ArgumentOutOfRangeException(nameof(dayIndex),
                    "Day index must be between 0 (Monday) and 6 (Sunday)");
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
