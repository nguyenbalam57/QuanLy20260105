using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using ManagementFile.App.Models;

namespace ManagementFile.App.Services
{
    /// <summary>
    /// Service for advanced search and filtering capabilities
    /// Phase 5 Week 14 - UX Enhancement & Advanced Features
    /// </summary>
    public sealed class AdvancedSearchService : INotifyPropertyChanged
    {

        #region Fields

        private readonly List<SearchIndex> _searchIndexes;
        private readonly List<SearchFilter> _savedFilters;
        private readonly Queue<SearchQuery> _searchHistory;
        private readonly Dictionary<string, List<SearchSuggestion>> _searchSuggestions;
        private readonly Dictionary<Type, List<object>> _dataCache;
        
        private string _currentQuery;
        private SearchFilter _activeFilter;
        private bool _isSearching;
        private int _searchResultsCount;
        private DateTime _lastSearchTime;
        private bool _disposed;

        private const int MAX_SEARCH_HISTORY = 50;
        private const int MAX_SUGGESTIONS = 10;
        private const int MIN_SEARCH_LENGTH = 2;

        #endregion

        #region Constructor

        public AdvancedSearchService()
        {
            _searchIndexes = new List<SearchIndex>();
            _savedFilters = new List<SearchFilter>();
            _searchHistory = new Queue<SearchQuery>();
            _searchSuggestions = new Dictionary<string, List<SearchSuggestion>>();
            _dataCache = new Dictionary<Type, List<object>>();

            InitializeDefaultFilters();
            InitializeSearchIndexes();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Current search query
        /// </summary>
        public string CurrentQuery
        {
            get => _currentQuery;
            set => SetProperty(ref _currentQuery, value);
        }

        /// <summary>
        /// Active search filter
        /// </summary>
        public SearchFilter ActiveFilter
        {
            get => _activeFilter;
            set => SetProperty(ref _activeFilter, value);
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
        /// Number of search results
        /// </summary>
        public int SearchResultsCount
        {
            get => _searchResultsCount;
            set => SetProperty(ref _searchResultsCount, value);
        }

        /// <summary>
        /// Last search time
        /// </summary>
        public DateTime LastSearchTime
        {
            get => _lastSearchTime;
            set => SetProperty(ref _lastSearchTime, value);
        }

        /// <summary>
        /// Saved search filters
        /// </summary>
        public IEnumerable<SearchFilter> SavedFilters => _savedFilters.AsEnumerable();

        /// <summary>
        /// Search history
        /// </summary>
        public IEnumerable<SearchQuery> SearchHistory => _searchHistory.AsEnumerable();

        /// <summary>
        /// Available search categories
        /// </summary>
        public IEnumerable<string> SearchCategories => _searchIndexes.Select(i => i.Category).Distinct();

        #endregion

        #region UI Helper Properties

        /// <summary>
        /// Search results count text
        /// </summary>
        public string SearchResultsText => $"{SearchResultsCount} results found";

        /// <summary>
        /// Last search time text
        /// </summary>
        public string LastSearchTimeText => LastSearchTime > DateTime.MinValue 
            ? $"Last searched: {LastSearchTime:HH:mm:ss}" 
            : "No recent searches";

        /// <summary>
        /// Has search results
        /// </summary>
        public bool HasSearchResults => SearchResultsCount > 0;

        /// <summary>
        /// Has search history
        /// </summary>
        public bool HasSearchHistory => _searchHistory.Any();

        /// <summary>
        /// Has saved filters
        /// </summary>
        public bool HasSavedFilters => _savedFilters.Any();

        #endregion

        #region Methods

        /// <summary>
        /// Initialize default search filters
        /// </summary>
        private void InitializeDefaultFilters()
        {
            _savedFilters.Add(new SearchFilter
            {
                Id = Guid.NewGuid(),
                Name = "All Items",
                Description = "Search across all data types",
                Categories = new List<string> { "Projects", "Tasks", "Files", "Users", "Reports" },
                IsDefault = true,
                CreatedAt = DateTime.Now
            });

            _savedFilters.Add(new SearchFilter
            {
                Id = Guid.NewGuid(),
                Name = "My Projects",
                Description = "Search in projects I'm involved in",
                Categories = new List<string> { "Projects", "Tasks" },
                UserId = "current_user", // Would be set from current user context
                IsDefault = false,
                CreatedAt = DateTime.Now
            });

            _savedFilters.Add(new SearchFilter
            {
                Id = Guid.NewGuid(),
                Name = "Recent Files",
                Description = "Search in recently accessed files",
                Categories = new List<string> { "Files" },
                DateRange = new DateRange 
                { 
                    StartDate = DateTime.Now.AddDays(-30), 
                    EndDate = DateTime.Now 
                },
                IsDefault = false,
                CreatedAt = DateTime.Now
            });

            ActiveFilter = _savedFilters.First(f => f.IsDefault);
        }

        /// <summary>
        /// Initialize search indexes
        /// </summary>
        private void InitializeSearchIndexes()
        {
            // Project search index
            _searchIndexes.Add(new SearchIndex
            {
                Id = Guid.NewGuid(),
                Category = "Projects",
                Fields = new List<string> { "Name", "Description", "Status", "Manager", "Department" },
                Weight = 1.0,
                IsEnabled = true
            });

            // Task search index
            _searchIndexes.Add(new SearchIndex
            {
                Id = Guid.NewGuid(),
                Category = "Tasks",
                Fields = new List<string> { "Title", "Description", "Status", "Priority", "AssignedTo", "Tags" },
                Weight = 1.0,
                IsEnabled = true
            });

            // File search index
            _searchIndexes.Add(new SearchIndex
            {
                Id = Guid.NewGuid(),
                Category = "Files",
                Fields = new List<string> { "FileName", "Description", "FileType", "Tags", "Path" },
                Weight = 0.8,
                IsEnabled = true
            });

            // User search index
            _searchIndexes.Add(new SearchIndex
            {
                Id = Guid.NewGuid(),
                Category = "Users",
                Fields = new List<string> { "FirstName", "LastName", "Email", "Department", "Role" },
                Weight = 0.6,
                IsEnabled = true
            });

            // Report search index
            _searchIndexes.Add(new SearchIndex
            {
                Id = Guid.NewGuid(),
                Category = "Reports",
                Fields = new List<string> { "ReportName", "ReportType", "Description", "GeneratedBy" },
                Weight = 0.5,
                IsEnabled = true
            });
        }

        /// <summary>
        /// Perform advanced search
        /// </summary>
        public async Task<SearchResults> SearchAsync(string query, SearchFilter filter = null)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < MIN_SEARCH_LENGTH)
            {
                return new SearchResults { Query = query, Results = new List<SearchResult>() };
            }

            try
            {
                IsSearching = true;
                CurrentQuery = query;
                var searchFilter = filter ?? ActiveFilter ?? _savedFilters.First(f => f.IsDefault);

                // Add to search history
                AddToSearchHistory(query, searchFilter);

                var results = new List<SearchResult>();

                // Search across different data types based on filter
                foreach (var category in searchFilter.Categories)
                {
                    var categoryResults = await SearchInCategoryAsync(query, category, searchFilter);
                    results.AddRange(categoryResults);
                }

                // Sort results by relevance score
                results = results.OrderByDescending(r => r.RelevanceScore)
                               .ThenByDescending(r => r.LastModified)
                               .Take(100) // Limit results
                               .ToList();

                SearchResultsCount = results.Count;
                LastSearchTime = DateTime.Now;

                // Update search suggestions
                await UpdateSearchSuggestionsAsync(query);

                return new SearchResults
                {
                    Query = query,
                    Filter = searchFilter,
                    Results = results,
                    TotalCount = results.Count,
                    SearchTime = DateTime.Now,
                    Categories = results.GroupBy(r => r.Category)
                                       .ToDictionary(g => g.Key, g => g.Count())
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}");
                return new SearchResults { Query = query, Results = new List<SearchResult>(), Error = ex.Message };
            }
            finally
            {
                IsSearching = false;
            }
        }

