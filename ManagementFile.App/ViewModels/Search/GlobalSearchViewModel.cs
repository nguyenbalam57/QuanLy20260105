using ManagementFile.App.ViewModels;
using ManagementFile.App.Services;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.ComponentModel;

namespace ManagementFile.App.ViewModels.Search
{
    /// <summary>
    /// Global Search ViewModel - Phase 7 Advanced Integration
    /// Universal search across all phases với intelligent suggestions
    /// </summary>
    public class GlobalSearchViewModel : BaseViewModel
    {
        #region Fields

        private readonly ServiceManager _serviceManager;
        private readonly DataCache _dataCache;
        private readonly EventBus _eventBus;
        private readonly NavigationService _navigationService;

        // Search State
        private string _searchQuery = "";
        private bool _isSearching;
        private string _searchStatus = "Ready to search";
        private int _searchResultsCount;
        private DateTime _lastSearchTime;

        // Search Configuration
        private bool _searchProjects = true;
        private bool _searchTasks = true;
        private bool _searchFiles = true;
        private bool _searchUsers = true;
        private bool _searchReports = true;
        private bool _searchNotifications = true;

        // Results
        private ObservableCollection<GlobalSearchResult> _searchResults;
        private ObservableCollection<SearchCategory> _searchCategories;
        private ObservableCollection<string> _searchSuggestions;
        private ObservableCollection<string> _recentSearches;

        // Selected items
        private GlobalSearchResult _selectedResult;
        private SearchCategory _selectedCategory;

        #endregion

        #region Constructor

        public GlobalSearchViewModel(
            ServiceManager serviceManager,
            DataCache dataCache,
            EventBus eventBus,
            NavigationService navigationService)
        {
            _serviceManager = serviceManager ?? throw new ArgumentNullException(nameof(serviceManager));
            _dataCache = dataCache ?? throw new ArgumentNullException(nameof(dataCache));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            // Initialize collections
            SearchResults = new ObservableCollection<GlobalSearchResult>();
            SearchCategories = new ObservableCollection<SearchCategory>();
            SearchSuggestions = new ObservableCollection<string>();
            RecentSearches = new ObservableCollection<string>();

            // Initialize commands
            InitializeCommands();

            // Initialize search categories
            InitializeSearchCategories();

            // Load recent searches
            LoadRecentSearches();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Search query text
        /// </summary>
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                {
                    OnSearchQueryChanged();
                }
            }
        }

        /// <summary>
        /// Is currently searching
        /// </summary>
        public bool IsSearching
        {
            get => _isSearching;
            set => SetProperty(ref _isSearching, value);
        }

        /// <summary>
        /// Search status message
        /// </summary>
        public string SearchStatus
        {
            get => _searchStatus;
            set => SetProperty(ref _searchStatus, value);
        }

        /// <summary>
        /// Number of search results
        /// </summary>
        public int SearchResultsCount
        {
            get => _searchResultsCount;
            set
            {
                SetProperty(ref _searchResultsCount, value);
                OnPropertyChanged(nameof(HasSearchResults));
                OnPropertyChanged(nameof(SearchResultsText));
            }
        }

        /// <summary>
        /// Last search timestamp
        /// </summary>
        public DateTime LastSearchTime
        {
            get => _lastSearchTime;
            set
            {
                SetProperty(ref _lastSearchTime, value);
                OnPropertyChanged(nameof(LastSearchTimeText));
            }
        }

        /// <summary>
        /// Search scope: Projects
        /// </summary>
        public bool SearchProjects
        {
            get => _searchProjects;
            set => SetProperty(ref _searchProjects, value);
        }

        /// <summary>
        /// Search scope: Tasks
        /// </summary>
        public bool SearchTasks
        {
            get => _searchTasks;
            set => SetProperty(ref _searchTasks, value);
        }

        /// <summary>
        /// Search scope: Files
        /// </summary>
        public bool SearchFiles
        {
            get => _searchFiles;
            set => SetProperty(ref _searchFiles, value);
        }

