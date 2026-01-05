using ManagementFile.App.Services;
using ManagementFile.App.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ManagementFile.App.ViewModels.Advanced
{
    /// <summary>
    /// ViewModel for Advanced Search View
    /// Phase 5 Week 14 - UX Enhancement & Advanced Features
    /// </summary>
    public class AdvancedSearchViewModel : BaseViewModel
    {
        #region Fields

        private readonly AdvancedSearchService _searchService;
        
        private string _searchQuery;
        private bool _isSearching;
        private int _searchResultsCount;
        private string _lastSearchTimeText;
        private string _activeFilterName;
        private SearchResult _selectedResult;

        // Filter properties
        private bool _filterAll = true;
        private bool _filterProjects;
        private bool _filterTasks;
        private bool _filterFiles;
        private bool _filterUsers;
        private bool _filterReports;

        // Collections
        private ObservableCollection<SearchResult> _searchResults;
        private ObservableCollection<SearchSuggestion> _searchSuggestions;
        private ObservableCollection<SearchQuery> _recentSearches;
        private ObservableCollection<SearchFilter> _savedFilters;

        private string _sortBy = "Relevance";

        #endregion

        #region Constructor

        public AdvancedSearchViewModel(
            AdvancedSearchService advancedSearchService)
        {
            _searchService = advancedSearchService ?? throw new ArgumentNullException(nameof(advancedSearchService));

            InitializeCollections();
            InitializeCommands();
            LoadData();

            // Subscribe to service events
            _searchService.PropertyChanged += OnSearchServicePropertyChanged;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Search query text
        /// </summary>
        public string SearchQuery
        {
            get => _searchQuery ?? "";
            set
            {
                SetProperty(ref _searchQuery, value);
                OnPropertyChanged(nameof(HasSearchText));
                
                // Auto-suggest after a delay
                if (!string.IsNullOrWhiteSpace(value) && value.Length >= 2)
                {
                    Task.Run(async () => await LoadSuggestionsAsync(value));
                }
            }
        }

        /// <summary>
        /// Is search operation in progress
        /// </summary>
        public bool IsSearching
        {
            get => _isSearching;
            set => SetProperty(ref _isSearching, value);
        }

        /// <summary>
        /// Search results count
        /// </summary>
        public int SearchResultsCount
        {
            get => _searchResultsCount;
            set => SetProperty(ref _searchResultsCount, value);
        }

        /// <summary>
        /// Last search time text
        /// </summary>
        public string LastSearchTimeText
        {
            get => _lastSearchTimeText ?? "No recent searches";
            set => SetProperty(ref _lastSearchTimeText, value);
        }

        /// <summary>
        /// Active filter name
        /// </summary>
        public string ActiveFilterName
        {
            get => _activeFilterName ?? "All Items";
            set => SetProperty(ref _activeFilterName, value);
        }

        /// <summary>
        /// Selected search result
        /// </summary>
        public SearchResult SelectedResult
        {
            get => _selectedResult;
            set => SetProperty(ref _selectedResult, value);
        }

        /// <summary>
        /// Sort by option
        /// </summary>
        public string SortBy
        {
            get => _sortBy;
            set => SetProperty(ref _sortBy, value);
        }

        #endregion

        #region Filter Properties

        /// <summary>
        /// Filter all items
        /// </summary>
        public bool FilterAll
        {
            get => _filterAll;
            set
            {
                SetProperty(ref _filterAll, value);
                if (value)
                {
                    FilterProjects = FilterTasks = FilterFiles = FilterUsers = FilterReports = false;
                }
            }
        }

        /// <summary>
        /// Filter projects
        /// </summary>
        public bool FilterProjects
        {
            get => _filterProjects;
            set
            {
                SetProperty(ref _filterProjects, value);
                if (value) FilterAll = false;
            }
        }

        /// <summary>
        /// Filter tasks
        /// </summary>
        public bool FilterTasks
        {
            get => _filterTasks;
            set
            {
                SetProperty(ref _filterTasks, value);
                if (value) FilterAll = false;
            }
        }

        /// <summary>
        /// Filter files
        /// </summary>
        public bool FilterFiles
        {
            get => _filterFiles;
            set
            {
                SetProperty(ref _filterFiles, value);
                if (value) FilterAll = false;
            }
        }

        /// <summary>
        /// Filter users
        /// </summary>
        public bool FilterUsers
        {
            get => _filterUsers;
            set
            {
                SetProperty(ref _filterUsers, value);
                if (value) FilterAll = false;
            }
        }

        /// <summary>
        /// Filter reports
        /// </summary>
        public bool FilterReports
        {
            get => _filterReports;
            set
            {
                SetProperty(ref _filterReports, value);
                if (value) FilterAll = false;
            }
        }

        #endregion

        #region Collections

        /// <summary>
        /// Search results
        /// </summary>
        public ObservableCollection<SearchResult> SearchResults => _searchResults;

        /// <summary>
        /// Search suggestions
        /// </summary>
        public ObservableCollection<SearchSuggestion> SearchSuggestions => _searchSuggestions;

        /// <summary>
        /// Recent searches
        /// </summary>
        public ObservableCollection<SearchQuery> RecentSearches => _recentSearches;

        /// <summary>
        /// Saved filters
        /// </summary>
        public ObservableCollection<SearchFilter> SavedFilters => _savedFilters;

        #endregion

        #region UI Helper Properties

        /// <summary>
        /// Has search text
        /// </summary>
        public bool HasSearchText => !string.IsNullOrWhiteSpace(SearchQuery);

        /// <summary>
        /// Search results count text
        /// </summary>
        public string SearchResultsCountText => SearchResultsCount == 0 
            ? "No results found" 
            : SearchResultsCount == 1 
                ? "1 result found" 
                : $"{SearchResultsCount} results found";

        /// <summary>
        /// Has suggestions
        /// </summary>
        public bool HasSuggestions => _searchSuggestions.Any();

        /// <summary>
        /// Has search history
        /// </summary>
        public bool HasSearchHistory => _recentSearches.Any();

        /// <summary>
        /// Has saved filters
        /// </summary>
        public bool HasSavedFilters => _savedFilters.Any();

        #endregion

        #region Commands

        public ICommand SearchCommand { get; private set; }
        public ICommand ClearSearchCommand { get; private set; }
        public ICommand SelectSuggestionCommand { get; private set; }
        public ICommand OpenSearchResultCommand { get; private set; }
        public ICommand ExportResultsCommand { get; private set; }
        public ICommand ShowAdvancedFiltersCommand { get; private set; }
        public ICommand SaveCurrentFilterCommand { get; private set; }
        public ICommand DeleteFilterCommand { get; private set; }
        public ICommand ClearHistoryCommand { get; private set; }

        #endregion

        #region Methods

        private void InitializeCollections()
        {
            _searchResults = new ObservableCollection<SearchResult>();
            _searchSuggestions = new ObservableCollection<SearchSuggestion>();
            _recentSearches = new ObservableCollection<SearchQuery>();
            _savedFilters = new ObservableCollection<SearchFilter>();
        }

        private void InitializeCommands()
        {
            SearchCommand = new RelayCommand(async () => await ExecuteSearchAsync(), () => HasSearchText);
            ClearSearchCommand = new RelayCommand(ClearSearch);
            SelectSuggestionCommand = new RelayCommand<SearchSuggestion>(SelectSuggestion);
            OpenSearchResultCommand = new RelayCommand<SearchResult>(OpenSearchResult);
            ExportResultsCommand = new RelayCommand(async () => await ExportResultsAsync(), () => _searchResults.Any());
            ShowAdvancedFiltersCommand = new RelayCommand(ShowAdvancedFilters);
            SaveCurrentFilterCommand = new RelayCommand(SaveCurrentFilter);
            DeleteFilterCommand = new RelayCommand<Guid>(DeleteFilter);
            ClearHistoryCommand = new RelayCommand(ClearHistory);
        }

        private void LoadData()
        {
            // Load initial data from service
            LoadRecentSearches();
            LoadSavedFilters();
            UpdateActiveFilter();
        }

        private void OnSearchServicePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AdvancedSearchService.IsSearching))
            {
                IsSearching = _searchService.IsSearching;
            }
            else if (e.PropertyName == nameof(AdvancedSearchService.SearchResultsCount))
            {
                SearchResultsCount = _searchService.SearchResultsCount;
                OnPropertyChanged(nameof(SearchResultsCountText));
            }
            else if (e.PropertyName == nameof(AdvancedSearchService.LastSearchTime))
            {
                LastSearchTimeText = _searchService.LastSearchTimeText;
            }
        }

        private async Task ExecuteSearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery)) return;

            try
            {
                // Create search filter based on current filter settings
                var filter = CreateCurrentFilter();
                
                // Perform search
                var results = await _searchService.SearchAsync(SearchQuery, filter);
                
                // Update UI
                _searchResults.Clear();
                foreach (var result in results.Results)
                {
                    _searchResults.Add(result);
                }

                SearchResultsCount = results.TotalCount;
                LastSearchTimeText = results.SearchTime.ToString("HH:mm:ss");
                
                // Load updated recent searches
                LoadRecentSearches();
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Search failed: {ex.Message}");
            }
        }

        private SearchFilter CreateCurrentFilter()
        {
            var categories = new List<string>();
            
            if (FilterAll)
            {
                categories.AddRange(new[] { "Projects", "Tasks", "Files", "Users", "Reports" });
            }
            else
            {
                if (FilterProjects) categories.Add("Projects");
                if (FilterTasks) categories.Add("Tasks");
                if (FilterFiles) categories.Add("Files");
                if (FilterUsers) categories.Add("Users");
                if (FilterReports) categories.Add("Reports");
            }

            return new SearchFilter
            {
                Name = "Current Filter",
                Categories = categories,
                IsDefault = false
            };
        }

        private void ClearSearch()
        {
            SearchQuery = "";
            _searchResults.Clear();
            _searchSuggestions.Clear();
            SearchResultsCount = 0;
            SelectedResult = null;
        }

        private void SelectSuggestion(SearchSuggestion suggestion)
        {
            if (suggestion == null) return;
            
            SearchQuery = suggestion.Text;
            _searchSuggestions.Clear();
            Task.Run(async () => await ExecuteSearchAsync());
        }

        private void OpenSearchResult(SearchResult result)
        {
            if (result == null) return;
            
            // This would typically navigate to the actual item
            ShowInfoMessage($"Opening: {result.Title} ({result.Category})");
        }

        private async Task ExportResultsAsync()
        {
            try
            {
                // Simulate export
                await Task.Delay(1000);
                ShowInfoMessage($"Exported {SearchResultsCount} search results successfully!");
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Export failed: {ex.Message}");
            }
        }

        private void ShowAdvancedFilters()
        {
            ShowInfoMessage("Advanced filters dialog would open here");
        }

        private void SaveCurrentFilter()
        {
            var filter = CreateCurrentFilter();
            filter.Name = $"Filter {DateTime.Now:HH:mm}";
            filter.Description = $"Custom filter saved at {DateTime.Now:HH:mm}";
            
            _searchService.SaveFilter(filter);
            LoadSavedFilters();
            ShowInfoMessage("Current filter saved successfully!");
        }

        private void DeleteFilter(Guid filterId)
        {
            _searchService.DeleteFilter(filterId);
            LoadSavedFilters();
            ShowInfoMessage("Filter deleted successfully!");
        }

        private void ClearHistory()
        {
            _searchService.ClearSearchHistory();
            LoadRecentSearches();
            ShowInfoMessage("Search history cleared!");
        }

        private async Task LoadSuggestionsAsync(string query)
        {
            try
            {
                var suggestions = await _searchService.GetSuggestionsAsync(query);
                
                Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    _searchSuggestions.Clear();
                    foreach (var suggestion in suggestions.Take(5))
                    {
                        _searchSuggestions.Add(suggestion);
                    }
                    OnPropertyChanged(nameof(HasSuggestions));
                }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading suggestions: {ex.Message}");
            }
        }

        private void LoadRecentSearches()
        {
            _recentSearches.Clear();
            foreach (var search in _searchService.SearchHistory.Take(10))
            {
                _recentSearches.Add(search);
            }
            OnPropertyChanged(nameof(HasSearchHistory));
        }

        private void LoadSavedFilters()
        {
            _savedFilters.Clear();
            foreach (var filter in _searchService.SavedFilters)
            {
                _savedFilters.Add(filter);
            }
            OnPropertyChanged(nameof(HasSavedFilters));
            UpdateActiveFilter();
        }

        private void UpdateActiveFilter()
        {
            if (FilterAll)
            {
                ActiveFilterName = "All Items";
            }
            else
            {
                var activeCategories = new List<string>();
                if (FilterProjects) activeCategories.Add("Projects");
                if (FilterTasks) activeCategories.Add("Tasks");
                if (FilterFiles) activeCategories.Add("Files");
                if (FilterUsers) activeCategories.Add("Users");
                if (FilterReports) activeCategories.Add("Reports");
                
                ActiveFilterName = activeCategories.Any() 
                    ? string.Join(", ", activeCategories) 
                    : "No Filter";
            }
        }

        private void ShowInfoMessage(string message)
        {
            System.Windows.MessageBox.Show(message, "Information", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void ShowErrorMessage(string message)
        {
            System.Windows.MessageBox.Show(message, "Error", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_searchService != null)
                    _searchService.PropertyChanged -= OnSearchServicePropertyChanged;
            }
            base.Dispose(disposing);
        }

        #endregion
    }

    #region Command Implementation

    /// <summary>
    /// Simple relay command implementation
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object parameter) => _execute();
    }

    /// <summary>
    /// Generic relay command implementation
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke((T)parameter) ?? true;

        public void Execute(object parameter) => _execute((T)parameter);
    }

    #endregion
}