        /// <summary>
        /// Search in specific category
        /// </summary>
        private async Task<List<SearchResult>> SearchInCategoryAsync(string query, string category, SearchFilter filter)
        {
            var results = new List<SearchResult>();
            var index = _searchIndexes.FirstOrDefault(i => i.Category == category && i.IsEnabled);
            
            if (index == null) return results;

            await Task.Delay(10); // Simulate async operation

            // Mock search results based on category
            switch (category.ToLower())
            {
                case "projects":
                    results.AddRange(SearchProjects(query, index, filter));
                    break;
                case "tasks":
                    results.AddRange(SearchTasks(query, index, filter));
                    break;
                case "files":
                    results.AddRange(SearchFiles(query, index, filter));
                    break;
                case "users":
                    results.AddRange(SearchUsers(query, index, filter));
                    break;
                case "reports":
                    results.AddRange(SearchReports(query, index, filter));
                    break;
            }

            return results;
        }

        /// <summary>
        /// Search projects
        /// </summary>
        private List<SearchResult> SearchProjects(string query, SearchIndex index, SearchFilter filter)
        {
            var results = new List<SearchResult>();
            var mockProjects = GetMockProjects();

            foreach (var project in mockProjects)
            {
                var score = CalculateRelevanceScore(query, project, index.Fields);
                if (score > 0)
                {
                    results.Add(new SearchResult
                    {
                        Id = project.Id,
                        Title = project.Name,
                        Description = project.Description,
                        Category = "Projects",
                        Type = "Project",
                        RelevanceScore = score * index.Weight,
                        LastModified = project.LastModified,
                        Path = $"/projects/{project.Id}",
                        Icon = "📊",
                        Metadata = new Dictionary<string, string>
                        {
                            { "Status", project.Status },
                            { "Manager", project.Manager },
                            { "Department", project.Department }
                        }
                    });
                }
            }

            return results;
        }