        /// <summary>
        /// Search scope: Users
        /// </summary>
        public bool SearchUsers
        {
            get => _searchUsers;
            set => SetProperty(ref _searchUsers, value);
        }

        /// <summary>
        /// Search scope: Reports
        /// </summary>
        public bool SearchReports
        {
            get => _searchReports;
            set => SetProperty(ref _searchReports, value);
        }

        /// <summary>
        /// Search scope: Notifications
        /// </summary>
        public bool SearchNotifications
        {
            get => _searchNotifications;
            set => SetProperty(ref _searchNotifications, value);
        }

        /// <summary>
        /// Search results collection
        /// </summary>
        public ObservableCollection<GlobalSearchResult> SearchResults
        {
            get => _searchResults;
            set => SetProperty(ref _searchResults, value);
        }

        /// <summary>
        /// Search categories for filtering
        /// </summary>
        public ObservableCollection<SearchCategory> SearchCategories
        {
            get => _searchCategories;
            set => SetProperty(ref _searchCategories, value);
        }

        /// <summary>
        /// Search suggestions
        /// </summary>
        public ObservableCollection<string> SearchSuggestions
        {
            get => _searchSuggestions;
            set => SetProperty(ref _searchSuggestions, value);
        }

        /// <summary>
        /// Recent searches
        /// </summary>
        public ObservableCollection<string> RecentSearches
        {
            get => _recentSearches;
            set => SetProperty(ref _recentSearches, value);
        }

        /// <summary>
        /// Selected search result
        /// </summary>
        public GlobalSearchResult SelectedResult
        {
            get => _selectedResult;
            set => SetProperty(ref _selectedResult, value);
        }

