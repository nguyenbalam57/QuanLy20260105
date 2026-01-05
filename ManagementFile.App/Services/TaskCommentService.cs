using ManagementFile.Contracts.DTOs.Common;
using ManagementFile.Contracts.DTOs.ProjectManagement;
using ManagementFile.Contracts.Enums;
using ManagementFile.Contracts.Requests.ProjectManagement.TaskComments;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagementFile.App.Services
{
    /// <summary>
    /// Service để quản lý TaskComment
    /// Xử lý CRUD operations, business logic và validation cho Task Comments
    /// Tích hợp với API thông qua ApiService
    /// </summary>
    public class TaskCommentService
    {
        private readonly ApiService _apiService;
        private readonly UserManagementService _userService;
        private readonly ProjectApiService _projectApiService;
        private readonly ILogger<TaskCommentService> _logger;
        private const string BaseEndpoint = "api/projects";

        public TaskCommentService(
            ApiService apiService,
            UserManagementService userService,
            ProjectApiService projectApiService,
            ILogger<TaskCommentService> logger)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _projectApiService = projectApiService ?? throw new ArgumentNullException(nameof(projectApiService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region CRUD Operations

        /// <summary>
        /// Tạo TaskComment mới thông qua API
        /// </summary>
        public async Task<TaskCommentResponseDto> CreateTaskCommentAsync(int projectId, CreateTaskCommentRequest request)
        {
            try
            {
                _logger.LogInformation("Creating new TaskComment for TaskId: {TaskId} ",
                    request.TaskId);

                // 1. CLIENT-SIDE VALIDATION
                var validationResult = ValidateCreateRequest(request);
                if (!validationResult.IsValid)
                {
                    return new TaskCommentResponseDto
                    {
                        Success = false,
                        Message = "Dữ liệu không hợp lệ",
                        Errors = validationResult.Errors
                    };
                }

                // 2. BUSINESS LOGIC VALIDATION
                await ValidateCreateBusinessRulesAsync(projectId, request);

                // 3. GỌI API
                // Lấy projectId từ taskId (cần call API để lấy task info)
                var taskInfo = await GetTaskInfoAsync(projectId, request.TaskId);
                if (taskInfo == null)
                {
                    return new TaskCommentResponseDto
                    {
                        Success = false,
                        Message = "Không tìm thấy task"
                    };
                }

                var endpoint = $"{BaseEndpoint}/{taskInfo.ProjectId}/tasks/{request.TaskId}/comments";
                var response = await _apiService.PostToEndpointAsync(endpoint, request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<TaskCommentDto>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        _logger.LogInformation("TaskComment created successfully with ID: {CommentId}",
                            apiResponse.Data.Id);

                        return new TaskCommentResponseDto
                        {
                            Success = true,
                            Message = "Bình luận đã được tạo thành công",
                            Data = apiResponse.Data
                        };
                    }
                    else
                    {
                        return new TaskCommentResponseDto
                        {
                            Success = false,
                            Message = apiResponse?.Message ?? "Lỗi không xác định từ API",
                            Errors = apiResponse?.Errors ?? new List<string>()
                        };
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("API call failed with status: {StatusCode}, Content: {Content}",
                        response.StatusCode, errorContent);

                    return new TaskCommentResponseDto
                    {
                        Success = false,
                        Message = $"Lỗi API: {response.StatusCode}",
                        Errors = new List<string> { errorContent }
                    };
                }
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation failed for CreateTaskComment");
                return new TaskCommentResponseDto
                {
                    Success = false,
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                };
            }
            catch (BusinessRuleException ex)
            {
                _logger.LogWarning(ex, "Business rule failed for CreateTaskComment");
                return new TaskCommentResponseDto
                {
                    Success = false,
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating TaskComment for TaskId: {TaskId}",
                    request.TaskId);
                return new TaskCommentResponseDto
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi không mong muốn",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        /// <summary>
        /// Cập nhật TaskComment thông qua API
        /// </summary>
        public async Task<TaskCommentResponseDto> UpdateTaskCommentAsync(int projectId, int taskId, UpdateTaskCommentRequest request)
        {
            try
            {
                _logger.LogInformation("Updating TaskComment ID: {CommentId} by user",
                    request.Id);

                // 1. CLIENT-SIDE VALIDATION
                var validationResult = ValidateUpdateRequest(request);
                if (!validationResult.IsValid)
                {
                    return new TaskCommentResponseDto
                    {
                        Success = false,
                        Message = "Dữ liệu không hợp lệ",
                        Errors = validationResult.Errors
                    };
                }

                // 2. LẤY THÔNG TIN COMMENT HIỆN TẠI
                var existingComment = await GetTaskCommentByIdAsync(projectId, taskId, request.Id);
                if (existingComment == null)
                {
                    return new TaskCommentResponseDto
                    {
                        Success = false,
                        Message = "Không tìm thấy comment"
                    };
                }

                // 3. BUSINESS LOGIC VALIDATION
                await ValidateUpdateBusinessRulesAsync(request, existingComment);

                // 4. GỌI API
                var taskInfo = await GetTaskInfoAsync(projectId, existingComment.TaskId);
                if (taskInfo == null)
                {
                    return new TaskCommentResponseDto
                    {
                        Success = false,
                        Message = "Không tìm thấy task liên quan"
                    };
                }

                var endpoint = $"{BaseEndpoint}/{taskInfo.ProjectId}/tasks/{existingComment.TaskId}/comments/{request.Id}";
                var response = await _apiService.PutToEndpointAsync(endpoint, request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<TaskCommentDto>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        _logger.LogInformation("TaskComment updated successfully: {CommentId}", request.Id);

                        return new TaskCommentResponseDto
                        {
                            Success = true,
                            Message = "Bình luận đã được cập nhật thành công",
                            Data = apiResponse.Data
                        };
                    }
                    else
                    {
                        return new TaskCommentResponseDto
                        {
                            Success = false,
                            Message = apiResponse?.Message ?? "Lỗi không xác định từ API",
                            Errors = apiResponse?.Errors ?? new List<string>()
                        };
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("API call failed with status: {StatusCode}, Content: {Content}",
                        response.StatusCode, errorContent);

                    return new TaskCommentResponseDto
                    {
                        Success = false,
                        Message = $"Lỗi API: {response.StatusCode}",
                        Errors = new List<string> { errorContent }
                    };
                }
            }
            catch (ValidationException ex)
            {
                return new TaskCommentResponseDto
                {
                    Success = false,
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                };
            }
            catch (BusinessRuleException ex)
            {
                return new TaskCommentResponseDto
                {
                    Success = false,
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating TaskComment: {CommentId}", request.Id);
                return new TaskCommentResponseDto
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi không mong muốn",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        /// <summary>
        /// Lấy TaskComment theo ID thông qua API
        /// </summary>
        public async Task<TaskCommentDto> GetTaskCommentByIdAsync(int projectId, int taskId, int commentId)
        {
            try
            {
                _logger.LogDebug("Getting TaskComment by ID: {CommentId}", commentId);

                var responseMessage = await _apiService.GetFromEndpointAsync($"{BaseEndpoint}/{projectId}/tasks/{taskId}/comments/{commentId}");

                if(responseMessage.IsSuccessStatusCode)
                {
                    var content = await responseMessage.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<TaskCommentDto>>(content);
                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }
                    else
                    {
                        _logger.LogWarning("API returned unsuccessful response for CommentId: {CommentId}", commentId);
                        return null;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting TaskComment by ID: {CommentId}", commentId);
                return null;
            }
        }

        /// <summary>
        /// Lấy danh sách TaskComment với pagination thông qua API
        /// </summary>
        public async Task<TaskCommentsPagedResponseDto> GetTaskCommentsPagedAsync(
            int projectId, 
            GetTaskCommentsRequest request)
        {
            try
            {
                _logger.LogInformation("Getting paged TaskComments for TaskId: {TaskId}", request.TaskId);

                // 1. VALIDATION
                if (request.TaskId <= 0)
                {
                    return new TaskCommentsPagedResponseDto
                    {
                        Success = false,
                        Message = "TaskId không hợp lệ"
                    };
                }

                // 2. BUILD QUERY PARAMETERS
                var queryParams = BuildTaskCommentsQuery(request);
                var endpoint = $"{BaseEndpoint}/{projectId}/tasks/{request.TaskId}/comments/paged{queryParams}";

                // 3. GỌI API
                var response = await _apiService.GetFromEndpointAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<PagedResult<TaskCommentDto>>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return new TaskCommentsPagedResponseDto
                        {
                            Success = true,
                            Message = "Lấy danh sách comments thành công",
                            Data = apiResponse.Data.Items,
                            Pagination = new PaginationMetadata
                            {
                                CurrentPage = apiResponse.Data.PageNumber,
                                PageSize = apiResponse.Data.PageSize,
                                TotalPages = apiResponse.Data.TotalPages,
                                TotalCount = apiResponse.Data.TotalCount,
                                HasPrevious = apiResponse.Data.HasPreviousPage,
                                HasNext = apiResponse.Data.HasNextPage
                            }
                        };
                    }
                    else
                    {
                        return new TaskCommentsPagedResponseDto
                        {
                            Success = false,
                            Message = apiResponse?.Message ?? "Lỗi không xác định từ API"
                        };
                    }
                }
                else
                {
                    // Fallback to mock data
                    return CreateMockCommentsPagedResponse(request);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paged TaskComments for TaskId: {TaskId}", request.TaskId);
                return CreateMockCommentsPagedResponse(request);
            }
        }

        /// <summary>
        /// Xóa TaskComment thông qua API
        /// </summary>
        public async Task<bool> DeleteTaskCommentAsync(int projectId, int taskId, int commentId)
        {
            try
            {
                _logger.LogInformation("Deleting TaskComment ID: {CommentId}",
                    commentId);

                // 1. LẤY THÔNG TIN COMMENT
                var existingComment = await GetTaskCommentByIdAsync(projectId, taskId, commentId);
                if (existingComment == null)
                {
                    _logger.LogWarning("TaskComment not found for deletion: {CommentId}", commentId);
                    return false;
                }

                // 2. KIỂM TRA QUYỀN XÓA
                await ValidateDeletePermissionAsync(projectId, existingComment);

                // 3. LẤY TASK INFO
                var taskInfo = await GetTaskInfoAsync(projectId, existingComment.TaskId);
                if (taskInfo == null)
                {
                    _logger.LogWarning("Task not found for comment deletion: {TaskId}", existingComment.TaskId);
                    return false;
                }

                // 4. GỌI API XÓA
                var endpoint = $"{BaseEndpoint}/{taskInfo.ProjectId}/tasks/{existingComment.TaskId}/comments/{commentId}";
                var response = await _apiService.DeleteFromEndpointAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("TaskComment deleted successfully: {CommentId}", commentId);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to delete TaskComment: {CommentId}, Status: {Status}, Error: {Error}",
                        commentId, response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting TaskComment: {CommentId}", commentId);
                return false;
            }
        }

        #endregion

        #region Specialized Operations

        /// <summary>
        /// Resolve TaskComment thông qua API
        /// </summary>
        public async Task<TaskCommentResponseDto> ResolveTaskCommentAsync(
            int projectId,
            int taskId,
            int commentId, 
            ResolveTaskCommentRequest request)
        {
            try
            {
                _logger.LogInformation("Resolving TaskComment ID: {CommentId}", commentId);

                // 1. LẤY THÔNG TIN COMMENT
                var existingComment = await GetTaskCommentByIdAsync(projectId, taskId, commentId);
                if (existingComment == null)
                {
                    return new TaskCommentResponseDto
                    {
                        Success = false,
                        Message = "Không tìm thấy comment"
                    };
                }

                // 2. VALIDATION
                await ValidateResolvePermissionAsync(existingComment);

                // 3. GỌI API
                var taskInfo = await GetTaskInfoAsync(projectId, existingComment.TaskId);
                var endpoint = $"{BaseEndpoint}/{taskInfo.ProjectId}/tasks/{existingComment.TaskId}/comments/{commentId}/resolve";
                var response = await _apiService.PatchToEndpointAsync(endpoint, request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<TaskCommentDto>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return new TaskCommentResponseDto
                        {
                            Success = true,
                            Message = "Comment đã được resolve thành công",
                            Data = apiResponse.Data
                        };
                    }
                }

                return new TaskCommentResponseDto
                {
                    Success = false,
                    Message = "Lỗi khi resolve comment"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving TaskComment: {CommentId}", commentId);
                return new TaskCommentResponseDto
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Verify TaskComment thông qua API
        /// </summary>
        public async Task<TaskCommentResponseDto> VerifyTaskCommentAsync(
            int projectId,
            int taskId,
            int commentId, 
            VerifyTaskCommentRequest request)
        {
            try
            {
                var existingComment = await GetTaskCommentByIdAsync(projectId, taskId, commentId);
                if (existingComment == null)
                {
                    return new TaskCommentResponseDto
                    {
                        Success = false,
                        Message = "Không tìm thấy comment"
                    };
                }

                await ValidateVerifyPermissionAsync(existingComment);

                var taskInfo = await GetTaskInfoAsync( projectId, existingComment.TaskId);
                var endpoint = $"{BaseEndpoint}/{taskInfo.ProjectId}/tasks/{existingComment.TaskId}/comments/{commentId}/verify";
                var response = await _apiService.PatchToEndpointAsync(endpoint, request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<TaskCommentDto>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return new TaskCommentResponseDto
                        {
                            Success = true,
                            Message = "Comment đã được verify thành công",
                            Data = apiResponse.Data
                        };
                    }
                }

                return new TaskCommentResponseDto
                {
                    Success = false,
                    Message = "Lỗi khi verify comment"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying TaskComment: {CommentId}", commentId);
                return new TaskCommentResponseDto
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Agree/Disagree với TaskComment thông qua API
        /// </summary>
        public async Task<TaskCommentResponseDto> AgreeTaskCommentAsync(
            int projectId,
            int taskId,
            int commentId, 
            AgreeTaskCommentRequest request)
        {
            try
            {
                var existingComment = await GetTaskCommentByIdAsync(projectId, taskId, commentId);
                if (existingComment == null)
                {
                    return new TaskCommentResponseDto
                    {
                        Success = false,
                        Message = "Không tìm thấy comment"
                    };
                }

                var taskInfo = await GetTaskInfoAsync(projectId, existingComment.TaskId);
                var endpoint = $"{BaseEndpoint}/{taskInfo.ProjectId}/tasks/{existingComment.TaskId}/comments/{commentId}/agree";
                var response = await _apiService.PatchToEndpointAsync(endpoint, request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<TaskCommentDto>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        var message = request.IsAgreed ? "Đã đồng ý với comment" : "Đã không đồng ý với comment";
                        return new TaskCommentResponseDto
                        {
                            Success = true,
                            Message = message,
                            Data = apiResponse.Data
                        };
                    }
                }

                return new TaskCommentResponseDto
                {
                    Success = false,
                    Message = "Lỗi khi cập nhật agree status"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error agreeing TaskComment: {CommentId}", commentId);
                return new TaskCommentResponseDto
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Assign TaskComment cho user thông qua API
        /// </summary>
        public async Task<TaskCommentResponseDto> AssignTaskCommentAsync(
            int projectId,
            int taskId,
            int commentId, 
            AssignTaskCommentRequest request)
        {
            try
            {
                var existingComment = await GetTaskCommentByIdAsync(projectId, taskId, commentId);
                if (existingComment == null)
                {
                    return new TaskCommentResponseDto
                    {
                        Success = false,
                        Message = "Không tìm thấy comment"
                    };
                }

                var taskInfo = await GetTaskInfoAsync(projectId, existingComment.TaskId);
                var endpoint = $"{BaseEndpoint}/{taskInfo.ProjectId}/tasks/{existingComment.TaskId}/comments/{commentId}/assign";
                var response = await _apiService.PatchToEndpointAsync(endpoint, request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<TaskCommentDto>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return new TaskCommentResponseDto
                        {
                            Success = true,
                            Message = "Comment đã được assign thành công",
                            Data = apiResponse.Data
                        };
                    }
                }

                return new TaskCommentResponseDto
                {
                    Success = false,
                    Message = "Lỗi khi assign comment"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning TaskComment: {CommentId}", commentId);
                return new TaskCommentResponseDto
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="taskId"></param>
        /// <param name="commentId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<TaskCommentResponseDto> ReviewTaskCommentAsync(
            int projectId,
            int taskId,
            int commentId,
            ReviewTaskCommentRequest request)
        {
            try
            {
                var existingComment = await GetTaskCommentByIdAsync(projectId, taskId, commentId);
                if (existingComment == null)
                {
                    return new TaskCommentResponseDto
                    {
                        Success = false,
                        Message = "Không tìm thấy comment"
                    };
                }

                var taskInfo = await GetTaskInfoAsync(projectId, existingComment.TaskId);
                var endpoint = $"{BaseEndpoint}/{taskInfo.ProjectId}/tasks/{existingComment.TaskId}/comments/{commentId}/review";
                var response = await _apiService.PatchToEndpointAsync(endpoint, request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<TaskCommentDto>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return new TaskCommentResponseDto
                        {
                            Success = true,
                            Message = "Comment đã được review thành công",
                            Data = apiResponse.Data
                        };
                    }
                }

                return new TaskCommentResponseDto
                {
                    Success = false,
                    Message = "Lỗi khi review comment"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reviewing TaskComment: {CommentId}", commentId);
                return new TaskCommentResponseDto
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<TaskCommentResponseDto> PriorityTaskCommentAsync(
            int projectId,
            int taskId,
            int commentId,
            PriorityTaskCommentRequest request)
        {
            try
            {
                var existingComment = await GetTaskCommentByIdAsync(projectId, taskId, commentId);
                if (existingComment == null)
                {
                    return new TaskCommentResponseDto
                    {
                        Success = false,
                        Message = "Không tìm thấy comment"
                    };
                }

                var taskInfo = await GetTaskInfoAsync(projectId, existingComment.TaskId);
                var endpoint = $"{BaseEndpoint}/{taskInfo.ProjectId}/tasks/{existingComment.TaskId}/comments/{commentId}/priority";
                var response = await _apiService.PatchToEndpointAsync(endpoint, request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<TaskCommentDto>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return new TaskCommentResponseDto
                        {
                            Success = true,
                            Message = "Comment đã được priority thành công",
                            Data = apiResponse.Data
                        };
                    }
                }

                return new TaskCommentResponseDto
                {
                    Success = false,
                    Message = "Lỗi khi priority comment"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error priority TaskComment: {CommentId}", commentId);
                return new TaskCommentResponseDto
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<TaskCommentResponseDto> BlockTaskCommentAsync(
    int projectId,
    int taskId,
    int commentId,
    ToggleBlockingTaskCommentRequest request)
        {
            try
            {
                var existingComment = await GetTaskCommentByIdAsync(projectId, taskId, commentId);
                if (existingComment == null)
                {
                    return new TaskCommentResponseDto
                    {
                        Success = false,
                        Message = "Không tìm thấy comment"
                    };
                }

                var taskInfo = await GetTaskInfoAsync(projectId, existingComment.TaskId);
                var endpoint = $"{BaseEndpoint}/{taskInfo.ProjectId}/tasks/{existingComment.TaskId}/comments/{commentId}/block";
                var response = await _apiService.PatchToEndpointAsync(endpoint, request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<TaskCommentDto>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return new TaskCommentResponseDto
                        {
                            Success = true,
                            Message = "Comment đã được block thành công",
                            Data = apiResponse.Data
                        };
                    }
                }

                return new TaskCommentResponseDto
                {
                    Success = false,
                    Message = "Lỗi khi block comment"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error blocking TaskComment: {CommentId}", commentId);
                return new TaskCommentResponseDto
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<TaskCommentResponseDto> RequiresDiscussionTaskCommentAsync(
    int projectId,
    int taskId,
    int commentId,
    ToggleDiscussionTaskCommentRequest request)
        {
            try
            {
                var existingComment = await GetTaskCommentByIdAsync(projectId, taskId, commentId);
                if (existingComment == null)
                {
                    return new TaskCommentResponseDto
                    {
                        Success = false,
                        Message = "Không tìm thấy comment"
                    };
                }

                var taskInfo = await GetTaskInfoAsync(projectId, existingComment.TaskId);
                var endpoint = $"{BaseEndpoint}/{taskInfo.ProjectId}/tasks/{existingComment.TaskId}/comments/{commentId}/discussion";
                var response = await _apiService.PatchToEndpointAsync(endpoint, request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<TaskCommentDto>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return new TaskCommentResponseDto
                        {
                            Success = true,
                            Message = "Comment đã được discussion thành công",
                            Data = apiResponse.Data
                        };
                    }
                }

                return new TaskCommentResponseDto
                {
                    Success = false,
                    Message = "Lỗi khi discussion comment"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error discussion TaskComment: {CommentId}", commentId);
                return new TaskCommentResponseDto
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<TaskCommentResponseDto> StatusTaskCommentAsync(
    int projectId,
    int taskId,
    int commentId,
    StatusTaskCommentRequest request)
        {
            try
            {
                var existingComment = await GetTaskCommentByIdAsync(projectId, taskId, commentId);
                if (existingComment == null)
                {
                    return new TaskCommentResponseDto
                    {
                        Success = false,
                        Message = "Không tìm thấy comment"
                    };
                }

                var taskInfo = await GetTaskInfoAsync(projectId, existingComment.TaskId);
                var endpoint = $"{BaseEndpoint}/{taskInfo.ProjectId}/tasks/{existingComment.TaskId}/comments/{commentId}/status";
                var response = await _apiService.PatchToEndpointAsync(endpoint, request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<TaskCommentDto>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return new TaskCommentResponseDto
                        {
                            Success = true,
                            Message = "Comment đã được status thành công",
                            Data = apiResponse.Data
                        };
                    }
                }

                return new TaskCommentResponseDto
                {
                    Success = false,
                    Message = "Lỗi khi status comment"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error status TaskComment: {CommentId}", commentId);
                return new TaskCommentResponseDto
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<TaskCommentResponseDto> CommentTypeTaskCommentAsync(
    int projectId,
    int taskId,
    int commentId,
    CommentTypeTaskCommentRequest request)
        {
            try
            {
                var existingComment = await GetTaskCommentByIdAsync(projectId, taskId, commentId);
                if (existingComment == null)
                {
                    return new TaskCommentResponseDto
                    {
                        Success = false,
                        Message = "Không tìm thấy comment"
                    };
                }

                var taskInfo = await GetTaskInfoAsync(projectId, existingComment.TaskId);
                var endpoint = $"{BaseEndpoint}/{taskInfo.ProjectId}/tasks/{existingComment.TaskId}/comments/{commentId}/type";
                var response = await _apiService.PatchToEndpointAsync(endpoint, request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<TaskCommentDto>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return new TaskCommentResponseDto
                        {
                            Success = true,
                            Message = "Comment đã được phân loại thành công",
                            Data = apiResponse.Data
                        };
                    }
                }

                return new TaskCommentResponseDto
                {
                    Success = false,
                    Message = "Lỗi khi type comment"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error type TaskComment: {CommentId}", commentId);
                return new TaskCommentResponseDto
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Lấy replies của TaskComment thông qua API
        /// </summary>
        public async Task<List<TaskCommentDto>> GetCommentRepliesAsync(
            int projectId,
            int taskId,
            int commentId)
        {
            try
            {
                var parentComment = await GetTaskCommentByIdAsync(projectId, taskId, commentId);
                if (parentComment == null)
                {
                    return new List<TaskCommentDto>();
                }

                var taskInfo = await GetTaskInfoAsync( projectId, parentComment.TaskId);
                var endpoint = $"{BaseEndpoint}/{taskInfo.ProjectId}/tasks/{parentComment.TaskId}/comments/{commentId}/replies";
                var response = await _apiService.GetFromEndpointAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<List<TaskCommentDto>>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }
                }

                return CreateMockReplies(commentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting replies for comment: {CommentId}", commentId);
                return CreateMockReplies(commentId);
            }
        }

        /// <summary>
        /// Lấy thống kê TaskComment thông qua API
        /// </summary>
        public async Task<TaskCommentStats> GetTaskCommentStatsAsync(int projectId, int taskId)
        {
            try
            {
                var taskInfo = await GetTaskInfoAsync(projectId, taskId);
                if (taskInfo == null)
                {
                    return null;
                }

                var endpoint = $"{BaseEndpoint}/{taskInfo.ProjectId}/tasks/{taskId}/comments/stats";
                var response = await _apiService.GetFromEndpointAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<TaskCommentStats>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comment stats for task: {TaskId}", taskId);
                return null;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Lấy thông tin task
        /// </summary>
        public async Task<TaskInfoDto> GetTaskInfoAsync(int projectId,int taskId)
        {
            try
            {
                // call API
                var projectTaskInfo = await _projectApiService.GetTaskByIdAsync(projectId, taskId);
                var taskInfo = new TaskInfoDto
                {
                    Id = projectTaskInfo.Id,
                    ProjectId = projectTaskInfo.ProjectId,
                    Title = projectTaskInfo.Title,
                    TaskCode = projectTaskInfo.TaskCode,
                    Status = projectTaskInfo.Status
                };
                return taskInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task info: {TaskId}", taskId);
                return null;
            }
        }

        /// <summary>
        /// Build query parameters cho GetTaskComments
        /// </summary>
        private string BuildTaskCommentsQuery(GetTaskCommentsRequest request)
        {
            var queryParams = new List<string>();

            if(request.TaskId > 0)
                queryParams.Add($"taskId={request.TaskId}");

            if (request.ParentTaskCommentId.HasValue)
                queryParams.Add($"ParentTaskCommentId={request.ParentTaskCommentId}");

            if (request.CommentTypes?.Count > 0)
            {
                foreach (var type in request.CommentTypes)
                {
                    queryParams.Add($"commentTypes={type}");
                }
            }

            if (request.CommentStatuses?.Count > 0)
            {
                foreach (var status in request.CommentStatuses)
                {
                    queryParams.Add($"commentStatuses={status}");
                }
            }

            if (request.Priorities?.Count > 0)
            {
                foreach (var priority in request.Priorities)
                {
                    queryParams.Add($"priorities={priority}");
                }
            }

            if (request.ReviewerId.HasValue)
                queryParams.Add($"reviewerId={request.ReviewerId}");

            if (request.AssignedToId.HasValue)
                queryParams.Add($"assignedToId={request.AssignedToId}");

            if (request.CreatedBy.HasValue)
                queryParams.Add($"createdBy={request.CreatedBy}");

            if (request.FromDate.HasValue)
                queryParams.Add($"fromDate={request.FromDate.Value:yyyy-MM-dd}");

            if (request.ToDate.HasValue)
                queryParams.Add($"toDate={request.ToDate.Value:yyyy-MM-dd}");

            if (request.IsResolved.HasValue)
                queryParams.Add($"isResolved={request.IsResolved}");

            if (request.IsVerified.HasValue)
                queryParams.Add($"isVerified={request.IsVerified}");

            if (request.IsBlocking.HasValue)
                queryParams.Add($"isBlocking={request.IsBlocking}");

            if (request.RequiresDiscussion.HasValue)
                queryParams.Add($"requiresDiscussion={request.RequiresDiscussion}");

            if (!string.IsNullOrWhiteSpace(request.SearchContent))
                queryParams.Add($"searchContent={Uri.EscapeDataString(request.SearchContent)}");

            if (!string.IsNullOrWhiteSpace(request.SearchIssueTitle))
                queryParams.Add($"searchIssueTitle={Uri.EscapeDataString(request.SearchIssueTitle)}");

            if (request.Tags?.Count > 0)
            {
                foreach (var tag in request.Tags)
                {
                    queryParams.Add($"tags={Uri.EscapeDataString(tag)}");
                }
            }

            queryParams.Add($"page={request.Page}");
            queryParams.Add($"pageSize={request.PageSize}");
            queryParams.Add($"sortBy={request.SortBy}");
            queryParams.Add($"sortDirection={request.SortDirection}");
            queryParams.Add($"includeReplies={request.IncludeReplies}");
            queryParams.Add($"includeSystemComments={request.IncludeSystemComments}");
            queryParams.Add($"includeDeleted={request.IncludeDeleted}");

            return queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
        }

        #endregion

        #region Validation Methods

        /// <summary>
        /// Validate CreateTaskCommentRequest
        /// </summary>
        private ValidationResult ValidateCreateRequest(CreateTaskCommentRequest request)
        {
            var result = new ValidationResult { IsValid = true };

            if (request == null)
            {
                result.IsValid = false;
                result.Errors.Add("Request không được null");
                return result;
            }

            // Data Annotations validation
            var context = new ValidationContext(request);
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var isValid = Validator.TryValidateObject(request, context, validationResults, true);

            if (!isValid)
            {
                result.IsValid = false;
                result.Errors.AddRange(validationResults.Select(vr => vr.ErrorMessage));
            }

            // Custom validation
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                result.IsValid = false;
                result.Errors.Add("Nội dung comment không được để trống");
            }

            if (request.Content?.Length > 4000)
            {
                result.IsValid = false;
                result.Errors.Add("Nội dung comment không được vượt quá 4000 ký tự");
            }

            if (request.TaskId <= 0)
            {
                result.IsValid = false;
                result.Errors.Add("TaskId phải lớn hơn 0");
            }

            if (request.EstimatedFixTime < 0 || request.EstimatedFixTime > 999.99m)
            {
                result.IsValid = false;
                result.Errors.Add("Thời gian ước tính phải từ 0 đến 999.99 giờ");
            }

            return result;
        }

        /// <summary>
        /// Validate UpdateTaskCommentRequest
        /// </summary>
        private ValidationResult ValidateUpdateRequest(UpdateTaskCommentRequest request)
        {
            var result = new ValidationResult { IsValid = true };

            if (request == null)
            {
                result.IsValid = false;
                result.Errors.Add("Request không được null");
                return result;
            }

            // Data Annotations validation
            var context = new ValidationContext(request);
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var isValid = Validator.TryValidateObject(request, context, validationResults, true);

            if (!isValid)
            {
                result.IsValid = false;
                result.Errors.AddRange(validationResults.Select(vr => vr.ErrorMessage));
            }

            // Custom validation
            if (request.Id <= 0)
            {
                result.IsValid = false;
                result.Errors.Add("Comment ID phải lớn hơn 0");
            }

            if(string.IsNullOrWhiteSpace(request.Content))
            {
                result.IsValid = false;
                result.Errors.Add("Nội dung comment không được để trống");
            }

            if (request.Content?.Length > 4000)
            {
                result.IsValid = false;
                result.Errors.Add("Nội dung comment không được vượt quá 4000 ký tự");
            }

            if (request.Version <= 0)
            {
                result.IsValid = false;
                result.Errors.Add("Version phải lớn hơn 0");
            }

            if (request.EstimatedFixTime < 0 || request.EstimatedFixTime > 999.99m)
            {
                result.IsValid = false;
                result.Errors.Add("Thời gian ước tính phải từ 0 đến 999.99 giờ");
            }

            if (request.ActualFixTime < 0 || request.ActualFixTime > 999.99m)
            {
                result.IsValid = false;
                result.Errors.Add("Thời gian thực tế phải từ 0 đến 999.99 giờ");
            }

            return result;
        }

        /// <summary>
        /// Validate business rules cho Create operation
        /// </summary>
        private async Task ValidateCreateBusinessRulesAsync(int projectId, CreateTaskCommentRequest request)
        {
            // Check if task exists
            var taskInfo = await GetTaskInfoAsync( projectId, request.TaskId);
            if (taskInfo == null)
            {
                throw new BusinessRuleException("Task không tồn tại");
            }

            // Validate parent comment if specified
            if (request.ParentCommentId.HasValue)
            {
                var parentComment = await GetTaskCommentByIdAsync(projectId, request.TaskId, request.ParentCommentId.Value);
                if (parentComment == null)
                {
                    throw new BusinessRuleException("Parent comment không tồn tại");
                }

                if (parentComment.TaskId != request.TaskId)
                {
                    throw new BusinessRuleException("Parent comment không thuộc task này");
                }
            }

            // Validate mentioned users
            if (request.MentionedUsers?.Count > 0)
            {
                foreach (var username in request.MentionedUsers)
                {
                    if (string.IsNullOrWhiteSpace(username))
                    {
                        throw new BusinessRuleException("Username được mention không được để trống");
                    }
                }
            }

            // Validate reviewer and assignee
            if (request.ReviewerId.HasValue && request.ReviewerId.Value <= 0)
            {
                throw new BusinessRuleException("ReviewerId không hợp lệ");
            }

            if (request.AssignedToId.HasValue && request.AssignedToId.Value <= 0)
            {
                throw new BusinessRuleException("AssignedToId không hợp lệ");
            }

            // Validate due date
            if (request.DueDate.HasValue && request.DueDate.Value < DateTime.Now.Date)
            {
                throw new BusinessRuleException("Due date không thể trong quá khứ");
            }

            // Validate blocking logic
            if (request.IsBlocking && request.CommentType == CommentType.General)
            {
                throw new BusinessRuleException("Comment loại General không thể đánh dấu là blocking");
            }
        }

        /// <summary>
        /// Validate business rules cho Update operation
        /// </summary>
        private async Task ValidateUpdateBusinessRulesAsync(
            UpdateTaskCommentRequest request, TaskCommentDto existingComment)
        {
            // Check version for optimistic concurrency
            if (request.Version != existingComment.Version)
            {
                throw new BusinessRuleException(
                    "Comment đã được cập nhật bởi người khác. Vui lòng refresh và thử lại.");
            }

            // Check status transition logic
            if (request.CommentStatus == TaskStatuss.Completed && string.IsNullOrWhiteSpace(request.ResolutionNotes))
            {
                throw new BusinessRuleException(
                    "Ghi chú giải quyết là bắt buộc khi đánh dấu comment hoàn thành");
            }

            // Check verification logic
            if (request.IsVerified && string.IsNullOrWhiteSpace(request.VerificationNotes))
            {
                throw new BusinessRuleException(
                    "Ghi chú verification là bắt buộc khi verify comment");
            }

            // Check permission to modify resolved comment
            if (existingComment.ResolvedAt.HasValue &&
                request.CommentStatus != TaskStatuss.Completed &&
                !IsCurrentUserAllowedToReopenComment(existingComment))
            {
                throw new BusinessRuleException(
                    "Bạn không có quyền mở lại comment đã được resolve");
            }

            // Validate actual time when completed
            if (request.CommentStatus == TaskStatuss.Completed && request.ActualFixTime <= 0)
            {
                throw new BusinessRuleException(
                    "Thời gian thực tế phải lớn hơn 0 khi hoàn thành comment");
            }
        }

        /// <summary>
        /// Validate permission to delete comment
        /// </summary>
        private async Task ValidateDeletePermissionAsync(int projectId, TaskCommentDto comment)
        {
            var currentUser = _userService.GetCurrentUser();
            if (currentUser == null)
            {
                throw new UnauthorizedAccessException("Người dùng chưa đăng nhập");
            }

            // Only comment creator, project manager, or admin can delete
            if (comment.CreatedBy != currentUser.Id &&
                currentUser.Role != UserRole.Admin &&
                currentUser.Role != UserRole.Manager)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền xóa comment này");
            }

            // Cannot delete resolved comment
            if (comment.ResolvedAt.HasValue)
            {
                throw new BusinessRuleException("Không thể xóa comment đã được resolve");
            }

            // Cannot delete comment with replies
            var replies = await GetCommentRepliesAsync(projectId, comment.TaskId, comment.Id);
            if (replies.Count > 0)
            {
                throw new BusinessRuleException("Không thể xóa comment có replies");
            }
        }

        /// <summary>
        /// Validate permission to resolve comment
        /// </summary>
        private async Task ValidateResolvePermissionAsync(TaskCommentDto comment)
        {
            var currentUser = _userService.GetCurrentUser();
            if (currentUser == null)
            {
                throw new UnauthorizedAccessException("Người dùng chưa đăng nhập");
            }

            // Only assignee, reviewer, or project manager can resolve
            if (comment.AssignedToId != currentUser.Id &&
                comment.ReviewerId != currentUser.Id &&
                currentUser.Role != UserRole.Admin &&
                currentUser.Role != UserRole.Manager)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền resolve comment này");
            }

        }

        /// <summary>
        /// Validate permission to verify comment
        /// </summary>
        private async Task ValidateVerifyPermissionAsync(TaskCommentDto comment)
        {
            var currentUser = _userService.GetCurrentUser();
            if (currentUser == null)
            {
                throw new UnauthorizedAccessException("Người dùng chưa đăng nhập");
            }

            // Only reviewer or project manager can verify
            if (comment.ReviewerId != currentUser.Id &&
                currentUser.Role != UserRole.Admin &&
                currentUser.Role != UserRole.Manager)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền verify comment này");
            }

            if (!comment.ResolvedAt.HasValue)
            {
                throw new BusinessRuleException("Comment phải được resolve trước khi verify");
            }

            if (comment.IsVerified)
            {
                throw new BusinessRuleException("Comment đã được verify");
            }
        }

        /// <summary>
        /// Check if current user can reopen resolved comment
        /// </summary>
        private bool IsCurrentUserAllowedToReopenComment(TaskCommentDto comment)
        {
            var currentUser = _userService.GetCurrentUser();
            if (currentUser == null)
                return false;

            // Admin and Manager can reopen
            if (currentUser.Role == UserRole.Admin || currentUser.Role == UserRole.Manager)
                return true;

            // Comment creator can reopen within 24 hours
            if (comment.CreatedBy == currentUser.Id &&
                comment.ResolvedAt.HasValue &&
                comment.ResolvedAt.Value > DateTime.UtcNow.AddDays(-1))
                return true;

            return false;
        }

        #endregion

        #region Mock Data Methods (Fallback)

        /// <summary>
        /// Lấy comment theo ID từ mock data
        /// </summary>
        private async Task<TaskCommentDto> GetCommentByIdFromMockAsync(int commentId)
        {
            return await Task.FromResult(new TaskCommentDto
            {
                Id = commentId,
                TaskId = 1,
                Content = $"Sample comment content {commentId}",
                CommentType = CommentType.General,
                CommentStatus = TaskStatuss.Todo,
                Priority = TaskPriority.Low,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                CreatedBy = 1,
                CreatedByName = "Test User",
                Version = 1,
                Tags = new List<string> { "sample", "mock" },
                RelatedFiles = new List<string>(),
                RelatedScreenshots = new List<string>(),
                RelatedDocuments = new List<string>(),
                Attachments = new List<string>(),
                MentionedUsers = new List<string>()
            });
        }

        /// <summary>
        /// Tạo mock response cho GetTaskCommentsPagedAsync
        /// </summary>
        private TaskCommentsPagedResponseDto CreateMockCommentsPagedResponse(GetTaskCommentsRequest request)
        {
            var mockComments = new List<TaskCommentDto>();

            for (int i = 1; i <= Math.Min(request.PageSize, 5); i++)
            {
                mockComments.Add(new TaskCommentDto
                {
                    Id = i,
                    TaskId = request.TaskId,
                    Content = $"Mock comment {i} for task {request.TaskId}",
                    CommentType = (CommentType)(i % 4),
                    CommentStatus = (TaskStatuss)(i % 3),
                    Priority = (TaskPriority)(i % 4),
                    CreatedAt = DateTime.UtcNow.AddDays(-i),
                    CreatedBy = i,
                    CreatedByName = $"User {i}",
                    Version = 1
                });
            }

            return new TaskCommentsPagedResponseDto
            {
                Success = true,
                Message = "Mock data loaded successfully",
                Data = mockComments,
                Pagination = new PaginationMetadata
                {
                    CurrentPage = request.Page,
                    PageSize = request.PageSize,
                    TotalPages = 2,
                    TotalCount = mockComments.Count + 5,
                    HasPrevious = request.Page > 1,
                    HasNext = request.Page < 2
                }
            };
        }

        /// <summary>
        /// Tạo mock replies
        /// </summary>
        private List<TaskCommentDto> CreateMockReplies(int parentCommentId)
        {
            return new List<TaskCommentDto>
            {
                new TaskCommentDto
                {
                    Id = parentCommentId + 1000,
                    TaskId = 1,
                    ParentCommentId = parentCommentId,
                    Content = $"Reply to comment {parentCommentId}",
                    CommentType = CommentType.General,
                    CommentStatus = TaskStatuss.Todo,
                    Priority = TaskPriority.Low,
                    CreatedAt = DateTime.UtcNow.AddHours(-2),
                    CreatedBy = 2,
                    CreatedByName = "Reply User",
                    Version = 1
                }
            };
        }

        /// <summary>
        /// Tạo mock stats
        /// </summary>
        private TaskCommentStats CreateMockStats(int taskId)
        {
            return new TaskCommentStats
            {
                TaskId = taskId,
                TotalComments = 15,
                PendingComments = 8,
                ResolvedComments = 6,
                BlockingComments = 1,
                VerifiedComments = 5,
                TotalEstimatedTime = 45.5m,
                TotalActualTime = 38.2m,
                OverdueComments = 2,
                CommentsByType = new Dictionary<CommentType, int>
                {
                    { CommentType.General, 8 },
                    { CommentType.IssueReport, 4 },
                    { CommentType.ChangeRequest, 2 },
                    { CommentType.StatusUpdate, 1 }
                },
                CommentsByStatus = new Dictionary<TaskStatuss, int>
                {
                    { TaskStatuss.Todo, 5 },
                    { TaskStatuss.InProgress, 3 },
                    { TaskStatuss.Completed, 6 },
                    { TaskStatuss.Blocked, 1 }
                },
                CommentsByPriority = new Dictionary<TaskPriority, int>
                {
                    { TaskPriority.Low, 6 },
                    { TaskPriority.Normal, 5 },
                    { TaskPriority.High, 3 },
                    { TaskPriority.Critical, 1 }
                }
            };
        }

        #endregion

        #region Supporting Classes

        /// <summary>
        /// Validation result class
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid { get; set; } = true;
            public List<string> Errors { get; set; } = new List<string>();
        }

        /// <summary>
        /// Business rule exception
        /// </summary>
        public class BusinessRuleException : Exception
        {
            public BusinessRuleException(string message) : base(message) { }
            public BusinessRuleException(string message, Exception innerException) : base(message, innerException) { }
        }

        /// <summary>
        /// Task info DTO
        /// </summary>
        public class TaskInfoDto
        {
            public int Id { get; set; }
            public int ProjectId { get; set; }
            public string Title { get; set; }
            public string TaskCode { get; set; }
            public TaskStatuss Status { get; set; }
        }

        #endregion
    }
}