        /// <summary>
        /// Search tasks
        /// </summary>
        private List<SearchResult> SearchTasks(string query, SearchIndex index, SearchFilter filter)
        {
            var results = new List<SearchResult>();
            var mockTasks = GetMockTasks();

            foreach (var task in mockTasks)
            {
                var score = CalculateRelevanceScore(query, task, index.Fields);
                if (score > 0)
                {
                    results.Add(new SearchResult
                    {
                        Id = task.Id,
                        Title = task.Title,
                        Description = task.Description,
                        Category = "Tasks",
                        Type = "Task",
                        RelevanceScore = score * index.Weight,
                        LastModified = task.LastModified,
                        Path = $"/tasks/{task.Id}",
                        Icon = "📋",
                        Metadata = new Dictionary<string, string>
                        {
                            { "Status", task.Status },
                            { "Priority", task.Priority },
                            { "AssignedTo", task.AssignedTo }
                        }
                    });
                }
            }

            return results;
        }

        /// <summary>
        /// Search files
        /// </summary>
        private List<SearchResult> SearchFiles(string query, SearchIndex index, SearchFilter filter)
        {
            var results = new List<SearchResult>();
            var mockFiles = GetMockFiles();

            foreach (var file in mockFiles)
            {
                var score = CalculateRelevanceScore(query, file, index.Fields);
                if (score > 0)
                {
                    results.Add(new SearchResult
                    {
                        Id = file.Id,
                        Title = file.FileName,
                        Description = file.Description,
                        Category = "Files",
                        Type = "File",
                        RelevanceScore = score * index.Weight,
                        LastModified = file.LastModified,
                        Path = file.Path,
                        Icon = GetFileIcon(file.FileType),
                        Metadata = new Dictionary<string, string>
                        {
                            { "FileType", file.FileType },
                            { "Size", file.Size.ToString() },
                            { "CreatedBy", file.CreatedBy }
                        }
                    });
                }
            }

            return results;
        }

        /// <summary>
        /// Search users
        /// </summary>
        private List<SearchResult> SearchUsers(string query, SearchIndex index, SearchFilter filter)
        {
            var results = new List<SearchResult>();
            var mockUsers = GetMockUsers();

            foreach (var user in mockUsers)
            {
                var score = CalculateRelevanceScore(query, user, index.Fields);
                if (score > 0)
                {
                    results.Add(new SearchResult
                    {
                        Id = user.Id,
                        Title = $"{user.FirstName} {user.LastName}",
                        Description = user.Email,
                        Category = "Users",
                        Type = "User",
                        RelevanceScore = score * index.Weight,
                        LastModified = user.LastLogin,
                        Path = $"/users/{user.Id}",
                        Icon = "👤",
                        Metadata = new Dictionary<string, string>
                        {
                            { "Department", user.Department },
                            { "Role", user.Role },
                            { "Status", user.IsActive ? "Active" : "Inactive" }
                        }
                    });
                }
            }

            return results;
        }

