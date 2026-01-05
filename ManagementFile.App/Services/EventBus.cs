using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ManagementFile.App.Services
{
    /// <summary>
    /// Event Bus service để giao tiếp giữa các phases và components
    /// Cung cấp messaging thống nhất cho ManagementFile Enterprise Platform
    /// </summary>
    public sealed class EventBus
    {
        #region Constructor for DI

        public EventBus()
        {
            _eventHandlers = new ConcurrentDictionary<string, List<IEventHandler>>();
            _eventHistory = new ConcurrentQueue<EventRecord>();
            _isEnabled = true;
        }
        #endregion

        #region Private Fields
        private readonly ConcurrentDictionary<string, List<IEventHandler>> _eventHandlers;
        private readonly ConcurrentQueue<EventRecord> _eventHistory;
        private readonly object _lockObject = new object();
        private bool _isEnabled;
        private long _totalEventsPublished;
        #endregion

        #region Event Subscription

        /// <summary>
        /// Đăng ký handler cho một event type
        /// </summary>
        /// <typeparam name="T">Loại event</typeparam>
        /// <param name="handler">Handler function</param>
        /// <returns>Subscription ID để unsubscribe</returns>
        public string Subscribe<T>(Action<T> handler) where T : class
        {
            var eventType = typeof(T).Name;
            var eventHandler = new EventHandler<T>(handler);
            var subscriptionId = Guid.NewGuid().ToString();
            eventHandler.SubscriptionId = subscriptionId;

            lock (_lockObject)
            {
                if (!_eventHandlers.ContainsKey(eventType))
                {
                    _eventHandlers[eventType] = new List<IEventHandler>();
                }

                _eventHandlers[eventType].Add(eventHandler);
            }

            System.Diagnostics.Debug.WriteLine($"📡 Đăng ký handler cho event: {eventType} (ID: {subscriptionId})");
            return subscriptionId;
        }

        /// <summary>
        /// Đăng ký async handler cho một event type
        /// </summary>
        /// <typeparam name="T">Loại event</typeparam>
        /// <param name="handler">Async handler function</param>
        /// <returns>Subscription ID để unsubscribe</returns>
        public string SubscribeAsync<T>(Func<T, Task> handler) where T : class
        {
            var eventType = typeof(T).Name;
            var eventHandler = new AsyncEventHandler<T>(handler);
            var subscriptionId = Guid.NewGuid().ToString();
            eventHandler.SubscriptionId = subscriptionId;

            lock (_lockObject)
            {
                if (!_eventHandlers.ContainsKey(eventType))
                {
                    _eventHandlers[eventType] = new List<IEventHandler>();
                }

                _eventHandlers[eventType].Add(eventHandler);
            }

            System.Diagnostics.Debug.WriteLine($"📡 Đăng ký async handler cho event: {eventType} (ID: {subscriptionId})");
            return subscriptionId;
        }

        /// <summary>
        /// Hủy đăng ký handler
        /// </summary>
        /// <param name="subscriptionId">Subscription ID</param>
        /// <returns>True nếu hủy thành công</returns>
        public bool Unsubscribe(string subscriptionId)
        {
            if (string.IsNullOrEmpty(subscriptionId))
                return false;

            lock (_lockObject)
            {
                foreach (var eventHandlers in _eventHandlers.Values)
                {
                    var handlerToRemove = eventHandlers.Find(h => h.SubscriptionId == subscriptionId);
                    if (handlerToRemove != null)
                    {
                        eventHandlers.Remove(handlerToRemove);
                        System.Diagnostics.Debug.WriteLine($"🗑️ Hủy đăng ký handler: {subscriptionId}");
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Hủy tất cả handlers cho một event type
        /// </summary>
        /// <typeparam name="T">Loại event</typeparam>
        /// <returns>Số handlers đã bị hủy</returns>
        public int UnsubscribeAll<T>() where T : class
        {
            var eventType = typeof(T).Name;
            
            lock (_lockObject)
            {
                if (_eventHandlers.TryGetValue(eventType, out var handlers))
                {
                    var count = handlers.Count;
                    handlers.Clear();
                    System.Diagnostics.Debug.WriteLine($"🗑️ Hủy tất cả handlers cho event: {eventType} ({count} handlers)");
                    return count;
                }
            }

            return 0;
        }

        #endregion

        #region Event Publishing

        /// <summary>
        /// Publish một event
        /// </summary>
        /// <typeparam name="T">Loại event</typeparam>
        /// <param name="eventData">Dữ liệu event</param>
        public void Publish<T>(T eventData) where T : class
        {
            if (!_isEnabled || eventData == null)
                return;

            var eventType = typeof(T).Name;
            var eventRecord = new EventRecord
            {
                EventType = eventType,
                EventData = eventData,
                Timestamp = DateTime.Now,
                Source = GetCallingMethod()
            };

            // Log event
            _eventHistory.Enqueue(eventRecord);
            _totalEventsPublished++;

            // Keep only last 1000 events
            while (_eventHistory.Count > 1000)
            {
                _eventHistory.TryDequeue(out _);
            }

            // Get handlers
            List<IEventHandler> handlers;
            lock (_lockObject)
            {
                if (!_eventHandlers.TryGetValue(eventType, out handlers) || handlers.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Không có handlers cho event: {eventType}");
                    return;
                }

                // Create a copy to avoid concurrent modification
                handlers = new List<IEventHandler>(handlers);
            }

            // Execute handlers
            var handledCount = 0;
            var errorCount = 0;

            foreach (var handler in handlers)
            {
                try
                {
                    handler.Handle(eventData);
                    handledCount++;
                }
                catch (Exception ex)
                {
                    errorCount++;
                    System.Diagnostics.Debug.WriteLine($"❌ Lỗi khi xử lý event {eventType}: {ex.Message}");
                    
                    // Publish error event
                    try
                    {
                        var errorEvent = new EventHandlingError
                        {
                            OriginalEventType = eventType,
                            HandlerType = handler.GetType().Name,
                            Error = ex,
                            Timestamp = DateTime.Now
                        };
                        PublishInternal(errorEvent);
                    }
                    catch
                    {
                        // Ignore errors when publishing error events to prevent infinite loops
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"📡 Published event {eventType}: {handledCount} handled, {errorCount} errors");
        }

        /// <summary>
        /// Publish async event
        /// </summary>
        /// <typeparam name="T">Loại event</typeparam>
        /// <param name="eventData">Dữ liệu event</param>
        public async Task PublishAsync<T>(T eventData) where T : class
        {
            if (!_isEnabled || eventData == null)
                return;

            var eventType = typeof(T).Name;
            var eventRecord = new EventRecord
            {
                EventType = eventType,
                EventData = eventData,
                Timestamp = DateTime.Now,
                Source = GetCallingMethod()
            };

            // Log event
            _eventHistory.Enqueue(eventRecord);
            _totalEventsPublished++;

            // Get handlers
            List<IEventHandler> handlers;
            lock (_lockObject)
            {
                if (!_eventHandlers.TryGetValue(eventType, out handlers) || handlers.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Không có handlers cho async event: {eventType}");
                    return;
                }

                handlers = new List<IEventHandler>(handlers);
            }

            // Execute handlers async
            var tasks = new List<Task>();
            var handledCount = 0;
            var errorCount = 0;

            foreach (var handler in handlers)
            {
                if (handler is IAsyncEventHandler asyncHandler)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            await asyncHandler.HandleAsync(eventData);
                            handledCount++;
                        }
                        catch (Exception ex)
                        {
                            errorCount++;
                            System.Diagnostics.Debug.WriteLine($"❌ Lỗi khi xử lý async event {eventType}: {ex.Message}");
                        }
                    }));
                }
                else
                {
                    // Handle sync handlers in background
                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            handler.Handle(eventData);
                            handledCount++;
                        }
                        catch (Exception ex)
                        {
                            errorCount++;
                            System.Diagnostics.Debug.WriteLine($"❌ Lỗi khi xử lý event {eventType}: {ex.Message}");
                        }
                    }));
                }
            }

            await Task.WhenAll(tasks);
            System.Diagnostics.Debug.WriteLine($"📡 Published async event {eventType}: {handledCount} handled, {errorCount} errors");
        }

        /// <summary>
        /// Internal publish để tránh infinite loops
        /// </summary>
        private void PublishInternal<T>(T eventData) where T : class
        {
            // Đơn giản hóa logic để tránh recursive calls
            var eventType = typeof(T).Name;
            System.Diagnostics.Debug.WriteLine($"🔄 Internal event published: {eventType}");
        }

        #endregion

        #region Predefined Events (Cross-Phase Communication)

        /// <summary>
        /// Publish user selection event (Admin → Client)
        /// </summary>
        public void PublishUserSelected(object user)
        {
            Publish(new UserSelectedEvent { SelectedUser = user, Timestamp = DateTime.Now });
        }

        /// <summary>
        /// Publish project selection event (Project → Reports)
        /// </summary>
        public void PublishProjectSelected(object project)
        {
            Publish(new ProjectSelectedEvent { SelectedProject = project, Timestamp = DateTime.Now });
        }

        /// <summary>
        /// Publish notification event (All phases)
        /// </summary>
        public void PublishNotification(string title, string message, string type = "Info")
        {
            Publish(new NotificationEvent 
            { 
                Title = title, 
                Message = message, 
                Type = type, 
                Timestamp = DateTime.Now 
            });
        }

        /// <summary>
        /// Publish data update event
        /// </summary>
        public void PublishDataUpdated(string dataType, string action, object data = null)
        {
            Publish(new DataUpdateEvent 
            { 
                DataType = dataType, 
                Action = action, 
                UpdatedData = data, 
                Timestamp = DateTime.Now 
            });
        }

        /// <summary>
        /// Publish performance alert event
        /// </summary>
        public void PublishPerformanceAlert(string alertType, string message, object metrics = null)
        {
            Publish(new PerformanceAlertEvent 
            { 
                AlertType = alertType, 
                Message = message, 
                Metrics = metrics, 
                Timestamp = DateTime.Now 
            });
        }

        #endregion

        #region Event Bus Management

        /// <summary>
        /// Bật/tắt EventBus
        /// </summary>
        /// <param name="enabled">True để bật, False để tắt</param>
        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            System.Diagnostics.Debug.WriteLine($"🎛️ EventBus {(enabled ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Kiểm tra EventBus có đang hoạt động không
        /// </summary>
        public bool IsEnabled => _isEnabled;

        /// <summary>
        /// Lấy thống kê EventBus
        /// </summary>
        public EventBusStatistics GetStatistics()
        {
            var handlerCount = 0;
            var eventTypes = new List<string>();

            lock (_lockObject)
            {
                foreach (var kvp in _eventHandlers)
                {
                    eventTypes.Add(kvp.Key);
                    handlerCount += kvp.Value.Count;
                }
            }

            return new EventBusStatistics
            {
                TotalEventsPublished = _totalEventsPublished,
                TotalEventTypes = eventTypes.Count,
                TotalHandlers = handlerCount,
                EventHistory = _eventHistory.Count,
                IsEnabled = _isEnabled,
                LastUpdated = DateTime.Now,
                RegisteredEventTypes = eventTypes
            };
        }

        /// <summary>
        /// Lấy event history
        /// </summary>
        /// <param name="count">Số events cần lấy (max 100)</param>
        /// <returns>Danh sách event records</returns>
        public List<EventRecord> GetEventHistory(int count = 50)
        {
            var events = new List<EventRecord>();
            var eventArray = _eventHistory.ToArray();
            
            var startIndex = Math.Max(0, eventArray.Length - Math.Min(count, 100));
            for (int i = startIndex; i < eventArray.Length; i++)
            {
                events.Add(eventArray[i]);
            }

            return events;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Lấy tên method đang gọi EventBus
        /// </summary>
        private string GetCallingMethod()
        {
            try
            {
                var stackTrace = new System.Diagnostics.StackTrace();
                var frame = stackTrace.GetFrame(3); // Skip EventBus methods
                return frame?.GetMethod()?.Name ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Dọn dẹp EventBus
        /// </summary>
        public void Cleanup()
        {
            try
            {
                lock (_lockObject)
                {
                    var totalHandlers = 0;
                    foreach (var handlers in _eventHandlers.Values)
                    {
                        totalHandlers += handlers.Count;
                        handlers.Clear();
                    }
                    _eventHandlers.Clear();
                    
                    System.Diagnostics.Debug.WriteLine($"🧹 EventBus đã được dọn dẹp ({totalHandlers} handlers removed)");
                }
                
                // Clear history
                while (_eventHistory.TryDequeue(out _)) { }
                
                _isEnabled = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi khi dọn dẹp EventBus: {ex.Message}");
            }
        }

        #endregion
    }

    #region Supporting Interfaces and Classes

    /// <summary>
    /// Interface cho event handlers
    /// </summary>
    public interface IEventHandler
    {
        string SubscriptionId { get; set; }
        void Handle(object eventData);
    }

    /// <summary>
    /// Interface cho async event handlers
    /// </summary>
    public interface IAsyncEventHandler : IEventHandler
    {
        Task HandleAsync(object eventData);
    }

    /// <summary>
    /// Sync event handler implementation
    /// </summary>
    internal class EventHandler<T> : IEventHandler where T : class
    {
        private readonly Action<T> _handler;
        
        public string SubscriptionId { get; set; } = "";
        
        public EventHandler(Action<T> handler)
        {
            _handler = handler;
        }
        
        public void Handle(object eventData)
        {
            if (eventData is T typedData)
            {
                _handler(typedData);
            }
        }
    }

    /// <summary>
    /// Async event handler implementation
    /// </summary>
    internal class AsyncEventHandler<T> : IAsyncEventHandler where T : class
    {
        private readonly Func<T, Task> _handler;
        
        public string SubscriptionId { get; set; } = "";
        
        public AsyncEventHandler(Func<T, Task> handler)
        {
            _handler = handler;
        }
        
        public void Handle(object eventData)
        {
            // For sync calls to async handlers, we don't wait
            if (eventData is T typedData)
            {
                Task.Run(() => _handler(typedData));
            }
        }
        
        public async Task HandleAsync(object eventData)
        {
            if (eventData is T typedData)
            {
                await _handler(typedData);
            }
        }
    }

    /// <summary>
    /// Event record cho history
    /// </summary>
    public class EventRecord
    {
        public string EventType { get; set; } = "";
        public object EventData { get; set; }
        public DateTime Timestamp { get; set; }
        public string Source { get; set; } = "";
    }

    /// <summary>
    /// EventBus statistics
    /// </summary>
    public class EventBusStatistics
    {
        public long TotalEventsPublished { get; set; }
        public int TotalEventTypes { get; set; }
        public int TotalHandlers { get; set; }
        public int EventHistory { get; set; }
        public bool IsEnabled { get; set; }
        public DateTime LastUpdated { get; set; }
        public List<string> RegisteredEventTypes { get; set; } = new List<string>();
    }

    #endregion

    #region Predefined Event Classes

    /// <summary>
    /// User selected event (Admin → Client)
    /// </summary>
    public class UserSelectedEvent
    {
        public object SelectedUser { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Project selected event (Project → Reports)
    /// </summary>
    public class ProjectSelectedEvent
    {
        public object SelectedProject { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Notification event (All phases)
    /// </summary>
    public class NotificationEvent
    {
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string Type { get; set; } = "Info"; // Info, Warning, Error, Success
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Data update event
    /// </summary>
    public class DataUpdateEvent
    {
        public string DataType { get; set; } = ""; // User, Project, Task, File, etc.
        public string Action { get; set; } = ""; // Created, Updated, Deleted
        public object UpdatedData { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Performance alert event
    /// </summary>
    public class PerformanceAlertEvent
    {
        public string AlertType { get; set; } = ""; // Memory, CPU, Disk, Network
        public string Message { get; set; } = "";
        public object Metrics { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Event handling error
    /// </summary>
    public class EventHandlingError
    {
        public string OriginalEventType { get; set; } = "";
        public string HandlerType { get; set; } = "";
        public Exception Error { get; set; }
        public DateTime Timestamp { get; set; }
    }

    #endregion
}