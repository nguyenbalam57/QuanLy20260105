using ManagementFile.Contracts.DTOs.Common;
using ManagementFile.Contracts.DTOs.ProjectManagement;
using ManagementFile.Contracts.DTOs.ProjectManagement.TimeLogs;
using ManagementFile.Contracts.Requests.ProjectManagement.TimeLogs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ManagementFile.App.Services.TimeTracking
{
    /// <summary>
    /// Service để tương tác với Time Tracking API
    /// Kế thừa từ ApiService để sử dụng chung infrastructure
    /// </summary>
    public class TimeTrackingApiService
    {
        private const string BASE_ENDPOINT = "api/TimeTracking";
        private readonly ApiService _apiService;

        public TimeTrackingApiService(ApiService apiService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        }

        #region Weekly Timesheet APIs

        /// <summary>
        /// Lấy weekly timesheet
        /// </summary>
        /// <param name="weekStartDate">Ngày bắt đầu tuần (Monday)</param>
        /// <param name="userId">ID user (null = current user)</param>
        /// <param name="projectId">ID project để filter</param>
        public async Task<WeeklyTimesheetDto> GetWeeklyTimesheetAsync(
            DateTime weekStartDate,
            int? userId = null,
            int? projectId = null)
        {
            try
            {
                var request = new GetWeeklyTimesheetRequest
                {
                    WeekStartDate = weekStartDate,
                    UserId = userId,
                    ProjectId = projectId
                };

                var response = await _apiService.PostToEndpointAsync($"{BASE_ENDPOINT}/weekly/get", request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<WeeklyTimesheetDto>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }

                    throw new Exception(apiResponse?.Message ?? "Không thể lấy weekly timesheet");
                }

                return await HandleErrorResponse<WeeklyTimesheetDto>(response);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ GetWeeklyTimesheetAsync Error: {ex.Message}");
                throw new Exception($"Lỗi khi lấy weekly timesheet: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Lưu weekly timesheet (batch save)
        /// </summary>
        public async Task<WeeklyTimesheetDto> SaveWeeklyTimesheetAsync(SaveWeeklyTimesheetRequest request)
        {
            try
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request));

                if (!request.Entries.Any())
                    throw new ArgumentException("Timesheet không có dữ liệu để lưu");

                System.Diagnostics.Debug.WriteLine($"💾 Saving weekly timesheet for week: {request.WeekStartDate:dd/MM/yyyy}");
                System.Diagnostics.Debug.WriteLine($"💾 Total entries: {request.Entries.Count}");

                var response = await _apiService.PostToEndpointAsync($"{BASE_ENDPOINT}/weekly/save", request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<WeeklyTimesheetDto>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        System.Diagnostics.Debug.WriteLine("✅ Weekly timesheet saved successfully");
                        return apiResponse.Data;
                    }

                    throw new Exception(apiResponse?.Message ?? "Không thể lưu weekly timesheet");
                }

                return await HandleErrorResponse<WeeklyTimesheetDto>(response);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ SaveWeeklyTimesheetAsync Error: {ex.Message}");
                throw new Exception($"Lỗi khi lưu weekly timesheet: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Copy timesheet từ tuần khác
        /// </summary>
        public async Task<WeeklyTimesheetDto> CopyWeeklyTimesheetAsync(
            DateTime sourceWeekStart,
            DateTime targetWeekStart,
            bool includeNotes = false)
        {
            try
            {
                var request = new CopyWeekTimesheetRequest
                {
                    SourceWeekStartDate = sourceWeekStart,
                    TargetWeekStartDate = targetWeekStart,
                    IncludeNotes = includeNotes
                };

                System.Diagnostics.Debug.WriteLine($"📋 Copying timesheet from {sourceWeekStart:dd/MM/yyyy} to {targetWeekStart:dd/MM/yyyy}");

                var response = await _apiService.PostToEndpointAsync($"{BASE_ENDPOINT}/weekly/copy", request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<WeeklyTimesheetDto>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        System.Diagnostics.Debug.WriteLine("✅ Timesheet copied successfully");
                        return apiResponse.Data;
                    }

                    throw new Exception(apiResponse?.Message ?? "Không thể copy weekly timesheet");
                }

                return await HandleErrorResponse<WeeklyTimesheetDto>(response);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ CopyWeeklyTimesheetAsync Error: {ex.Message}");
                throw new Exception($"Lỗi khi copy weekly timesheet: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Lấy danh sách tasks available cho timesheet
        /// </summary>
        public async Task<List<TaskForTimesheetDto>> GetAvailableTasksForTimesheetAsync(int? projectId = null)
        {
            try
            {
                var endpoint = $"{BASE_ENDPOINT}/weekly/available-tasks";
                if (projectId.HasValue)
                {
                    endpoint += $"?projectId={projectId.Value}";
                }

                var response = await _apiService.GetFromEndpointAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<List<TaskForTimesheetDto>>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }

                    throw new Exception(apiResponse?.Message ?? "Không thể lấy danh sách tasks");
                }

                return await HandleErrorResponse<List<TaskForTimesheetDto>>(response);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ GetAvailableTasksForTimesheetAsync Error: {ex.Message}");
                throw new Exception($"Lỗi khi lấy danh sách tasks: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validate weekly timesheet trước khi submit
        /// </summary>
        public async Task<WeeklyTimesheetValidationResult> ValidateWeeklyTimesheetAsync(SaveWeeklyTimesheetRequest request)
        {
            try
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request));

                var response = await _apiService.PostToEndpointAsync($"{BASE_ENDPOINT}/weekly/validate", request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<WeeklyTimesheetValidationResult>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }

                    throw new Exception(apiResponse?.Message ?? "Không thể validate timesheet");
                }

                return await HandleErrorResponse<WeeklyTimesheetValidationResult>(response);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ValidateWeeklyTimesheetAsync Error: {ex.Message}");
                throw new Exception($"Lỗi khi validate timesheet: {ex.Message}", ex);
            }
        }

        #endregion

        #region Regular Time Log APIs

        /// <summary>
        /// Lấy danh sách time logs với filter
        /// </summary>
        public async Task<PagedResult<TimeTrackingTaskTimeLogDto>> GetTimeLogsAsync(TimeLogFilterRequest filter)
        {
            try
            {
                if (filter == null)
                    filter = new TimeLogFilterRequest();

                var queryParams = new List<string>();

                if (filter.TaskId > 0) queryParams.Add($"taskId={filter.TaskId}");
                if (filter.UserId > 0) queryParams.Add($"userId={filter.UserId}");
                if (filter.ProjectId > 0) queryParams.Add($"projectId={filter.ProjectId}");
                if (filter.StartDate.HasValue) queryParams.Add($"startDate={filter.StartDate.Value:yyyy-MM-dd}");
                if (filter.EndDate.HasValue) queryParams.Add($"endDate={filter.EndDate.Value:yyyy-MM-dd}");
                if (filter.IsBillable.HasValue) queryParams.Add($"isBillable={filter.IsBillable.Value}");

                queryParams.Add($"pageNumber={filter.PageNumber}");
                queryParams.Add($"pageSize={filter.PageSize}");
                queryParams.Add($"sortBy={filter.SortBy}");
                queryParams.Add($"sortDirection={filter.SortDirection}");

                var queryString = string.Join("&", queryParams);
                var endpoint = $"{BASE_ENDPOINT}/logs?{queryString}";

                var response = await _apiService.GetFromEndpointAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<PagedResult<TimeTrackingTaskTimeLogDto>>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }

                    throw new Exception(apiResponse?.Message ?? "Không thể lấy danh sách time logs");
                }

                return await HandleErrorResponse<PagedResult<TimeTrackingTaskTimeLogDto>>(response);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ GetTimeLogsAsync Error: {ex.Message}");
                throw new Exception($"Lỗi khi lấy danh sách time logs: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Lấy time log theo ID
        /// </summary>
        public async Task<TimeTrackingTaskTimeLogDto> GetTimeLogByIdAsync(int timeLogId)
        {
            try
            {
                var response = await _apiService.GetFromEndpointAsync($"{BASE_ENDPOINT}/logs/{timeLogId}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<TimeTrackingTaskTimeLogDto>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }

                    throw new Exception(apiResponse?.Message ?? "Không tìm thấy time log");
                }

                return await HandleErrorResponse<TimeTrackingTaskTimeLogDto>(response);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ GetTimeLogByIdAsync Error: {ex.Message}");
                throw new Exception($"Lỗi khi lấy thông tin time log: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Tạo time log thủ công
        /// </summary>
        public async Task<TimeTrackingTaskTimeLogDto> CreateTimeLogAsync(CreateTimeLogRequest request)
        {
            try
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request));

                var response = await _apiService.PostToEndpointAsync($"{BASE_ENDPOINT}/logs", request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<TimeTrackingTaskTimeLogDto>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        System.Diagnostics.Debug.WriteLine("✅ Time log created successfully");
                        return apiResponse.Data;
                    }

                    throw new Exception(apiResponse?.Message ?? "Không thể tạo time log");
                }

                return await HandleErrorResponse<TimeTrackingTaskTimeLogDto>(response);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ CreateTimeLogAsync Error: {ex.Message}");
                throw new Exception($"Lỗi khi tạo time log: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Cập nhật time log
        /// </summary>
        public async Task<TimeTrackingTaskTimeLogDto> UpdateTimeLogAsync(int timeLogId, UpdateTimeLogRequest request)
        {
            try
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request));

                var response = await _apiService.PutToEndpointAsync($"{BASE_ENDPOINT}/logs/{timeLogId}", request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<TimeTrackingTaskTimeLogDto>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        System.Diagnostics.Debug.WriteLine("✅ Time log updated successfully");
                        return apiResponse.Data;
                    }

                    throw new Exception(apiResponse?.Message ?? "Không thể cập nhật time log");
                }

                return await HandleErrorResponse<TimeTrackingTaskTimeLogDto>(response);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ UpdateTimeLogAsync Error: {ex.Message}");
                throw new Exception($"Lỗi khi cập nhật time log: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Xóa time log
        /// </summary>
        public async Task<bool> DeleteTimeLogAsync(int timeLogId)
        {
            try
            {
                var response = await _apiService.DeleteFromEndpointAsync($"{BASE_ENDPOINT}/logs/{timeLogId}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<bool>>(content);

                    if (apiResponse?.Success == true)
                    {
                        System.Diagnostics.Debug.WriteLine("✅ Time log deleted successfully");
                        return apiResponse.Data;
                    }

                    throw new Exception(apiResponse?.Message ?? "Không thể xóa time log");
                }

                await HandleErrorResponse<bool>(response);
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ DeleteTimeLogAsync Error: {ex.Message}");
                throw new Exception($"Lỗi khi xóa time log: {ex.Message}", ex);
            }
        }

        #endregion

        #region Timer APIs

        /// <summary>
        /// Bắt đầu timer cho task
        /// </summary>
        public async Task<TimeTrackingTaskTimeLogDto> StartTimerAsync(int taskId, string description = "", bool isBillable = true)
        {
            try
            {
                var request = new StartTimerRequest
                {
                    TaskId = taskId,
                    Description = description,
                    IsBillable = isBillable
                };

                System.Diagnostics.Debug.WriteLine($"▶️ Starting timer for task {taskId}");

                var response = await _apiService.PostToEndpointAsync($"{BASE_ENDPOINT}/start", request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<TimeTrackingTaskTimeLogDto>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        System.Diagnostics.Debug.WriteLine("✅ Timer started successfully");
                        return apiResponse.Data;
                    }

                    throw new Exception(apiResponse?.Message ?? "Không thể bắt đầu timer");
                }

                return await HandleErrorResponse<TimeTrackingTaskTimeLogDto>(response);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ StartTimerAsync Error: {ex.Message}");
                throw new Exception($"Lỗi khi bắt đầu timer: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Dừng timer hiện tại
        /// </summary>
        public async Task<TimeTrackingTaskTimeLogDto> StopTimerAsync(int? timeLogId = null, string description = null)
        {
            try
            {
                var request = new StopTimerRequest
                {
                    TimeLogId = timeLogId ?? -1,
                    Description = description
                };

                System.Diagnostics.Debug.WriteLine($"⏹️ Stopping timer");

                var response = await _apiService.PostToEndpointAsync($"{BASE_ENDPOINT}/stop", request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<TimeTrackingTaskTimeLogDto>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        System.Diagnostics.Debug.WriteLine("✅ Timer stopped successfully");
                        return apiResponse.Data;
                    }

                    throw new Exception(apiResponse?.Message ?? "Không thể dừng timer");
                }

                return await HandleErrorResponse<TimeTrackingTaskTimeLogDto>(response);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ StopTimerAsync Error: {ex.Message}");
                throw new Exception($"Lỗi khi dừng timer: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Lấy timer hiện tại đang chạy
        /// </summary>
        public async Task<TimeTrackingTaskTimeLogDto> GetCurrentTimerAsync()
        {
            try
            {
                var response = await _apiService.GetFromEndpointAsync($"{BASE_ENDPOINT}/current");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<TimeTrackingTaskTimeLogDto>>(content);

                    if (apiResponse?.Success == true)
                    {
                        return apiResponse.Data; // Có thể null nếu không có timer đang chạy
                    }

                    throw new Exception(apiResponse?.Message ?? "Không thể lấy thông tin timer");
                }

                return await HandleErrorResponse<TimeTrackingTaskTimeLogDto>(response);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ GetCurrentTimerAsync Error: {ex.Message}");
                throw new Exception($"Lỗi khi lấy thông tin timer: {ex.Message}", ex);
            }
        }

        #endregion

        #region Report APIs

        /// <summary>
        /// Lấy báo cáo tổng hợp time tracking
        /// </summary>
        public async Task<TimeTrackingSummaryDto> GetTimeTrackingSummaryAsync(
            int? userId = null,
            int? projectId = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                var queryParams = new List<string>();

                if (userId.HasValue) queryParams.Add($"userId={userId.Value}");
                if (projectId.HasValue) queryParams.Add($"projectId={projectId.Value}");
                if (startDate.HasValue) queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
                if (endDate.HasValue) queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");

                var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
                var endpoint = $"{BASE_ENDPOINT}/reports/summary{queryString}";

                var response = await _apiService.GetFromEndpointAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<TimeTrackingSummaryDto>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }

                    throw new Exception(apiResponse?.Message ?? "Không thể lấy báo cáo");
                }

                return await HandleErrorResponse<TimeTrackingSummaryDto>(response);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ GetTimeTrackingSummaryAsync Error: {ex.Message}");
                throw new Exception($"Lỗi khi lấy báo cáo tổng hợp: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Lấy báo cáo thời gian theo user
        /// </summary>
        public async Task<UserTimeReportDto> GetUserTimeReportAsync(
            int userId,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                var queryParams = new List<string>();
                if (startDate.HasValue) queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
                if (endDate.HasValue) queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");

                var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
                var endpoint = $"{BASE_ENDPOINT}/reports/user/{userId}{queryString}";

                var response = await _apiService.GetFromEndpointAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<UserTimeReportDto>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }

                    throw new Exception(apiResponse?.Message ?? "Không thể lấy báo cáo user");
                }

                return await HandleErrorResponse<UserTimeReportDto>(response);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ GetUserTimeReportAsync Error: {ex.Message}");
                throw new Exception($"Lỗi khi lấy báo cáo user: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Lấy báo cáo thời gian theo project
        /// </summary>
        public async Task<ProjectTimeReportDto> GetProjectTimeReportAsync(
            int projectId,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                var queryParams = new List<string>();
                if (startDate.HasValue) queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
                if (endDate.HasValue) queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");

                var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
                var endpoint = $"{BASE_ENDPOINT}/reports/project/{projectId}{queryString}";

                var response = await _apiService.GetFromEndpointAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectTimeReportDto>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }

                    throw new Exception(apiResponse?.Message ?? "Không thể lấy báo cáo project");
                }

                return await HandleErrorResponse<ProjectTimeReportDto>(response);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ GetProjectTimeReportAsync Error: {ex.Message}");
                throw new Exception($"Lỗi khi lấy báo cáo project: {ex.Message}", ex);
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Xử lý error response và throw exception với message phù hợp
        /// </summary>
        private async Task<T> HandleErrorResponse<T>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();

            try
            {
                var errorResponse = JsonConvert.DeserializeObject<ApiResponse<object>>(content);
                var errorMessage = errorResponse?.Message ?? response.ReasonPhrase;

                // Log chi tiết error
                System.Diagnostics.Debug.WriteLine($"🚨 API Error: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"🚨 Error Message: {errorMessage}");

                if (errorResponse?.Errors != null && errorResponse.Errors.Any())
                {
                    System.Diagnostics.Debug.WriteLine("🚨 Error Details:");
                    foreach (var error in errorResponse.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"   - {error}");
                    }

                    // Combine error messages
                    errorMessage += "\n" + string.Join("\n", errorResponse.Errors);
                }

                throw new Exception(errorMessage);
            }
            catch (JsonException)
            {
                // Nếu không parse được JSON, throw raw content
                throw new Exception($"HTTP {(int)response.StatusCode}: {content}");
            }
        }

        #endregion
    }
}