        /// <summary>
        /// Search reports
        /// </summary>
        private List<SearchResult> SearchReports(string query, SearchIndex index, SearchFilter filter)
        {
            var results = new List<SearchResult>();
            var mockReports = GetMockReports();

            foreach (var report in mockReports)
            {
                var score = CalculateRelevanceScore(query, report, index.Fields);
                if (score > 0)
                {
                    results.Add(new SearchResult
                    {
                        Id = report.Id,
                        Title = report.ReportName,
                        Description = report.Description,
                        Category = "Reports",
                        Type = "Report",
                        RelevanceScore = score * index.Weight,
                        LastModified = report.GeneratedAt,
                        Path = $"/reports/{report.Id}",
                        Icon = "📊",
                        Metadata = new Dictionary<string, string>
                        {
                            { "ReportType", report.ReportType },
                            { "GeneratedBy", report.GeneratedBy },
                            { "Status", "Available" }
                        }
                    });
                }
            }

            return results;
        }

        /// <summary>
        /// Calculate relevance score for search result
        /// </summary>
        private double CalculateRelevanceScore(string query, object item, List<string> searchFields)
        {
            double totalScore = 0;
            var queryWords = query.ToLower().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var field in searchFields)
            {
                var fieldValue = GetFieldValue(item, field)?.ToLower() ?? string.Empty;
                
                if (string.IsNullOrEmpty(fieldValue)) continue;

                foreach (var word in queryWords)
                {
                    // Exact match gets highest score
                    if (fieldValue.Contains(word))
                    {
                        if (fieldValue.StartsWith(word))
                            totalScore += 10; // Prefix match
                        else if (fieldValue.Contains($" {word}"))
                            totalScore += 8; // Word boundary match
                        else
                            totalScore += 5; // Contains match
                    }

                    // Fuzzy match for slight variations
                    if (CalculateLevenshteinDistance(word, fieldValue) <= 2 && word.Length > 3)
                    {
                        totalScore += 3;
                    }
                }
            }

            return totalScore;
        }

        /// <summary>
        /// Get field value from object using reflection
        /// </summary>
        private string GetFieldValue(object item, string fieldName)
        {
            try
            {
                var property = item.GetType().GetProperty(fieldName);
                return property?.GetValue(item)?.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Calculate Levenshtein distance for fuzzy matching
        /// </summary>
        private int CalculateLevenshteinDistance(string source, string target)
        {
            if (string.IsNullOrEmpty(source)) return target?.Length ?? 0;
            if (string.IsNullOrEmpty(target)) return source.Length;

            var sourceLength = source.Length;
            var targetLength = target.Length;
            var distance = new int[sourceLength + 1, targetLength + 1];

            for (int i = 0; i <= sourceLength; distance[i, 0] = i++) { }
            for (int j = 0; j <= targetLength; distance[0, j] = j++) { }

            for (int i = 1; i <= sourceLength; i++)
            {
                for (int j = 1; j <= targetLength; j++)
                {
                    var cost = target[j - 1] == source[i - 1] ? 0 : 1;
                    distance[i, j] = Math.Min(Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1), distance[i - 1, j - 1] + cost);
                }
            }

            return distance[sourceLength, targetLength];
        }

        /// <summary>
        /// Get file icon based on file type
        /// </summary>
        private string GetFileIcon(string fileType)
        {
            if (string.IsNullOrEmpty(fileType)) return "📁";
            
            var lowerType = fileType.ToLower();
            if (lowerType == "pdf") return "📄";
            if (lowerType == "doc" || lowerType == "docx") return "📝";
            if (lowerType == "xls" || lowerType == "xlsx") return "📊";
            if (lowerType == "ppt" || lowerType == "pptx") return "📋";
            if (lowerType == "jpg" || lowerType == "png" || lowerType == "gif") return "🖼️";
            if (lowerType == "mp4" || lowerType == "avi" || lowerType == "mov") return "🎥";
            if (lowerType == "mp3" || lowerType == "wav") return "🎵";
            if (lowerType == "zip" || lowerType == "rar") return "📦";
            
            return "📁";
        }

        /// <summary>
        /// Add query to search history
        /// </summary>
        private void AddToSearchHistory(string query, SearchFilter filter)
        {
            var searchQuery = new SearchQuery
            {
                Query = query,
                Filter = filter,
                SearchTime = DateTime.Now,
                ResultCount = 0 // Will be updated after search
            };

            _searchHistory.Enqueue(searchQuery);

            // Keep only recent history
            while (_searchHistory.Count > MAX_SEARCH_HISTORY)
            {
                _searchHistory.Dequeue();
            }

            OnPropertyChanged(nameof(SearchHistory));
            OnPropertyChanged(nameof(HasSearchHistory));
        }

        /// <summary>
        /// Update search suggestions
        /// </summary>
        private async Task UpdateSearchSuggestionsAsync(string query)
        {
            await Task.Delay(10); // Simulate async operation

            var suggestions = new List<SearchSuggestion>();

            // Add suggestions based on search history
            var historySuggestions = _searchHistory
                .Where(h => h.Query.ToLower().Contains(query.ToLower()) && h.Query != query)
                .Take(5)
                .Select(h => new SearchSuggestion
                {
                    Text = h.Query,
                    Type = "History",
                    Icon = "🕒",
                    Score = 0.8
                })
                .ToList();

            suggestions.AddRange(historySuggestions);

            // Add category-based suggestions
            var categorySuggestions = SearchCategories
                .Where(c => c.ToLower().Contains(query.ToLower()))
                .Take(3)
                .Select(c => new SearchSuggestion
                {
                    Text = c,
                    Type = "Category",
                    Icon = "📁",
                    Score = 0.6
                })
                .ToList();

            suggestions.AddRange(categorySuggestions);

            // Store suggestions
            _searchSuggestions[query] = suggestions.OrderByDescending(s => s.Score).Take(MAX_SUGGESTIONS).ToList();
        }

        /// <summary>
        /// Get search suggestions for query
        /// </summary>
        public async Task<List<SearchSuggestion>> GetSuggestionsAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < MIN_SEARCH_LENGTH)
            {
                return new List<SearchSuggestion>();
            }

