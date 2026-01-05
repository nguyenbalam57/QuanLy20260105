using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace ManagementFile.App.Services
{
    /// <summary>
    /// Service quản lý cache dữ liệu chia sẻ giữa các phases
    /// Cung cấp caching thống nhất cho ManagementFile Enterprise Platform
    /// </summary>
    public sealed class DataCache : IDisposable
    {
        #region Constructor for DI

        public DataCache()
        {
            _cache = new ConcurrentDictionary<string, CacheItem>();
            _cacheStats = new CacheStatistics();
            StartBackgroundCleanup();
        }
        #endregion

        #region Private Fields
        private readonly ConcurrentDictionary<string, CacheItem> _cache;
        private readonly CacheStatistics _cacheStats;
        private System.Threading.Timer _cleanupTimer;
        #endregion

        #region Background Tasks

        /// <summary>
        /// Khởi động background cleanup task
        /// </summary>
        public void StartBackgroundCleanup()
        {
            _cleanupTimer = new System.Threading.Timer(
                callback: (_) => CleanupExpiredItems(),
                state: null,
                dueTime: TimeSpan.FromMinutes(5),
                period: TimeSpan.FromMinutes(10)
            );

            System.Diagnostics.Debug.WriteLine("🕐 Background cache cleanup started");
        }

        #endregion

        #region Cache Operations

        /// <summary>
        /// Thêm item vào cache
        /// </summary>
        /// <typeparam name="T">Loại dữ liệu</typeparam>
        /// <param name="key">Key của cache item</param>
        /// <param name="value">Giá trị cần cache</param>
        /// <param name="expiration">Thời gian hết hạn (optional)</param>
        public void Set<T>(string key, T value, TimeSpan? expiration = null)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key không được rỗng", nameof(key));

            var cacheItem = new CacheItem
            {
                Key = key,
                Value = value,
                CreatedAt = DateTime.Now,
                ExpiresAt = expiration.HasValue ? DateTime.Now.Add(expiration.Value) : DateTime.MaxValue,
                LastAccessedAt = DateTime.Now,
                AccessCount = 0,
                DataType = typeof(T).Name
            };

            var isUpdate = _cache.ContainsKey(key);
            _cache.AddOrUpdate(key, cacheItem, (k, existing) =>
            {
                existing.Value = value;
                existing.LastAccessedAt = DateTime.Now;
                existing.AccessCount++;
                return existing;
            });

            // Update statistics
            if (isUpdate)
            {
                _cacheStats.UpdateCount++;
                System.Diagnostics.Debug.WriteLine($"🔄 Cache updated - Key: {key}");
            }
            else
            {
                _cacheStats.AddCount++;
                System.Diagnostics.Debug.WriteLine($"➕ Cache added - Key: {key}");
            }
        }

        /// <summary>
        /// Lấy item từ cache
        /// </summary>
        /// <typeparam name="T">Loại dữ liệu mong đợi</typeparam>
        /// <param name="key">Key của cache item</param>
        /// <returns>Giá trị hoặc default(T) nếu không tìm thấy</returns>
        public T Get<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
                return default(T);

            if (_cache.TryGetValue(key, out var cacheItem))
            {
                // Kiểm tra expiration
                if (cacheItem.ExpiresAt <= DateTime.Now)
                {
                    Remove(key);
                    System.Diagnostics.Debug.WriteLine($"⏰ Cache expired - Key: {key}");
                    return default(T);
                }

                // Update access info
                cacheItem.LastAccessedAt = DateTime.Now;
                cacheItem.AccessCount++;
                _cacheStats.HitCount++;

                try
                {
                    return (T)cacheItem.Value;
                }
                catch (InvalidCastException)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Cache type mismatch - Key: {key}, Expected: {typeof(T).Name}, Actual: {cacheItem.DataType}");
                    return default(T);
                }
            }

            _cacheStats.MissCount++;
            System.Diagnostics.Debug.WriteLine($"❓ Cache miss - Key: {key}");
            return default(T);
        }

        /// <summary>
        /// Kiểm tra xem key có tồn tại trong cache không
        /// </summary>
        /// <param name="key">Key cần kiểm tra</param>
        /// <returns>True nếu tồn tại và chưa hết hạn</returns>
        public bool Contains(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            if (_cache.TryGetValue(key, out var cacheItem))
            {
                if (cacheItem.ExpiresAt <= DateTime.Now)
                {
                    Remove(key);
                    return false;
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Xóa item khỏi cache
        /// </summary>
        /// <param name="key">Key cần xóa</param>
        /// <returns>True nếu xóa thành công</returns>
        public bool Remove(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            if (_cache.TryRemove(key, out var removedItem))
            {
                _cacheStats.RemoveCount++;
                System.Diagnostics.Debug.WriteLine($"🗑️ Cache removed - Key: {key}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Lấy hoặc tạo cache item bằng factory function
        /// </summary>
        /// <typeparam name="T">Loại dữ liệu</typeparam>
        /// <param name="key">Key của cache item</param>
        /// <param name="factory">Function tạo dữ liệu nếu không có trong cache</param>
        /// <param name="expiration">Thời gian hết hạn (optional)</param>
        /// <returns>Giá trị từ cache hoặc factory</returns>
        public T GetOrAdd<T>(string key, Func<T> factory, TimeSpan? expiration = null)
        {
            var cachedValue = Get<T>(key);
            if (!EqualityComparer<T>.Default.Equals(cachedValue, default(T)))
            {
                return cachedValue;
            }

            var newValue = factory();
            Set(key, newValue, expiration);
            return newValue;
        }

        #endregion

        #region Specialized Cache Methods (Cross-Phase Data)

        /// <summary>
        /// Cache thông tin user hiện tại
        /// </summary>
        /// <param name="user">User object</param>
        public void SetCurrentUser(object user)
        {
            Set("CurrentUser", user, TimeSpan.FromHours(8)); // 8 hours
        }

        /// <summary>
        /// Lấy thông tin user hiện tại
        /// </summary>
        /// <returns>User object hoặc null</returns>
        public T GetCurrentUser<T>() where T : class
        {
            return Get<T>("CurrentUser");
        }

        /// <summary>
        /// Cache project được chọn (để chia sẻ giữa phases)
        /// </summary>
        /// <param name="project">Project object</param>
        public void SetSelectedProject(object project)
        {
            Set("SelectedProject", project, TimeSpan.FromHours(2)); // 2 hours
        }

        /// <summary>
        /// Lấy project được chọn
        /// </summary>
        /// <returns>Project object hoặc null</returns>
        public T GetSelectedProject<T>() where T : class
        {
            return Get<T>("SelectedProject");
        }

        /// <summary>
        /// Cache search history
        /// </summary>
        /// <param name="searchHistory">Search history list</param>
        public void SetSearchHistory(List<string> searchHistory)
        {
            Set("SearchHistory", searchHistory, TimeSpan.FromDays(7)); // 7 days
        }

        /// <summary>
        /// Lấy search history
        /// </summary>
        /// <returns>Search history list</returns>
        public List<string> GetSearchHistory()
        {
            return Get<List<string>>("SearchHistory") ?? new List<string>();
        }

        /// <summary>
        /// Cache performance metrics từ Phase 5
        /// </summary>
        /// <param name="metrics">Performance metrics object</param>
        public void SetPerformanceMetrics(object metrics)
        {
            Set("PerformanceMetrics", metrics, TimeSpan.FromMinutes(5)); // 5 minutes
        }

        /// <summary>
        /// Lấy performance metrics
        /// </summary>
        /// <returns>Performance metrics object</returns>
        public T GetPerformanceMetrics<T>() where T : class
        {
            return Get<T>("PerformanceMetrics");
        }

        /// <summary>
        /// Cache user preferences
        /// </summary>
        /// <param name="preferences">User preferences object</param>
        public void SetUserPreferences(object preferences)
        {
            Set("UserPreferences", preferences, TimeSpan.FromDays(30)); // 30 days
        }

        /// <summary>
        /// Lấy user preferences
        /// </summary>
        /// <returns>User preferences object</returns>
        public T GetUserPreferences<T>() where T : class
        {
            return Get<T>("UserPreferences");
        }

        #endregion

        #region Cache Management

        /// <summary>
        /// Xóa tất cả items đã hết hạn
        /// </summary>
        public void CleanupExpiredItems()
        {
            var expiredKeys = new List<string>();
            var now = DateTime.Now;

            foreach (var kvp in _cache)
            {
                if (kvp.Value.ExpiresAt <= now)
                {
                    expiredKeys.Add(kvp.Key);
                }
            }

            foreach (var key in expiredKeys)
            {
                Remove(key);
            }

            if (expiredKeys.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"🧹 Cleaned up {expiredKeys.Count} expired cache items");
            }
        }

        /// <summary>
        /// Xóa tất cả items trong cache
        /// </summary>
        public void Clear()
        {
            var count = _cache.Count;
            _cache.Clear();
            _cacheStats.ClearCount++;
            System.Diagnostics.Debug.WriteLine($"🗑️ Cleared all cache items ({count} items)");
        }

        /// <summary>
        /// Xóa cache items theo pattern
        /// </summary>
        /// <param name="pattern">Pattern để match keys (supports * wildcard)</param>
        public void ClearByPattern(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return;

            var keysToRemove = new List<string>();
            var regexPattern = pattern.Replace("*", ".*");
            var regex = new System.Text.RegularExpressions.Regex(regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            foreach (var key in _cache.Keys)
            {
                if (regex.IsMatch(key))
                {
                    keysToRemove.Add(key);
                }
            }

            foreach (var key in keysToRemove)
            {
                Remove(key);
            }

            if (keysToRemove.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"🗑️ Removed {keysToRemove.Count} cache items matching pattern: {pattern}");
            }
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Lấy thống kê cache
        /// </summary>
        /// <returns>Cache statistics object</returns>
        public CacheStatistics GetStatistics()
        {
            _cacheStats.CurrentItemCount = _cache.Count;
            _cacheStats.LastUpdated = DateTime.Now;
            
            // Tính hit rate
            var totalRequests = _cacheStats.HitCount + _cacheStats.MissCount;
            _cacheStats.HitRate = totalRequests > 0 ? (double)_cacheStats.HitCount / totalRequests * 100 : 0;

            return _cacheStats;
        }

        /// <summary>
        /// Lấy thông tin chi tiết về cache items
        /// </summary>
        /// <returns>Danh sách cache item info</returns>
        public List<CacheItemInfo> GetCacheItemsInfo()
        {
            var items = new List<CacheItemInfo>();

            foreach (var kvp in _cache)
            {
                items.Add(new CacheItemInfo
                {
                    Key = kvp.Key,
                    DataType = kvp.Value.DataType,
                    CreatedAt = kvp.Value.CreatedAt,
                    LastAccessedAt = kvp.Value.LastAccessedAt,
                    AccessCount = kvp.Value.AccessCount,
                    ExpiresAt = kvp.Value.ExpiresAt,
                    IsExpired = kvp.Value.ExpiresAt <= DateTime.Now,
                    Size = EstimateObjectSize(kvp.Value.Value)
                });
            }

            return items;
        }

        /// <summary>
        /// Ước tính kích thước object (đơn giản)
        /// </summary>
        private long EstimateObjectSize(object obj)
        {
            if (obj == null) return 0;
            
            try
            {
                // Ước tính đơn giản dựa trên type
                var type = obj.GetType();
                if (type == typeof(string))
                    return ((string)obj).Length * 2; // Unicode characters
                else if (type.IsValueType)
                    return System.Runtime.InteropServices.Marshal.SizeOf(type);
                else
                    return 100; // Ước tính cho reference types
            }
            catch
            {
                return 50; // Default estimate
            }
        }

        #endregion


        #region Cleanup

        /// <summary>
        /// Dọn dẹp DataCache
        /// </summary>
        public void Cleanup()
        {
            try
            {
                Clear();
                System.Diagnostics.Debug.WriteLine("🧹 DataCache đã được dọn dẹp");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi khi dọn dẹp DataCache: {ex.Message}");
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
            Clear();
        }

        #endregion

        #region Supporting Classes - Moved to public

        /// <summary>
        /// Đại diện cho một cache item
        /// </summary>
        public class CacheItem
        {
            public string Key { get; set; } = "";
            public object Value { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime ExpiresAt { get; set; }
            public DateTime LastAccessedAt { get; set; }
            public int AccessCount { get; set; }
            public string DataType { get; set; } = "";
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Thông tin cache item cho external usage
    /// </summary>
    public class CacheItemInfo
    {
        public string Key { get; set; } = "";
        public string DataType { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime LastAccessedAt { get; set; }
        public int AccessCount { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsExpired { get; set; }
        public long Size { get; set; }
    }

    /// <summary>
    /// Thống kê cache
    /// </summary>
    public class CacheStatistics
    {
        public int CurrentItemCount { get; set; }
        public long HitCount { get; set; }
        public long MissCount { get; set; }
        public long AddCount { get; set; }
        public long UpdateCount { get; set; }
        public long RemoveCount { get; set; }
        public long ClearCount { get; set; }
        public double HitRate { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    #endregion
}