        /// <summary>
        /// Selected search category
        /// </summary>
        public SearchCategory SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    FilterResultsByCategory();
                }
            }
        }

        #endregion

        #region UI Helper Properties

        /// <summary>
        /// Has search results
        /// </summary>
        public bool HasSearchResults => SearchResultsCount > 0;

        /// <summary>
        /// Search results text
        /// </summary>
        public string SearchResultsText => $"{SearchResultsCount} results found";

        /// <summary>
        /// Last search time text
        /// </summary>
        public string LastSearchTimeText
        {
            get
            {
                if (LastSearchTime == default) return "";
                return $"Last search: {LastSearchTime:HH:mm:ss}";
            }
        }

        /// <summary>
        /// Has search query
        /// </summary>
        public bool HasSearchQuery => !string.IsNullOrWhiteSpace(SearchQuery);

        /// <summary>
        /// Search suggestions visibility
        /// </summary>
        public bool ShowSearchSuggestions => HasSearchQuery && SearchSuggestions.Count > 0;

        #endregion

        #region Commands

        public ICommand SearchCommand { get; private set; }
        public ICommand ClearSearchCommand { get; private set; }
        public ICommand OpenResultCommand { get; private set; }
        public ICommand AddToFavoritesCommand { get; private set; }
        public ICommand ExportResultsCommand { get; private set; }
        public ICommand SelectSuggestionCommand { get; private set; }
        public ICommand SelectRecentSearchCommand { get; private set; }
        public ICommand SelectCategoryCommand { get; private set; }
        public ICommand AdvancedSearchCommand { get; private set; }

        #endregion

        #region Methods

        private void InitializeCommands()
        {
            SearchCommand = new AsyncRelayCommand(ExecuteSearchAsync, CanExecuteSearch);
            ClearSearchCommand = new RelayCommand(ExecuteClearSearch);
            OpenResultCommand = new RelayCommand<GlobalSearchResult>(ExecuteOpenResult);
            AddToFavoritesCommand = new RelayCommand<GlobalSearchResult>(ExecuteAddToFavorites);
            ExportResultsCommand = new RelayCommand(ExecuteExportResults);
            SelectSuggestionCommand = new RelayCommand<string>(ExecuteSelectSuggestion);
            SelectRecentSearchCommand = new RelayCommand<string>(ExecuteSelectRecentSearch);
            SelectCategoryCommand = new RelayCommand<SearchCategory>(ExecuteSelectCategory);
            AdvancedSearchCommand = new RelayCommand(ExecuteAdvancedSearch);
        }

        /// <summary>
        /// Initialize search categories
        /// </summary>
        private void InitializeSearchCategories()
        {
            SearchCategories.Clear();

            SearchCategories.Add(new SearchCategory
            {
                Id = "all",
                Name = "All Results",
                Icon = "🔍",
                IsSelected = true
            });

            SearchCategories.Add(new SearchCategory
            {
                Id = "projects",
                Name = "Projects",
                Icon = "📋",
                IsSelected = false
            });

            SearchCategories.Add(new SearchCategory
            {
                Id = "tasks",
                Name = "Tasks", 
                Icon = "✓",
                IsSelected = false
            });

            SearchCategories.Add(new SearchCategory
            {
                Id = "files",
                Name = "Files",
                Icon = "📁",
                IsSelected = false
            });

            SearchCategories.Add(new SearchCategory
            {
                Id = "users",
                Name = "Users",
                Icon = "👥",
                IsSelected = false
            });

            SearchCategories.Add(new SearchCategory
            {
                Id = "reports",
                Name = "Reports",
                Icon = "📊",
                IsSelected = false
            });

            SearchCategories.Add(new SearchCategory
            {
                Id = "notifications",
                Name = "Notifications",
                Icon = "🔔",
                IsSelected = false
            });

            SelectedCategory = SearchCategories.First();
        }

        /// <summary>
        /// Load recent searches from cache
        /// </summary>
        private void LoadRecentSearches()
        {
            try
            {
                // Load from cache if available
                var recentSearches = _dataCache.Get<List<string>>("recent-searches");
                if (recentSearches != null)
                {
                    RecentSearches.Clear();
                    foreach (var search in recentSearches.Take(10))
                    {
                        RecentSearches.Add(search);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Recent searches loading error: {ex.Message}");
            }
        }

        /// <summary>
        /// Save search to recent searches
        /// </summary>
        private void SaveToRecentSearches(string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query)) return;

                // Remove if already exists
                if (RecentSearches.Contains(query))
                {
                    RecentSearches.Remove(query);
                }

                // Add to beginning
                RecentSearches.Insert(0, query);

                // Keep only 10 recent searches
                while (RecentSearches.Count > 10)
                {
                    RecentSearches.RemoveAt(RecentSearches.Count - 1);
                }

                // Save to cache
                _dataCache.Set("recent-searches", RecentSearches.ToList(), TimeSpan.FromDays(30));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Save recent search error: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle search query changes for real-time suggestions
        /// </summary>
        private void OnSearchQueryChanged()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SearchQuery))
                {
                    SearchSuggestions.Clear();
                    OnPropertyChanged(nameof(ShowSearchSuggestions));
                    return;
                }

                // Generate search suggestions
                Task.Run(() => GenerateSearchSuggestionsAsync(SearchQuery));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Search query change error: {ex.Message}");
            }
        }

        /// <summary>
        /// Generate intelligent search suggestions
        /// </summary>
        private async Task GenerateSearchSuggestionsAsync(string query)
        {
            try
            {
                await Task.Delay(300); // Debounce delay

                if (SearchQuery != query) return; // Query changed, ignore

                var suggestions = new List<string>();

                // Add suggestions based on search scope
                if (SearchProjects)
                {
                    suggestions.AddRange(GetProjectSuggestions(query));
                }

                if (SearchTasks)
                {
                    suggestions.AddRange(GetTaskSuggestions(query));
                }

                if (SearchUsers)
                {
                    suggestions.AddRange(GetUserSuggestions(query));
                }

                if (SearchFiles)
                {
                    suggestions.AddRange(GetFileSuggestions(query));
                }

                // Update UI on main thread
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    SearchSuggestions.Clear();
                    foreach (var suggestion in suggestions.Distinct().Take(5))
                    {
                        SearchSuggestions.Add(suggestion);
                    }
                    OnPropertyChanged(nameof(ShowSearchSuggestions));
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Search suggestions error: {ex.Message}");
            }
        }

        /// <summary>
        /// Execute global search across all phases
        /// </summary>
        private async Task ExecuteSearchAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SearchQuery)) return;

                IsSearching = true;
                SearchStatus = "Searching across all phases...";
                SearchResults.Clear();

                var allResults = new List<GlobalSearchResult>();

                // Search in parallel across phases
                var searchTasks = new List<Task<List<GlobalSearchResult>>>();

                if (SearchProjects)
                    searchTasks.Add(SearchProjectsAsync(SearchQuery));

                if (SearchTasks)
                    searchTasks.Add(SearchTasksAsync(SearchQuery));

                if (SearchFiles)
                    searchTasks.Add(SearchFilesAsync(SearchQuery));

                if (SearchUsers)
                    searchTasks.Add(SearchUsersAsync(SearchQuery));

                if (SearchReports)
                    searchTasks.Add(SearchReportsAsync(SearchQuery));

                if (SearchNotifications)
                    searchTasks.Add(SearchNotificationsAsync(SearchQuery));

                // Wait for all searches to complete
                var searchResults = await Task.WhenAll(searchTasks);

                // Combine and rank results
                foreach (var results in searchResults)
                {
                    allResults.AddRange(results);
                }

                // Sort by relevance
                var rankedResults = allResults
                    .OrderByDescending(r => CalculateRelevanceScore(r, SearchQuery))
                    .Take(100) // Limit to 100 results
                    .ToList();

                // Update UI
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var result in rankedResults)
                    {
                        SearchResults.Add(result);
                    }

                    SearchResultsCount = SearchResults.Count;
                    LastSearchTime = DateTime.Now;
                    SearchStatus = $"Found {SearchResultsCount} results";

                    // Update category counts
                    UpdateCategoryCounts();
                });

                // Save to recent searches
                SaveToRecentSearches(SearchQuery);

                // Publish search event
                _eventBus.Publish(new SearchPerformedEvent
                {
                    Query = SearchQuery,
                    ResultCount = SearchResultsCount,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                SearchStatus = $"Search error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"❌ Global search error: {ex.Message}");
            }
            finally
            {
                IsSearching = false;
            }
        }

        /// <summary>
        /// Search projects (Phase 2)
        /// </summary>
        private async Task<List<GlobalSearchResult>> SearchProjectsAsync(string query)
        {
            var results = new List<GlobalSearchResult>();

            try
            {
                await Task.Delay(200); // Simulate search time

                // Mock project search results
                var mockProjects = new[]
                {
                    "Project Alpha - Web Development",
                    "Project Beta - Mobile App", 
                    "Project Gamma - Data Analysis",
                    "Project Delta - System Integration",
                    "Project Epsilon - UI/UX Design"
                };

                foreach (var project in mockProjects)
                {
                    if (project.ToLower().Contains(query.ToLower()))
                    {
                        results.Add(new GlobalSearchResult
                        {
                            Id = $"project-{Guid.NewGuid()}",
                            Title = project,
                            Description = "Active project in development phase",
                            Category = "Projects",
                            Icon = "📋",
                            Phase = "Phase 2",
                            LastModified = DateTime.Now.AddDays(-new Random().Next(1, 30)),
                            Relevance = CalculateStringRelevance(project, query)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Project search error: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// Search tasks (Phase 2)
        /// </summary>
        private async Task<List<GlobalSearchResult>> SearchTasksAsync(string query)
        {
            var results = new List<GlobalSearchResult>();

            try
            {
                await Task.Delay(150);

                var mockTasks = new[]
                {
                    "Implement user authentication",
                    "Design database schema",
                    "Create API documentation",
                    "Setup CI/CD pipeline",
                    "Write unit tests",
                    "Performance optimization",
                    "Security audit review"
                };

                foreach (var task in mockTasks)
                {
                    if (task.ToLower().Contains(query.ToLower()))
                    {
                        results.Add(new GlobalSearchResult
                        {
                            Id = $"task-{Guid.NewGuid()}",
                            Title = task,
                            Description = "Task assigned to development team",
                            Category = "Tasks",
                            Icon = "✓",
                            Phase = "Phase 2",
                            LastModified = DateTime.Now.AddDays(-new Random().Next(1, 15)),
                            Relevance = CalculateStringRelevance(task, query)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Task search error: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// Search files (Phase 3)
        /// </summary>
        private async Task<List<GlobalSearchResult>> SearchFilesAsync(string query)
        {
            var results = new List<GlobalSearchResult>();

            try
            {
                await Task.Delay(180);

                var mockFiles = new[]
                {
                    "Requirements Document.docx",
                    "System Architecture.pdf",
                    "API Specification.json",
                    "Database Schema.sql",
                    "UI Mockups.fig",
                    "Test Reports.xlsx",
                    "Deployment Guide.md"
                };

                foreach (var file in mockFiles)
                {
                    if (file.ToLower().Contains(query.ToLower()))
                    {
                        results.Add(new GlobalSearchResult
                        {
                            Id = $"file-{Guid.NewGuid()}",
                            Title = file,
                            Description = "Project file in shared workspace",
                            Category = "Files",
                            Icon = "📁",
                            Phase = "Phase 3",
                            LastModified = DateTime.Now.AddDays(-new Random().Next(1, 20)),
                            Relevance = CalculateStringRelevance(file, query)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ File search error: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// Search users (Phase 1)
        /// </summary>
        private async Task<List<GlobalSearchResult>> SearchUsersAsync(string query)
        {
            var results = new List<GlobalSearchResult>();

            try
            {
                await Task.Delay(120);

                var mockUsers = new[]
                {
                    "John Doe - Project Manager",
                    "Jane Smith - Developer",
                    "Mike Johnson - Designer", 
                    "Sarah Wilson - QA Tester",
                    "David Brown - DevOps Engineer"
                };

                foreach (var user in mockUsers)
                {
                    if (user.ToLower().Contains(query.ToLower()))
                    {
                        results.Add(new GlobalSearchResult
                        {
                            Id = $"user-{Guid.NewGuid()}",
                            Title = user,
                            Description = "Team member with active assignments",
                            Category = "Users",
                            Icon = "👥",
                            Phase = "Phase 1",
                            LastModified = DateTime.Now.AddDays(-new Random().Next(1, 7)),
                            Relevance = CalculateStringRelevance(user, query)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ User search error: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// Search reports (Phase 4)
        /// </summary>
        private async Task<List<GlobalSearchResult>> SearchReportsAsync(string query)
        {
            var results = new List<GlobalSearchResult>();

            try
            {
                await Task.Delay(160);

                var mockReports = new[]
                {
                    "Monthly Performance Report",
                    "Project Status Dashboard",
                    "Team Productivity Analysis",
                    "Resource Utilization Report",
                    "Quality Metrics Summary"
                };

                foreach (var report in mockReports)
                {
                    if (report.ToLower().Contains(query.ToLower()))
                    {
                        results.Add(new GlobalSearchResult
                        {
                            Id = $"report-{Guid.NewGuid()}",
                            Title = report,
                            Description = "Analytics report with business insights",
                            Category = "Reports",
                            Icon = "📊",
                            Phase = "Phase 4",
                            LastModified = DateTime.Now.AddDays(-new Random().Next(1, 10)),
                            Relevance = CalculateStringRelevance(report, query)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Report search error: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// Search notifications (Phase 3)
        /// </summary>
        private async Task<List<GlobalSearchResult>> SearchNotificationsAsync(string query)
        {
            var results = new List<GlobalSearchResult>();

            try
            {
                await Task.Delay(100);

                var mockNotifications = new[]
                {
                    "Task deadline reminder",
                    "Project milestone achieved",
                    "New team member joined",
                    "System maintenance scheduled",
                    "Report generation completed"
                };

                foreach (var notification in mockNotifications)
                {
                    if (notification.ToLower().Contains(query.ToLower()))
                    {
                        results.Add(new GlobalSearchResult
                        {
                            Id = $"notification-{Guid.NewGuid()}",
                            Title = notification,
                            Description = "System notification for team updates",
                            Category = "Notifications",
                            Icon = "🔔",
                            Phase = "Phase 3",
                            LastModified = DateTime.Now.AddDays(-new Random().Next(1, 5)),
                            Relevance = CalculateStringRelevance(notification, query)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Notification search error: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// Calculate relevance score for search result
        /// </summary>
        private double CalculateRelevanceScore(GlobalSearchResult result, string query)
        {
            try
            {
                double score = 0;

                // Title relevance (highest weight)
                score += CalculateStringRelevance(result.Title, query) * 0.6;

                // Description relevance
                score += CalculateStringRelevance(result.Description, query) * 0.3;

                // Recency bonus
                var daysSinceModified = (DateTime.Now - result.LastModified).TotalDays;
                score += Math.Max(0, (30 - daysSinceModified) / 30) * 0.1;

                return score;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Calculate string relevance for search matching
        /// </summary>
        private double CalculateStringRelevance(string text, string query)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(query))
                return 0;

            var textLower = text.ToLower();
            var queryLower = query.ToLower();

            // Exact match
            if (textLower == queryLower) return 1.0;

            // Contains match
            if (textLower.Contains(queryLower)) return 0.8;

            // Word match
            var textWords = textLower.Split(' ');
            var queryWords = queryLower.Split(' ');
            var matchingWords = textWords.Intersect(queryWords).Count();
            var totalWords = queryWords.Length;

            return (double)matchingWords / totalWords * 0.6;
        }

        /// <summary>
        /// Update category counts after search
        /// </summary>
        private void UpdateCategoryCounts()
        {
            try
            {
                foreach (var category in SearchCategories)
                {
                    if (category.Id == "all")
                    {
                        category.Count = SearchResults.Count;
                    }
                    else
                    {
                        category.Count = SearchResults.Count(r => 
                            string.Equals(r.Category, category.Name, StringComparison.OrdinalIgnoreCase));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Category count update error: {ex.Message}");
            }
        }

        /// <summary>
        /// Filter results by selected category
        /// </summary>
        private void FilterResultsByCategory()
        {
            try
            {
                if (SelectedCategory == null || SelectedCategory.Id == "all")
                {
                    // Show all results
                    foreach (var result in SearchResults)
                    {
                        result.IsVisible = true;
                    }
                }
                else
                {
                    // Filter by category
                    foreach (var result in SearchResults)
                    {
                        result.IsVisible = string.Equals(result.Category, SelectedCategory.Name, 
                                                      StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Category filter error: {ex.Message}");
            }
        }

        // Get mock suggestions methods
        private List<string> GetProjectSuggestions(string query)
        {
            var suggestions = new List<string> { "project alpha", "project management", "project status" };
            return suggestions.Where(s => s.ToLower().StartsWith(query.ToLower())).ToList();
        }

        private List<string> GetTaskSuggestions(string query)
        {
            var suggestions = new List<string> { "task management", "task assignment", "task completion" };
            return suggestions.Where(s => s.ToLower().StartsWith(query.ToLower())).ToList();
        }

        private List<string> GetUserSuggestions(string query)
        {
            var suggestions = new List<string> { "user management", "user profile", "user permissions" };
            return suggestions.Where(s => s.ToLower().StartsWith(query.ToLower())).ToList();
        }

        private List<string> GetFileSuggestions(string query)
        {
            var suggestions = new List<string> { "file management", "file upload", "file sharing" };
            return suggestions.Where(s => s.ToLower().StartsWith(query.ToLower())).ToList();
        }

        #endregion

        #region Command Implementations

        private bool CanExecuteSearch()
        {
            return !string.IsNullOrWhiteSpace(SearchQuery) && !IsSearching;
        }

        private void ExecuteClearSearch()
        {
            SearchQuery = "";
            SearchResults.Clear();
            SearchResultsCount = 0;
            SearchStatus = "Search cleared";
            SelectedCategory = SearchCategories.FirstOrDefault(c => c.Id == "all");
        }

        private void ExecuteOpenResult(GlobalSearchResult result)
        {
            try
            {
                if (result == null) return;

                switch (result.Category.ToLower())
                {
                    case "projects":
                    case "tasks":
                        _navigationService.NavigateToTab("Projects");
                        break;
                    case "files":
                        _navigationService.NavigateToTab("Files");
                        break;
                    case "users":
                        _navigationService.NavigateToTab("Admin");
                        break;
                    case "reports":
                        _navigationService.NavigateToTab("Reports");
                        break;
                    case "notifications":
                        _navigationService.NavigateToTab("Notifications");
                        break;
                }

                // Publish navigation event
                _eventBus.Publish(new SearchResultOpenedEvent
                {
                    ResultId = result.Id,
                    Category = result.Category,
                    Title = result.Title,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Open result error: {ex.Message}");
            }
        }

        private void ExecuteAddToFavorites(GlobalSearchResult result)
        {
            try
            {
                if (result == null) return;

                // Mock add to favorites functionality
                System.Diagnostics.Debug.WriteLine($"⭐ Added to favorites: {result.Title}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Add to favorites error: {ex.Message}");
            }
        }

        private void ExecuteExportResults()
        {
            try
            {
                // Mock export functionality
                System.Diagnostics.Debug.WriteLine($"📄 Exporting {SearchResults.Count} search results...");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Export results error: {ex.Message}");
            }
        }

        private void ExecuteSelectSuggestion(string suggestion)
        {
            if (!string.IsNullOrWhiteSpace(suggestion))
            {
                SearchQuery = suggestion;
                _ = Task.Run(async () => await ExecuteSearchAsync());
            }
        }

        private void ExecuteSelectRecentSearch(string recentSearch)
        {
            if (!string.IsNullOrWhiteSpace(recentSearch))
            {
                SearchQuery = recentSearch;
                _ = Task.Run(async () => await ExecuteSearchAsync());
            }
        }

        private void ExecuteSelectCategory(GlobalSearchResult result)
        {
            try
            {
                if (result == null) return;

                // Navigate to appropriate phase based on result category
                SelectedCategory = SearchCategories.FirstOrDefault(c => c.Name.Equals(result.Category, StringComparison.OrdinalIgnoreCase));

                // Update search results visibility
                FilterResultsByCategory();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Select category error: {ex.Message}");
            }
        }

        private void ExecuteAdvancedSearch()
        {
            try
            {
                // Mock advanced search dialog
                System.Diagnostics.Debug.WriteLine("🔍 Opening advanced search options...");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Advanced search error: {ex.Message}");
            }
        }

        private void ExecuteSelectCategory(SearchCategory category)
        {
            try
            {
                if (category == null) return;

                // Unselect all other categories
                foreach (var cat in SearchCategories)
                {
                    cat.IsSelected = false;
                }

                // Select the chosen category
                category.IsSelected = true;
                SelectedCategory = category;

                // Filter will be applied automatically via the setter
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Category selection error: {ex.Message}");
            }
        }

        #endregion

        #region IDisposable

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Cleanup if needed
            }
            base.Dispose(disposing);
        }

        #endregion
    }

    #region Supporting Models

    /// <summary>
    /// Global search result
    /// </summary>
    public class GlobalSearchResult : INotifyPropertyChanged
    {
        private bool _isVisible = true;

        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Phase { get; set; } = "";
        public DateTime LastModified { get; set; }
        public double Relevance { get; set; }

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;
                OnPropertyChanged();
            }
        }

        public string LastModifiedText => LastModified.ToString("MMM dd, yyyy");

        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Search category
    /// </summary>
    public class SearchCategory : INotifyPropertyChanged
    {
        private bool _isSelected;
        private int _count;

        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "";

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public int Count
        {
            get => _count;
            set
            {
                _count = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        public string DisplayName => Count > 0 ? $"{Name} ({Count})" : Name;

        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Search performed event
    /// </summary>
    public class SearchPerformedEvent
    {
        public string Query { get; set; } = "";
        public int ResultCount { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Search result opened event
    /// </summary>
    public class SearchResultOpenedEvent
    {
        public string ResultId { get; set; } = "";
        public string Category { get; set; } = "";
        public string Title { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }

    #endregion
}