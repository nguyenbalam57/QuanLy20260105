using ManagementFile.App.Models.Projects;
using ManagementFile.App.Models.TimeTracking;
using ManagementFile.App.Services.TimeTracking;
using ManagementFile.Contracts.DTOs.ProjectManagement.TimeLogs;
using ManagementFile.Contracts.Requests.ProjectManagement.TimeLogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ManagementFile.App.ViewModels.TimeLogs
{
    public class WeeklyTimesheetViewModel : BaseViewModel, INotifyPropertyChanged
    {
        private readonly TimeTrackingApiService _timeTrackingApiService;

        private int _currentUserId;
        private DateTime _weekStart;
        private DateTime _weekEnd;
        private ObservableCollection<WeeklyTimeEntry> _entries;
        private ObservableCollection<ProjectTaskModel> _availableTasks;
        private ProjectTaskModel _selectedTaskToAdd;
        private bool _isLoading;
        private string _statusMessage;
        private ObservableCollection<string> _validationMessages;

        private ProjectModel _selectProjectModel;


        // Daily totals
        private decimal[] _dailyTotals = new decimal[7];
        private bool[] _dailyHasWarning = new bool[7];

        public WeeklyTimesheetViewModel(
            TimeTrackingApiService timeTrackingApiService)
        {
            _timeTrackingApiService = timeTrackingApiService ?? throw new ArgumentNullException(nameof(timeTrackingApiService));

            Entries = new ObservableCollection<WeeklyTimeEntry>();
            AvailableTasks = new ObservableCollection<ProjectTaskModel>();
            ValidationMessages = new ObservableCollection<string>();

            // Set default week to current week
            SetCurrentWeek();

            // Initialize commands
            InitializeCommands();

            // Load data
            _ = InitializeAsync();
        }

        #region Properties

        public DateTime WeekStart
        {
            get => _weekStart;
            set
            {
                if (SetProperty(ref _weekStart, value))
                {
                    _weekEnd = _weekStart.AddDays(6);
                    OnPropertyChanged(nameof(WeekEnd));
                    OnPropertyChanged(nameof(WeekDisplay));
                    OnPropertyChanged(nameof(WeekNumber));
                    OnPropertyChanged(nameof(DayHeaders));
                    _ = LoadDataAsync();
                }
            }
        }

        public DateTime WeekEnd
        {
            get => _weekEnd;
        }

        public string WeekDisplay =>
            $"{WeekStart:dd/MM/yyyy} - {WeekEnd:dd/MM/yyyy}";

        public int WeekNumber =>
            System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                WeekStart,
                System.Globalization.CalendarWeekRule.FirstDay,
                DayOfWeek.Monday);

        public ObservableCollection<WeeklyTimeEntry> Entries
        {
            get => _entries;
            set => SetProperty(ref _entries, value);
        }

        public ObservableCollection<ProjectTaskModel> AvailableTasks
        {
            get => _availableTasks;
            set => SetProperty(ref _availableTasks, value);
        }

        public ProjectTaskModel SelectedTaskToAdd
        {
            get => _selectedTaskToAdd;
            set => SetProperty(ref _selectedTaskToAdd, value);
        }

        public ProjectModel SelectProjectModel
        {
            get => _selectProjectModel;
            set
            {
                if (SetProperty(ref _selectProjectModel, value))
                {
                    Task.Run(async () => await LoadAvailableTasksAsync());
                }
            }
                
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ObservableCollection<string> ValidationMessages
        {
            get => _validationMessages;
            set => SetProperty(ref _validationMessages, value);
        }

        // Week totals
        public decimal WeekTotalHours => _dailyTotals.Sum();

        // Daily totals properties for binding
        public decimal DailyTotal0 => _dailyTotals[0];
        public decimal DailyTotal1 => _dailyTotals[1];
        public decimal DailyTotal2 => _dailyTotals[2];
        public decimal DailyTotal3 => _dailyTotals[3];
        public decimal DailyTotal4 => _dailyTotals[4];
        public decimal DailyTotal5 => _dailyTotals[5];
        public decimal DailyTotal6 => _dailyTotals[6];

        public bool DailyHasWarning0 => _dailyHasWarning[0];
        public bool DailyHasWarning1 => _dailyHasWarning[1];
        public bool DailyHasWarning2 => _dailyHasWarning[2];
        public bool DailyHasWarning3 => _dailyHasWarning[3];
        public bool DailyHasWarning4 => _dailyHasWarning[4];
        public bool DailyHasWarning5 => _dailyHasWarning[5];
        public bool DailyHasWarning6 => _dailyHasWarning[6];

        // Day headers
        public string[] DayHeaders => new[]
        {
            $"T2\n{WeekStart:dd/MM}",
            $"T3\n{WeekStart. AddDays(1):dd/MM}",
            $"T4\n{WeekStart. AddDays(2):dd/MM}",
            $"T5\n{WeekStart. AddDays(3):dd/MM}",
            $"T6\n{WeekStart. AddDays(4):dd/MM}",
            $"T7\n{WeekStart. AddDays(5):dd/MM}",
            $"CN\n{WeekStart.AddDays(6):dd/MM}"
        };

        #endregion

        #region Commands

        public ICommand PreviousWeekCommand { get; private set; }
        public ICommand NextWeekCommand { get; private set; }
        public ICommand CurrentWeekCommand { get; private set; }
        public ICommand AddTaskCommand { get; private set; }
        public ICommand RemoveTaskCommand { get; private set; }
        public ICommand SaveCommand { get; private set; }
        public ICommand CopyPreviousWeekCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }
        public ICommand CellChangedCommand { get; private set; }

        private void InitializeCommands()
        {
            PreviousWeekCommand = new RelayCommand(() => WeekStart = WeekStart.AddDays(-7));
            NextWeekCommand = new RelayCommand(() => WeekStart = WeekStart.AddDays(7));
            CurrentWeekCommand = new RelayCommand(SetCurrentWeek);
            AddTaskCommand = new RelayCommand(AddTask, () => SelectedTaskToAdd != null);
            RemoveTaskCommand = new RelayCommand<WeeklyTimeEntry>(RemoveTask);
            SaveCommand = new RelayCommand(async () => await SaveAsync(true));
            CopyPreviousWeekCommand = new RelayCommand(async () => await CopyPreviousWeekAsync());
            RefreshCommand = new RelayCommand(async () => await LoadDataAsync());
            CellChangedCommand = new RelayCommand<CellChangedEventArgs>(OnCellChanged);
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize async - load current user và available tasks
        /// </summary>
        private async Task InitializeAsync()
        {
            try
            {
                // Get current user ID
                var currentUser = App.GetCurrentUser();
                _currentUserId = currentUser.Id;

                // Load available tasks
                await LoadAvailableTasksAsync();

                // Load timesheet data
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Khởi tạo thất bại: {ex.Message}";
                MessageBox.Show(
                    $"Không thể khởi tạo ViewModel:\n{ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion

        #region Methods

        private void SetCurrentWeek()
        {
            var today = DateTime.Today;
            var diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            WeekStart = today.AddDays(-diff).Date;
        }

        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Đang tải dữ liệu...";

                // ✅ Gọi API thông qua service
                var timesheet = await _timeTrackingApiService.GetWeeklyTimesheetAsync(WeekStart);

                // Clear và populate entries
                Entries.Clear();

                foreach (var taskEntry in timesheet.TaskEntries)
                {
                    var entry = new WeeklyTimeEntry
                    {
                        TaskId = taskEntry.TaskId,
                        TaskTitle = taskEntry.TaskTitle,
                        TaskCode = taskEntry.TaskCode,
                        HourlyRate = taskEntry.HourlyRate ?? 0,
                    };

                    // Fill daily hours
                    foreach (var dailyEntry in taskEntry.DailyEntries)
                    {
                        entry.SetHours(dailyEntry.DayIndex, dailyEntry.Hours);
                        entry.SetNote(dailyEntry.DayIndex, dailyEntry.Note);
                    }

                    entry.PropertyChanged += Entry_PropertyChanged;
                    Entries.Add(entry);
                }

                RecalculateTotals();
                ValidateWeek();

                StatusMessage = $"✅ Đã tải {Entries.Count} task";
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Lỗi: {ex.Message}";
                MessageBox.Show(
                    $"Không thể tải dữ liệu:\n{ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void AddTask()
        {
            if (SelectedTaskToAdd == null)
                return;

            // Check if task already exists
            if (Entries.Any(e => e.TaskId == SelectedTaskToAdd.Id))
            {
                MessageBox.Show("Task này đã có trong timesheet", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var entry = new WeeklyTimeEntry
            {
                TaskId = SelectedTaskToAdd.Id,
                TaskTitle = SelectedTaskToAdd.Title,
                TaskCode = SelectedTaskToAdd.TaskCode,
                HourlyRate = 50000, // Default, should get from user profile
            };

            entry.PropertyChanged += Entry_PropertyChanged;
            Entries.Add(entry);

            SelectedTaskToAdd = null;
            StatusMessage = $"✅ Đã thêm task: {entry.TaskTitle}";
        }

        private void RemoveTask(WeeklyTimeEntry entry)
        {
            if (entry == null)
                return;

            var result = MessageBox.Show(
                $"Xóa task '{entry.TaskTitle}' khỏi timesheet?\n\nLưu ý: Dữ liệu chưa lưu sẽ bị mất.",
                "Xác nhận",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                entry.PropertyChanged -= Entry_PropertyChanged;
                Entries.Remove(entry);
                RecalculateTotals();
                ValidateWeek();
                StatusMessage = $"✅ Đã xóa task: {entry.TaskTitle}";
            }
        }

        private void OnCellChanged(CellChangedEventArgs args)
        {
            if (args == null)
                return;

            var entry = args.Entry;
            var dayIndex = args.DayIndex;
            var hours = args.NewValue;

            // Set new value
            entry.SetHours(dayIndex, hours);

            // ✅ Simple client-side validation (không cần _validationService)
            var date = WeekStart.AddDays(dayIndex);
            var dailyTotal = _dailyTotals[dayIndex] + hours;

            // Check basic validations
            bool hasWarning = false;
            string warningMessage = null;

            if (dailyTotal > 12)
            {
                hasWarning = true;
                warningMessage = $"⚠️ Tổng giờ trong ngày: {dailyTotal:F1}h (>12h)";
            }
            else if (dailyTotal > 8)
            {
                hasWarning = true;
                warningMessage = $"⚠️ Overtime: {dailyTotal:F1}h";
            }

            // Weekend warning
            if (dayIndex >= 5 && hours > 0) // Saturday or Sunday
            {
                hasWarning = true;
                warningMessage = "⚠️ Làm việc cuối tuần";
            }

            entry.SetWarning(dayIndex, hasWarning, warningMessage);

            // Recalculate totals
            RecalculateTotals();

            // Revalidate week
            ValidateWeek();
        }

        private void Entry_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // When any entry property changes, recalculate totals
            if (e.PropertyName.StartsWith("Hours") || e.PropertyName == nameof(WeeklyTimeEntry.TotalHours))
            {
                RecalculateTotals();
            }
        }

        private void RecalculateTotals()
        {
            // Reset daily totals
            for (int day = 0; day < 7; day++)
            {
                _dailyTotals[day] = 0;
                _dailyHasWarning[day] = false;
            }

            // Calculate from entries
            if (Entries != null)
            {
                foreach (var entry in Entries)
                {
                    for (int day = 0; day < 7; day++)
                    {
                        var hours = entry.GetHours(day);
                        _dailyTotals[day] += hours;

                        if (entry.GetHasWarning(day))
                        {
                            _dailyHasWarning[day] = true;
                        }
                    }
                }
            }

            // Notify UI for all daily properties
            for (int day = 0; day < 7; day++)
            {
                OnPropertyChanged($"DailyTotal{day}");
                OnPropertyChanged($"DailyCost{day}");
                OnPropertyChanged($"DailyHasWarning{day}");
            }

            OnPropertyChanged(nameof(WeekTotalHours));
        }

        private void ValidateWeek()
        {
            ValidationMessages.Clear();

            // ✅ Simple client-side validation
            var weekTotal = _dailyTotals.Sum();

            // Check week total
            if (weekTotal > 60)
            {
                ValidationMessages.Add($"⚠️ Tổng giờ tuần ({weekTotal:F1}h) vượt quá 60h");
            }
            else if (weekTotal > 40)
            {
                ValidationMessages.Add($"⚠️ Overtime tuần: {weekTotal:F1}h (chuẩn: 40h)");
            }

            // Check daily totals
            for (int day = 0; day < 5; day++) // Mon-Fri
            {
                if (_dailyTotals[day] > 10)
                {
                    var date = WeekStart.AddDays(day);
                    ValidationMessages.Add($"⚠️ Ngày {date:dd/MM}: {_dailyTotals[day]:F1}h (quá cao)");
                }
            }

            // Check weekend work
            if (_dailyTotals[5] > 0 || _dailyTotals[6] > 0)
            {
                ValidationMessages.Add("⚠️ Có làm việc cuối tuần");
            }

            // Check empty timesheet
            if (weekTotal == 0 && Entries.Any())
            {
                ValidationMessages.Add("ℹ️ Timesheet chưa có dữ liệu");
            }

            // If no validation messages, add success message
            if (ValidationMessages.Count == 0)
            {
                ValidationMessages.Add("✅ Không có cảnh báo");
            }
        }

        private async Task SaveAsync(bool andSubmit)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Đang lưu... ";

                // Build save request
                var saveRequest = new SaveWeeklyTimesheetRequest
                {
                    WeekStartDate = WeekStart,
                    SubmitForApproval = andSubmit,
                    Entries = Entries.Select(e => new WeeklyTimesheetEntryRequest
                    {
                        TaskId = e.TaskId,
                        HourlyRate = e.HourlyRate,
                        DailyEntries = Enumerable.Range(0, 7)
                            .Select(day => new DailyTimeEntryRequest
                            {
                                DayIndex = day,
                                Hours = e.GetHours(day),
                                Note = e.GetNote(day) ?? ""
                            })
                            .Where(d => d.Hours >= 0)
                            .ToList()
                    })
                    .Where(e => e.DailyEntries.Any())
                    .ToList()
                };

                // Validate trước khi lưu
                if (andSubmit)
                {
                    var validation = await _timeTrackingApiService.ValidateWeeklyTimesheetAsync(saveRequest);

                    if (!validation.IsValid)
                    {
                        var errorMsg = string.Join("\n", validation.Errors);
                        MessageBox.Show(errorMsg, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        StatusMessage = "❌ Validation failed";
                        return;
                    }
                }

                // ✅ Save qua API
                var savedTimesheet = await _timeTrackingApiService.SaveWeeklyTimesheetAsync(saveRequest);

                StatusMessage = andSubmit
                    ? "✅ Đã lưu"
                    : "✅ Đã lưu timesheet";

                MessageBox.Show(
                    StatusMessage,
                    "Thành công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Reload data
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Lỗi: {ex.Message}";
                MessageBox.Show(
                    $"Lỗi khi lưu:\n{ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task CopyPreviousWeekAsync()
        {
            var result = MessageBox.Show(
                "Copy dữ liệu từ tuần trước?\n\nDữ liệu hiện tại sẽ bị ghi đè.",
                "Xác nhận",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                IsLoading = true;
                StatusMessage = "Đang copy từ tuần trước...";

                var previousWeekStart = WeekStart.AddDays(-7);

                // ✅ Copy qua API
                var copiedTimesheet = await _timeTrackingApiService.CopyWeeklyTimesheetAsync(
                    previousWeekStart,
                    WeekStart,
                    includeNotes: false);

                StatusMessage = "✅ Đã copy từ tuần trước";

                // Reload data
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Lỗi: {ex.Message}";
                MessageBox.Show(
                    $"Lỗi khi copy:\n{ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadAvailableTasksAsync()
        {
            if(SelectProjectModel == null)
                return;

            try
            {
                // ✅ Load tasks qua API
                var tasks = await LoadTaskParentChilldAsync(SelectProjectModel.Id);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    AvailableTasks.Clear();
                    foreach (var task in tasks)
                    {
                        AvailableTasks.Add(task);
                    }
                });

                System.Diagnostics.Debug.WriteLine($"✅ Loaded {AvailableTasks.Count} available tasks");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading tasks: {ex.Message}");
                // Don't show error to user, just log it
            }
        }

        private async Task<List<ProjectTaskModel>> LoadTaskParentChilldAsync(int projectId)
        {
            try
            {
                // ✅ Load tasks qua API
                var tasks = await _timeTrackingApiService.GetAvailableTasksForTimesheetAsync(projectId);

                var taskModels = tasks.Select(t => new ProjectTaskModel
                {
                    Id = t.TaskId,
                    ParentTaskId = t.ParentTaskId,
                    Title = t.TaskTitle,
                    TaskCode = t.TaskCode,
                    ProjectId = t.ProjectId
                }).ToList();

                List<ProjectTaskModel> sortedTasks = new List<ProjectTaskModel>();

                // Sắp xếp theo từ nhỏ đến lớn theo cấp cha con
                // Sử dụng đệ quy để xây dựng danh sách
                foreach (var task in taskModels.Where(t => t.ParentTaskId == null).OrderBy(t => t.Id).ToList())
                {
                    task.HierarchyLevel = 0;
                    sortedTasks.Add(task);
                    var childTasks = await GetChildTasksAsync(task.Id, taskModels, task.HierarchyLevel);
                    // Có thể xử lý childTasks nếu cần
                    if (childTasks != null && childTasks.Count > 0)
                    {
                        sortedTasks.AddRange(childTasks);
                    }
                }

                return sortedTasks;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading tasks: {ex.Message}");
                return new List<ProjectTaskModel>();
            }
        }

        private async Task<List<ProjectTaskModel>> GetChildTasksAsync(int parentTaskId, List<ProjectTaskModel> allTasks, int hierarchyLevel)
        {
            int nextHierarchyLevel = hierarchyLevel + 1;

            var childTasks = allTasks.Where(t => t.ParentTaskId == parentTaskId).ToList();
            var result = new List<ProjectTaskModel>(childTasks.OrderBy(t => t.Id));
            foreach(var task in result)
            {
                task.HierarchyLevel = nextHierarchyLevel;
            }
            foreach (var child in childTasks.OrderBy(t => t.Id))
            {
                var subChildren = await GetChildTasksAsync(child.Id, allTasks, nextHierarchyLevel);
                result.AddRange(subChildren);
            }
            return result;
        }


        #endregion

    }

    #region Helper Classes

    public class CellChangedEventArgs
    {
        public WeeklyTimeEntry Entry { get; set; }
        public int DayIndex { get; set; }
        public decimal NewValue { get; set; }
    }

    #endregion
}