            await Task.Delay(10); // Simulate async operation

            return _searchSuggestions.ContainsKey(query) ? _searchSuggestions[query] : new List<SearchSuggestion>();
        }

        /// <summary>
        /// Save search filter
        /// </summary>
        public void SaveFilter(SearchFilter filter)
        {
            if (filter == null) return;

            filter.Id = Guid.NewGuid();
            filter.CreatedAt = DateTime.Now;
            _savedFilters.Add(filter);

            OnPropertyChanged(nameof(SavedFilters));
            OnPropertyChanged(nameof(HasSavedFilters));
        }

        /// <summary>
        /// Delete search filter
        /// </summary>
        public void DeleteFilter(Guid filterId)
        {
            var filter = _savedFilters.FirstOrDefault(f => f.Id == filterId);
            if (filter != null && !filter.IsDefault)
            {
                _savedFilters.Remove(filter);
                OnPropertyChanged(nameof(SavedFilters));
                OnPropertyChanged(nameof(HasSavedFilters));
            }
        }

        /// <summary>
        /// Clear search history
        /// </summary>
        public void ClearSearchHistory()
        {
            _searchHistory.Clear();
            OnPropertyChanged(nameof(SearchHistory));
            OnPropertyChanged(nameof(HasSearchHistory));
        }

        /// <summary>
        /// Clear search suggestions
        /// </summary>
        public void ClearSearchSuggestions()
        {
            _searchSuggestions.Clear();
        }

        #endregion

        #region Mock Data Methods

        private List<MockProject> GetMockProjects()
        {
            return new List<MockProject>
            {
                new MockProject { Id = "1", Name = "Website Redesign", Description = "Modern website redesign project", Status = "In Progress", Manager = "John Doe", Department = "IT", LastModified = DateTime.Now.AddDays(-2) },
                new MockProject { Id = "2", Name = "Mobile App Development", Description = "iOS and Android mobile application", Status = "Planning", Manager = "Jane Smith", Department = "Development", LastModified = DateTime.Now.AddDays(-1) },
                new MockProject { Id = "3", Name = "Database Migration", Description = "Legacy system database migration", Status = "Completed", Manager = "Mike Johnson", Department = "IT", LastModified = DateTime.Now.AddDays(-5) },
                new MockProject { Id = "4", Name = "Security Audit", Description = "Comprehensive security assessment", Status = "In Progress", Manager = "Sarah Wilson", Department = "Security", LastModified = DateTime.Now.AddHours(-6) },
                new MockProject { Id = "5", Name = "Performance Optimization", Description = "System performance improvements", Status = "Planning", Manager = "Tom Brown", Department = "IT", LastModified = DateTime.Now.AddDays(-3) }
            };
        }

        private List<MockTask> GetMockTasks()
        {
            return new List<MockTask>
            {
                new MockTask { Id = "1", Title = "UI Design Review", Description = "Review new user interface designs", Status = "In Progress", Priority = "High", AssignedTo = "Alice Cooper", LastModified = DateTime.Now.AddHours(-2) },
                new MockTask { Id = "2", Title = "Database Schema Update", Description = "Update database schema for new features", Status = "Pending", Priority = "Medium", AssignedTo = "Bob Taylor", LastModified = DateTime.Now.AddDays(-1) },
                new MockTask { Id = "3", Title = "API Integration", Description = "Integrate third-party payment API", Status = "Completed", Priority = "High", AssignedTo = "Charlie Davis", LastModified = DateTime.Now.AddDays(-4) },
                new MockTask { Id = "4", Title = "Testing Phase", Description = "Comprehensive testing of new features", Status = "In Progress", Priority = "Medium", AssignedTo = "Diana Prince", LastModified = DateTime.Now.AddHours(-8) },
                new MockTask { Id = "5", Title = "Documentation Update", Description = "Update technical documentation", Status = "Pending", Priority = "Low", AssignedTo = "Edward Norton", LastModified = DateTime.Now.AddDays(-2) }
            };
        }

        private List<MockFile> GetMockFiles()
        {
            return new List<MockFile>
            {
                new MockFile { Id = "1", FileName = "Project_Requirements.docx", Description = "Project requirements document", FileType = "docx", Size = 2048, Path = "/projects/1/documents/", CreatedBy = "John Doe", LastModified = DateTime.Now.AddDays(-1) },
                new MockFile { Id = "2", FileName = "Design_Mockups.pdf", Description = "UI design mockups and wireframes", FileType = "pdf", Size = 15360, Path = "/projects/1/designs/", CreatedBy = "Jane Smith", LastModified = DateTime.Now.AddHours(-6) },
                new MockFile { Id = "3", FileName = "Database_Schema.sql", Description = "Database schema definition", FileType = "sql", Size = 1024, Path = "/projects/3/database/", CreatedBy = "Mike Johnson", LastModified = DateTime.Now.AddDays(-3) },
                new MockFile { Id = "4", FileName = "Test_Results.xlsx", Description = "Testing results and metrics", FileType = "xlsx", Size = 4096, Path = "/projects/4/testing/", CreatedBy = "Diana Prince", LastModified = DateTime.Now.AddHours(-4) },
                new MockFile { Id = "5", FileName = "Security_Report.pdf", Description = "Security audit findings", FileType = "pdf", Size = 8192, Path = "/projects/4/reports/", CreatedBy = "Sarah Wilson", LastModified = DateTime.Now.AddDays(-2) }
            };
        }

        private List<MockUser> GetMockUsers()
        {
            return new List<MockUser>
            {
                new MockUser { Id = "1", FirstName = "John", LastName = "Doe", Email = "john.doe@company.com", Department = "IT", Role = "Manager", IsActive = true, LastLogin = DateTime.Now.AddDays(-1) },
                new MockUser { Id = "2", FirstName = "Jane", LastName = "Smith", Email = "jane.smith@company.com", Department = "Development", Role = "Developer", IsActive = true, LastLogin = DateTime.Now.AddHours(-2) },
                new MockUser { Id = "3", FirstName = "Mike", LastName = "Johnson", Email = "mike.johnson@company.com", Department = "IT", Role = "Admin", IsActive = true, LastLogin = DateTime.Now.AddDays(-2) },
                new MockUser { Id = "4", FirstName = "Sarah", LastName = "Wilson", Email = "sarah.wilson@company.com", Department = "Security", Role = "Analyst", IsActive = true, LastLogin = DateTime.Now.AddHours(-6) },
                new MockUser { Id = "5", FirstName = "Tom", LastName = "Brown", Email = "tom.brown@company.com", Department = "IT", Role = "Developer", IsActive = false, LastLogin = DateTime.Now.AddDays(-10) }
            };
        }

        private List<MockReport> GetMockReports()
        {
            return new List<MockReport>
            {
                new MockReport { Id = "1", ReportName = "Project Progress Report", Description = "Monthly project progress summary", ReportType = "Project", GeneratedBy = "System", GeneratedAt = DateTime.Now.AddDays(-1) },
                new MockReport { Id = "2", ReportName = "Team Productivity Analysis", Description = "Team performance metrics", ReportType = "Analytics", GeneratedBy = "John Doe", GeneratedAt = DateTime.Now.AddHours(-8) },
                new MockReport { Id = "3", ReportName = "Security Audit Summary", Description = "Security assessment results", ReportType = "Security", GeneratedBy = "Sarah Wilson", GeneratedAt = DateTime.Now.AddDays(-3) },
                new MockReport { Id = "4", ReportName = "File Usage Statistics", Description = "File access and usage patterns", ReportType = "Usage", GeneratedBy = "System", GeneratedAt = DateTime.Now.AddHours(-12) },
                new MockReport { Id = "5", ReportName = "Performance Metrics", Description = "System performance analysis", ReportType = "Performance", GeneratedBy = "Mike Johnson", GeneratedAt = DateTime.Now.AddDays(-2) }
            };
        }

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

    #region Search Models

    /// <summary>
    /// Search filter model
    /// </summary>
    public class SearchFilter
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public List<string> Categories { get; set; } = new List<string>();
        public string UserId { get; set; } = "";
        public DateRange DateRange { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Date range model
    /// </summary>
    public class DateRange
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    /// <summary>
    /// Search index model
    /// </summary>
    public class SearchIndex
    {
        public Guid Id { get; set; }
        public string Category { get; set; } = "";
        public List<string> Fields { get; set; } = new List<string>();
        public double Weight { get; set; } = 1.0;
        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// Search query model
    /// </summary>
    public class SearchQuery
    {
        public string Query { get; set; } = "";
        public SearchFilter Filter { get; set; }
        public DateTime SearchTime { get; set; }
        public int ResultCount { get; set; }
    }

    /// <summary>
    /// Search results model
    /// </summary>
    public class SearchResults
    {
        public string Query { get; set; } = "";
        public SearchFilter Filter { get; set; }
        public List<SearchResult> Results { get; set; } = new List<SearchResult>();
        public int TotalCount { get; set; }
        public DateTime SearchTime { get; set; }
        public Dictionary<string, int> Categories { get; set; } = new Dictionary<string, int>();
        public string Error { get; set; } = "";
    }

    /// <summary>
    /// Search result model
    /// </summary>
    public class SearchResult
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public string Type { get; set; } = "";
        public double RelevanceScore { get; set; }
        public DateTime LastModified { get; set; }
        public string Path { get; set; } = "";
        public string Icon { get; set; } = "";
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Search suggestion model
    /// </summary>
    public class SearchSuggestion
    {
        public string Text { get; set; } = "";
        public string Type { get; set; } = "";
        public string Icon { get; set; } = "";
        public double Score { get; set; }
    }

    #endregion

    #region Mock Models

    public class MockProject
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Status { get; set; } = "";
        public string Manager { get; set; } = "";
        public string Department { get; set; } = "";
        public DateTime LastModified { get; set; }
    }

    public class MockTask
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Status { get; set; } = "";
        public string Priority { get; set; } = "";
        public string AssignedTo { get; set; } = "";
        public DateTime LastModified { get; set; }
    }

    public class MockFile
    {
        public string Id { get; set; } = "";
        public string FileName { get; set; } = "";
        public string Description { get; set; } = "";
        public string FileType { get; set; } = "";
        public long Size { get; set; }
        public string Path { get; set; } = "";
        public string CreatedBy { get; set; } = "";
        public DateTime LastModified { get; set; }
    }

    public class MockUser
    {
        public string Id { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Department { get; set; } = "";
        public string Role { get; set; } = "";
        public bool IsActive { get; set; }
        public DateTime LastLogin { get; set; }
    }

    public class MockReport
    {
        public string Id { get; set; } = "";
        public string ReportName { get; set; } = "";
        public string Description { get; set; } = "";
        public string ReportType { get; set; } = "";
        public string GeneratedBy { get; set; } = "";
        public DateTime GeneratedAt { get; set; }
    }

    #endregion
}