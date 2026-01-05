using ManagementFile.App.Models;
using ManagementFile.App.ViewModels.LogInOut;
using ManagementFile.Contracts.DTOs.Common;
using ManagementFile.Contracts.DTOs.ProjectManagement;
using ManagementFile.Contracts.Enums;
using ManagementFile.Contracts.Enums.Extensions;
using ManagementFile.Contracts.Requests.ProjectManagement.ProjectMembers;
using ManagementFile.Contracts.Requests.ProjectManagement.Projects;
using ManagementFile.Contracts.Requests.ProjectManagement.ProjectTasks;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ManagementFile.App.Services
{
    /// <summary>
    /// Enhanced ProjectService with real API integration
    /// Replacing ProjectManagementService.cs with better architecture
    /// </summary>
    public class ProjectApiService
    {
        private readonly ApiService _apiService;
        private readonly UserManagementService _userService;
        private static readonly object _lock = new object();
        private const string BaseEndpoint = "api/projects";

        public ProjectApiService(ApiService apiService, UserManagementService userService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }


        #region Projects APIs (Real Integration)

        /// <summary>
        /// Lấy danh sách projects với pagination và filter - Real API
        /// </summary>
        public async Task<ProjectManagementPagedResult<ProjectDto>> GetProjectsAsync(
            ProjectFilterRequest filter)
        {
            try
            {
                var queryParams = BuildProjectFilterQuery(filter);
                var endpoint = $"{BaseEndpoint}{queryParams}";
                
                var response = await _apiService.GetFromEndpointAsync(endpoint);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<PagedResult<ProjectDto>>>(content);
                    
                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return new ProjectManagementPagedResult<ProjectDto>
                        {
                            Data = apiResponse.Data.Items,
                            TotalCount = apiResponse.Data.TotalCount,
                            PageNumber = apiResponse.Data.PageNumber,
                            PageSize = apiResponse.Data.PageSize,
                            TotalPages = apiResponse.Data.TotalPages,
                            HasPreviousPage = apiResponse.Data.HasPreviousPage,
                            HasNextPage = apiResponse.Data.HasNextPage
                        };
                    }
                }
                else 
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectDto>>(errorContent);
                    throw new Exception($"Error: {errorResponse?.Message ?? errorContent}");
                }

                // Fallback to mock data on API failure
                return null;
            }
            catch (Exception)
            {
                // Fallback to mock data on exception
                return null;
            }
        }

        /// <summary>
        /// Lấy project theo ID - Real API
        /// </summary>
        public async Task<ProjectDto> GetProjectByIdAsync(int projectId)
        {
            try
            {
                if (projectId < 0)
                    throw new ArgumentException("Project ID không được để trống");

                var endpoint = $"{BaseEndpoint}/{projectId}";
                var response = await _apiService.GetFromEndpointAsync(endpoint);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectDto>>(content);
                    
                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectDto>>(errorContent);
                    throw new Exception($"Error: {errorResponse?.Message ?? errorContent}");
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Tạo project mới - Real API
        /// </summary>
        public async Task<ProjectDto> CreateProjectAsync(CreateProjectRequest request)
        {
            try
            {

                // 2. BUSINESS LOGIC VALIDATION (if needed)
                await ValidateBusinessRules(request);

                // Log thông tin request
                System.Diagnostics.Debug.WriteLine($"🔧 Creating project with data type: {request.GetType().Name}");

                // 3. GỌI API
                //await ProgressiveProjectTest();

                var response = await _apiService.PostToEndpointAsync(BaseEndpoint, request);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectDto>>(content);
                    
                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectDto>>(errorContent);
                    throw new Exception($"Error: {errorResponse?.Message ?? errorContent}");
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi tạo project: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Cập nhật project - Real API
        /// </summary>
        public async Task<ProjectDto> UpdateProjectAsync(int projectId, UpdateProjectRequest request)
        {
            try
            {
                var endpoint = $"{BaseEndpoint}/{projectId}";
                var response = await _apiService.PutToEndpointAsync(endpoint, request);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectDto>>(content);
                    
                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectDto>>(errorContent);
                    throw new Exception($"Error: {errorResponse?.Message ?? errorContent}");
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi cập nhật project: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Xóa project - Real API
        /// </summary>
        public async Task<bool> DeleteProjectAsync(int projectId)
        {
            try
            {
                var endpoint = $"{BaseEndpoint}/{projectId}";
                var response = await _apiService.DeleteFromEndpointAsync(endpoint);

                if(!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectDto>>(content);
                    throw new Exception($"Error: {errorResponse?.Message ?? content}");
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi xóa project: {ex.Message}", ex);
            }
        }

        #endregion

        #region Project Tasks APIs (Real Integration)

        /// <summary>
        /// Lấy danh sách tasks của project - Real API
        /// </summary>
        public async Task<ProjectManagementPagedResult<ProjectTaskDto>> GetProjectTasksAsync(
            TaskFilterRequest filter)
        {
            try
            {

                var queryParams = BuildTaskFilterQuery(filter);
                var endpoint = $"{BaseEndpoint}/{filter.ProjectId}/tasks{queryParams}";
                
                var response = await _apiService.GetFromEndpointAsync(endpoint);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<PagedResult<ProjectTaskDto>>>(content);
                    
                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return new ProjectManagementPagedResult<ProjectTaskDto>
                        {
                            Data = apiResponse.Data.Items,
                            TotalCount = apiResponse.Data.TotalCount,
                            PageNumber = apiResponse.Data.PageNumber,
                            PageSize = apiResponse.Data.PageSize,
                            TotalPages = apiResponse.Data.TotalPages,
                            HasPreviousPage = apiResponse.Data.HasPreviousPage,
                            HasNextPage = apiResponse.Data.HasNextPage
                        };
                    }
                }
                else 
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectTaskDto>>(errorContent);
                    throw new Exception($"Error: {errorResponse?.Message ?? errorContent}");
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Lấy task theo ID
        /// </summary>
        public async Task<ProjectTaskDto> GetTaskByIdAsync(int projectId, int taskId)
        {
            try
            {
                var response = await _apiService.GetFromEndpointAsync($"{BaseEndpoint}/{projectId}/tasks/{taskId}");
                var json = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectTaskDto>>(json);
                
                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    return apiResponse.Data;
                }
                else
                {
                    throw new Exception($"Error: {apiResponse?.Message}");
                }

                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Tạo task mới - Real API
        /// </summary>
        public async Task<ProjectTaskDto> CreateTaskAsync(int projectId, CreateTaskRequest model)
        {
            try
            {

                var endpoint = $"{BaseEndpoint}/{projectId}/tasks";
                var response = await _apiService.PostToEndpointAsync(endpoint, model);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectTaskDto>>(content);
                    
                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectTaskDto>>(errorContent);
                    throw new Exception($"Error: {errorResponse?.Message ?? errorContent}");
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi tạo task: {ex.Message}", ex);
            }
        }
        /// <summary>
        /// Cập nhật task - Real API
        /// </summary>
        public async Task<ProjectTaskDto> UpdateTaskAsync(int projectId, int taskId, UpdateTaskRequest request)
        {
            try
            {
                // 1. VALIDATION
                if (request == null)
                    throw new ArgumentNullException(nameof(request), "Update request không được null");

                if (taskId <= 0)
                    throw new ArgumentException("Task ID phải lớn hơn 0", nameof(taskId));

                // Validate request using data annotations
                var validationResult = ValidateUpdateTaskRequest(request);
                if (!validationResult.IsValid)
                {
                    throw new ValidationException($"Validation failed: {string.Join(", ", validationResult.Errors)}");
                }

                // 2. BUSINESS LOGIC VALIDATION
                await ValidateUpdateTaskBusinessRules(request);

                // 3. GỌI API
                var endpoint = $"{BaseEndpoint}/{projectId}/tasks/{taskId}";
                var response = await _apiService.PutToEndpointAsync(endpoint, request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectTaskDto>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectTaskDto>>(errorContent);
                    throw new Exception($"Error: {errorResponse?.Message ?? errorContent}");
                }

                // Fallback to mock data on API failure
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi cập nhật task: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Xóa task
        /// </summary>
        public async Task<bool> DeleteTaskAsync(int projectId, int taskId)
        {
            try
            {
                var response = await _apiService.DeleteFromEndpointAsync($"{BaseEndpoint}/{projectId}/tasks/{taskId}");

                if(!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectTaskDto>>(content);
                    throw new Exception($"Error: {errorResponse?.Message ?? content}");
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi xóa task: {ex.Message}", ex);
            }
        }

        // bo sung tien do
        public async Task<ProjectTaskDto> UpdateTaskProgressAsync(int projectId, int taskId, TaskProgressUpdateRequest request)
        {
            try
            {
                var endpoint = $"{BaseEndpoint}/{projectId}/tasks/{taskId}/progress";
                var response = await _apiService.PatchToEndpointAsync(endpoint, request);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectTaskDto>>(content);
                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }
                    else
                    {
                        throw new Exception($"Error: {apiResponse?.Message}");
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi cập nhật tiến độ task: {ex.Message}", ex);
            }
        }

        #endregion

        #region Project Members APIs (Real Integration)

        /// <summary>
        /// Lấy members của project - Real API
        /// </summary>
        public async Task<List<ProjectMemberDto>> GetProjectMembersAsync(int projectId)
        {
            try
            {
                var endpoint = $"{BaseEndpoint}/{projectId}/members";
                var response = await _apiService.GetFromEndpointAsync(endpoint);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<List<ProjectMemberDto>>>(content);
                    
                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonConvert.DeserializeObject<ApiResponse<List<ProjectMemberDto>>>(errorContent);
                    throw new Exception($"Error: {errorResponse?.Message ?? errorContent}");
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<ProjectMemberDto> GetProjectMemberByIdAsync(int projectId, int memberId)
        {
            try
            {
                var endpoint = $"{BaseEndpoint}/{projectId}/membersget/{memberId}";
                var response = await _apiService.GetFromEndpointAsync(endpoint);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectMemberDto>>(content);
                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectMemberDto>>(errorContent);
                    throw new Exception($"Error: {errorResponse?.Message ?? errorContent}");
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Thêm thành viên vào project - Real API
        /// </summary>
        public async Task<ProjectMemberDto> AddProjectMemberAsync(int projectId, CreateProjectMemberRequest model)
        {
            try
            {
                //var request = new
                //{
                //    userId = model.UserId,
                //    projectRole = model.ProjectRole,
                //    allocationPercentage = model.AllocationPercentage,
                //    hourlyRate = model.HourlyRate,
                //    notes = model.Notes
                //};

                var endpoint = $"{BaseEndpoint}/{projectId}/members";
                var response = await _apiService.PostToEndpointAsync(endpoint, model);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectMemberDto>>(content);
                    
                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectMemberDto>>(errorContent);
                    throw new Exception($"Error: {errorResponse?.Message ?? errorContent}");
                }

                // Return mock member on API failure
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi thêm member: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Xóa member khỏi project
        /// </summary>
        public async Task<bool> RemoveProjectMemberAsync(int projectId, int userId)
        {
            try
            {
                var response = await _apiService.DeleteFromEndpointAsync($"{BaseEndpoint}/{projectId}/members/{userId}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectMemberDto>>(errorContent);
                    throw new Exception($"Error: {errorResponse?.Message ?? errorContent}");
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi xóa member: {ex.Message}", ex);
            }
        }



        /// <summary>
        /// Cập nhật thành viên project - Real API
        /// </summary>
        public async Task<ProjectMemberDto> UpdateProjectMemberAsync(int projectId, int memberId, UpdateProjectMemberRequest model)
        {
            try
            {
                //var request = new
                //{
                //    projectRole = model.ProjectRole,
                //    allocationPercentage = model.AllocationPercentage,
                //    hourlyRate = model.HourlyRate,
                //    notes = model.Notes,

                //};

                var endpoint = $"{BaseEndpoint}/{projectId}/members/{memberId}";
                var response = await _apiService.PutToEndpointAsync(endpoint, model);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectMemberDto>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectMemberDto>>(errorContent);
                    throw new Exception($"Error: {errorResponse?.Message ?? errorContent}");
                }

                // Return mock member on API failure
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi cập nhật member: {ex.Message}", ex);
            }
        }

        #endregion

        #region Project Hierarchy APIs

        /// <summary>
        /// Lấy danh sách children của một project - Real API
        /// GET: api/projects/{parentId}/children?pageNumber=1&pageSize=20
        /// </summary>
        public async Task<ProjectManagementPagedResult<ProjectDto>> GetProjectChildrenAsync(
            int parentProjectId,
            int pageNumber = 1,
            int pageSize = 20)
        {
            try
            {
                var queryParams = $"?pageNumber={pageNumber}&pageSize={pageSize}";
                var endpoint = $"{BaseEndpoint}/{parentProjectId}/children{queryParams}";

                var response = await _apiService.GetFromEndpointAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<PagedResult<ProjectDto>>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return new ProjectManagementPagedResult<ProjectDto>
                        {
                            Data = apiResponse.Data.Items,
                            TotalCount = apiResponse.Data.TotalCount,
                            PageNumber = apiResponse.Data.PageNumber,
                            PageSize = apiResponse.Data.PageSize,
                            TotalPages = apiResponse.Data.TotalPages,
                            HasPreviousPage = apiResponse.Data.HasPreviousPage,
                            HasNextPage = apiResponse.Data.HasNextPage
                        };
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectDto>>(errorContent);
                    throw new Exception($"Error: {errorResponse?.Message ?? errorContent}");
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting project children: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Lấy toàn bộ hierarchy tree của một project - Real API
        /// GET: api/projects/{id}/hierarchy?maxDepth=3
        /// </summary>
        public async Task<ProjectDto> GetProjectHierarchyAsync(
            int projectId,
            int maxDepth = 3)
        {
            try
            {
                var endpoint = $"{BaseEndpoint}/{projectId}/hierarchy?maxDepth={maxDepth}";
                var response = await _apiService.GetFromEndpointAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectDto>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectDto>>(errorContent);
                    throw new Exception($"Error: {errorResponse?.Message ?? errorContent}");
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi lấy hierarchy: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Di chuyển project sang parent mới - Real API
        /// PUT: api/projects/{id}/move
        /// </summary>
        public async Task<ProjectDto> MoveProjectAsync(
            int projectId,
            int? newParentId,
            string reason = "")
        {
            try
            {
                var request = new
                {
                    NewParentId = newParentId,
                    Reason = reason
                };

                var endpoint = $"{BaseEndpoint}/{projectId}/move";
                var response = await _apiService.PutToEndpointAsync(endpoint, request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectDto>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectDto>>(errorContent);
                    throw new Exception($"Error: {errorResponse?.Message ?? errorContent}");
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi di chuyển project: {ex.Message}", ex);
            }
        }

        #endregion

        #region Project Tasks Hierarchy APIs (- Compatible with .NET Framework 4.8)

        /// <summary>
        /// Lấy danh sách subtasks của một task - Real API
        /// GET: api/projects/{projectId}/tasks/{parentTaskId}/subtasks
        /// </summary>
        public async Task<List<ProjectTaskDto>> GetSubTasksAsync(int projectId, int parentTaskId)
        {
            try
            {
                var endpoint = $"{BaseEndpoint}/{projectId}/tasks/{parentTaskId}/subtasks";
                var response = await _apiService.GetFromEndpointAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<List<ProjectTaskDto>>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonConvert.DeserializeObject<ApiResponse<List<ProjectTaskDto>>>(errorContent);
                    throw new Exception($"Error: {errorResponse?.Message ?? errorContent}");
                }

                return new List<ProjectTaskDto>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting subtasks: {ex.Message}");
                return new List<ProjectTaskDto>();
            }
        }

        /// <summary>
        /// Lấy subtasks với pagination (lazy loading) - Real API
        /// GET: api/projects/{projectId}/tasks/{parentTaskId}/subtasks/paged?page=1&pageSize=20
        /// </summary>
        public async Task<ProjectManagementPagedResult<ProjectTaskDto>> GetSubTasksPagedAsync(
            int projectId,
            int parentTaskId,
            int pageNumber = 1,
            int pageSize = 20)
        {
            try
            {
                var queryParams = $"?page={pageNumber}&pageSize={pageSize}";
                var endpoint = $"{BaseEndpoint}/{projectId}/tasks/{parentTaskId}/subtasks/paged{queryParams}";

                var response = await _apiService.GetFromEndpointAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<PagedResult<ProjectTaskDto>>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return new ProjectManagementPagedResult<ProjectTaskDto>
                        {
                            Data = apiResponse.Data.Items,
                            TotalCount = apiResponse.Data.TotalCount,
                            PageNumber = apiResponse.Data.PageNumber,
                            PageSize = apiResponse.Data.PageSize,
                            TotalPages = apiResponse.Data.TotalPages,
                            HasPreviousPage = apiResponse.Data.HasPreviousPage,
                            HasNextPage = apiResponse.Data.HasNextPage
                        };
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonConvert.DeserializeObject<ApiResponse<List<ProjectTaskDto>>>(errorContent);
                    throw new Exception($"Error: {errorResponse?.Message ?? errorContent}");
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting paged subtasks: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Lấy full hierarchy tree của một task - Real API
        /// GET: api/projects/{projectId}/tasks/{taskId}/hierarchy?maxDepth=3
        /// </summary>
        public async Task<ProjectTaskDto> GetTaskHierarchyAsync(
            int projectId,
            int taskId,
            int maxDepth = 3)
        {
            try
            {
                var endpoint = $"{BaseEndpoint}/{projectId}/tasks/{taskId}/hierarchy?maxDepth={maxDepth}";
                var response = await _apiService.GetFromEndpointAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectTaskDto>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectTaskDto>>(errorContent);
                    throw new Exception($"Error: {errorResponse?.Message ?? errorContent}");
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi lấy task hierarchy: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Di chuyển task sang parent mới - Real API
        /// PATCH: api/projects/{projectId}/tasks/{taskId}/move
        /// </summary>
        public async Task<ProjectTaskDto> MoveTaskAsync(
            int projectId,
            int taskId,
            int? newParentTaskId,
            string reason = "")
        {
            try
            {
                var request = new
                {
                    NewParentTaskId = newParentTaskId,
                    Reason = reason
                };

                var endpoint = $"{BaseEndpoint}/{projectId}/tasks/{taskId}/move";
                var response = await _apiService.PatchToEndpointAsync(endpoint, request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectTaskDto>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectTaskDto>>(errorContent);
                    throw new Exception($"Error: {errorResponse?.Message ?? errorContent}");
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi di chuyển task: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Lấy root tasks (tasks không có parent) - Real API
        /// GET: api/projects/{projectId}/tasks/root?page=1&pageSize=20
        /// </summary>
        public async Task<ProjectManagementPagedResult<ProjectTaskDto>> GetRootTasksAsync(
            int projectId,
            int pageNumber = 1,
            int pageSize = 20)
        {
            try
            {
                var queryParams = $"?page={pageNumber}&pageSize={pageSize}";
                var endpoint = $"{BaseEndpoint}/{projectId}/tasks/root{queryParams}";

                var response = await _apiService.GetFromEndpointAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<PagedResult<ProjectTaskDto>>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return new ProjectManagementPagedResult<ProjectTaskDto>
                        {
                            Data = apiResponse.Data.Items,
                            TotalCount = apiResponse.Data.TotalCount,
                            PageNumber = apiResponse.Data.PageNumber,
                            PageSize = apiResponse.Data.PageSize,
                            TotalPages = apiResponse.Data.TotalPages,
                            HasPreviousPage = apiResponse.Data.HasPreviousPage,
                            HasNextPage = apiResponse.Data.HasNextPage
                        };
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectTaskDto>>(errorContent);
                    throw new Exception($"Error: {errorResponse?.Message ?? errorContent}");
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting root tasks: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Lấy task path (breadcrumb từ root đến current task) - Real API
        /// GET: api/projects/{projectId}/tasks/{taskId}/path
        /// </summary>
        public async Task<List<ProjectTaskDto>> GetTaskPathAsync(int projectId, int taskId)
        {
            try
            {
                var endpoint = $"{BaseEndpoint}/{projectId}/tasks/{taskId}/path";
                var response = await _apiService.GetFromEndpointAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<List<ProjectTaskDto>>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonConvert.DeserializeObject<ApiResponse<List<ProjectTaskDto>>>(errorContent);
                    throw new Exception($"Error: {errorResponse?.Message ?? errorContent}");
                }

                return new List<ProjectTaskDto>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting task path: {ex.Message}");
                return new List<ProjectTaskDto>();
            }
        }

        /// <summary>
        /// Bulk move tasks sang parent mới - Real API
        /// POST: api/projects/{projectId}/tasks/bulk-move
        /// </summary>
        public async Task<BulkMoveTasksResponse> BulkMoveTasksAsync(
            int projectId,
            List<int> taskIds,
            int? newParentTaskId,
            string reason = "")
        {
            try
            {
                var request = new
                {
                    TaskIds = taskIds,
                    NewParentTaskId = newParentTaskId,
                    Reason = reason
                };

                var endpoint = $"{BaseEndpoint}/{projectId}/tasks/bulk-move";
                var response = await _apiService.PostToEndpointAsync(endpoint, request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<BulkMoveTasksResponse>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonConvert.DeserializeObject<ApiResponse<BulkMoveTasksResponse>>(errorContent);
                    throw new Exception($"Error: {errorResponse?.Message ?? errorContent}");
                }

                // Return failed response if API call failed
                return new BulkMoveTasksResponse
                {
                    TotalTasks = taskIds?.Count ?? 0,
                    MovedTasks = 0,
                    FailedTasks = taskIds?.Count ?? 0,
                    Message = "Không thể di chuyển tasks. Vui lòng thử lại.",
                    Errors = new List<string> { "API call failed" }
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi bulk move tasks: {ex.Message}", ex);
            }
        }

        #endregion

        #region Project Workflow APIs

        /// <summary>
        /// Bắt đầu project - Real API
        /// </summary>
        public async Task<ProjectDto> StartProjectAsync(int projectId)
        {
            try
            {
                var endpoint = $"{BaseEndpoint}/{projectId}/start";
                var response = await _apiService.PostToEndpointAsync(endpoint, new { });

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectDto>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi bắt đầu project: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Hoàn thành project - Real API
        /// </summary>
        public async Task<ProjectDto> CompleteProjectAsync(int projectId)
        {
            try
            {
                var endpoint = $"{BaseEndpoint}/{projectId}/complete";
                var response = await _apiService.PostToEndpointAsync(endpoint, new { });

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectDto>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi hoàn thành project: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Tạm dừng project - Real API
        /// </summary>
        public async Task<ProjectDto> PauseProjectAsync(int projectId, string reason)
        {
            try
            {
                var request = new { Reason = reason };
                var endpoint = $"{BaseEndpoint}/{projectId}/pause";
                var response = await _apiService.PostToEndpointAsync(endpoint, request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectDto>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi tạm dừng project: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Tiếp tục project - Real API
        /// </summary>
        public async Task<ProjectDto> ResumeProjectAsync(int projectId)
        {
            try
            {
                var endpoint = $"{BaseEndpoint}/{projectId}/resume";
                var response = await _apiService.PostToEndpointAsync(endpoint, new { });

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectDto>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi tiếp tục project: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Cập nhật tiến độ project - Real API
        /// </summary>
        public async Task<ProjectDto> UpdateProgressAsync(int projectId, UpdateProjectProgressRequest request)
        {
            try
            {
                var endpoint = $"{BaseEndpoint}/{projectId}/progress";
                var response = await _apiService.PutToEndpointAsync(endpoint, request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectDto>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi cập nhật tiến độ: {ex.Message}", ex);
            }
        }

        //cap nhat trang thai project
        public async Task<ProjectDto> UpdateProjectStatusAsync(int projectId, UpdateProjectStatusRequest request)
        {
            try
            {
                var endpoint = $"{BaseEndpoint}/{projectId}/status";
                var response = await _apiService.PutToEndpointAsync(endpoint, request);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectDto>>(content);
                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi cập nhật trạng thái project: {ex.Message}", ex);
            }
        }

        #endregion

        #region Project Summary APIs

        /// <summary>
        /// Lấy summary/statistics của project - Real API
        /// </summary>
        public async Task<ProjectDashboardDto> GetProjectSummaryAsync(int projectId)
        {
            try
            {
                var endpoint = $"{BaseEndpoint}/{projectId}/summary";
                var response = await _apiService.GetFromEndpointAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<ProjectSummaryDto>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return MapToProjectDashboardDto(apiResponse.Data);
                    }
                }

                return CreateMockProjectDashboard(projectId);
            }
            catch (Exception)
            {
                return CreateMockProjectDashboard(projectId);
            }
        }

        #endregion

        #region Additional Mapping Methods (BỔ SUNG MỚI)

        /// <summary>
        /// Map ProjectSummaryDto to ProjectDashboardDto
        /// </summary>
        private ProjectDashboardDto MapToProjectDashboardDto(ProjectSummaryDto dto)
        {
            return new ProjectDashboardDto
            {
                ProjectId = dto.ProjectId,
                ProjectName = dto.ProjectName,
                Status = dto.Status,
                CompletionPercentage = dto.CompletionPercentage,
                TotalTasks = dto.TotalTasks,
                CompletedTasks = dto.CompletedTasks,
                //InProgressTasks = dto.InProgressTasks,
                //OverdueTasks = dto.OverdueTasks,
                TotalMembers = dto.TotalMembers,
                TotalFiles = dto.TotalFiles,
                TotalHours = dto.ActualHours,
                RecentTasks = new List<ProjectTaskDto>()
            };
        }

        #endregion


        #region Helper Methods

        private string BuildProjectFilterQuery(ProjectFilterRequest filter)
        {
            var queryParams = new List<string>();
            
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                queryParams.Add($"SearchTerm={Uri.EscapeDataString(filter.SearchTerm)}");
            
            if(filter.Status != ProjectStatus.All)
                queryParams.Add($"Status={filter.Status}");
            
            if (filter.ProjectManagerId.HasValue && filter.ProjectManagerId > 0)
                queryParams.Add($"ProjectManagerId={filter.ProjectManagerId}");
            
            if (filter.IsActive.HasValue)
                queryParams.Add($"IsActive={filter.IsActive.Value}");

            if (filter.ProjectParentId.HasValue && filter.ProjectParentId > 0)
                queryParams.Add($"ProjectParentId={filter.ProjectParentId.Value}");
            
            queryParams.Add($"PageNumber={filter.PageNumber}");
            queryParams.Add($"PageSize={filter.PageSize}");
            queryParams.Add($"SortBy={filter.SortBy}");
            queryParams.Add($"SortDirection={filter.SortDirection}");

            return queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
        }

        private string BuildTaskFilterQuery(TaskFilterRequest filter)
        {
            var queryParams = new List<string>();
            
            if (filter.ProjectId > 0)
                queryParams.Add($"ProjectId={filter.ProjectId}");
            if(filter.ReporterId.HasValue && filter.ReporterId > 0)
                queryParams.Add($"ReporterId={filter.ReporterId.Value}");
            if (filter.AssignedToId.HasValue && filter.AssignedToId > 0)
                queryParams.Add($"AssignedToId={filter.AssignedToId}");
            if (filter.Status.HasValue && filter.Status != TaskStatuss.All)
                queryParams.Add($"Status={filter.Status}");
            if (filter.Priority.HasValue && filter.Priority != TaskPriority.All)
                queryParams.Add($"Priority={filter.Priority}");
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                queryParams.Add($"SearchTerm={Uri.EscapeDataString(filter.SearchTerm)}");
            if (filter.IsActive.HasValue)
                queryParams.Add($"IsActive={filter.IsActive.Value}");
            if(filter.ParentTaskId.HasValue && filter.ParentTaskId.Value > 0)
                queryParams.Add($"ParentTaskId={filter.ParentTaskId.Value}");

            queryParams.Add($"PageNumber={filter.PageNumber}");
            queryParams.Add($"PageSize={filter.PageSize}");
            queryParams.Add($"SortBy={filter.SortBy}");
            queryParams.Add($"SortDirection={filter.SortDirection}");

            return queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
        }

        #endregion

        #region Mock Data Methods (Fallback)

        /// <summary>
        /// Tạo mock project dashboard khi API không available
        /// </summary>
        public ProjectDashboardDto CreateMockProjectDashboard(int projectId)
        {
            return new ProjectDashboardDto
            {
                ProjectId = projectId,
                ProjectName = "Sample Project",
                Status = ProjectStatus.Planning,
                CompletionPercentage = 65.5m,
                TotalTasks = 24,
                CompletedTasks = 16,
                InProgressTasks = 6,
                OverdueTasks = 2,
                TotalMembers = 8,
                TotalFiles = 156,
                TotalHours = 384.5m,
                RecentTasks = new List<ProjectTaskDto>
                {
                    CreateMockTask(004),
                    CreateMockTask(005),
                    CreateMockTask(006)
                }
            };
        }

        private ProjectTaskDto CreateMockTask(int id)
        {
            return new ProjectTaskDto
            {
                Id = id,
                ProjectId = -1,
                TaskCode = $"TSK-{id.ToString().ToUpper()}",
                Title = $"Task {id}",
                Description = "Sample task description",
                Status = TaskStatuss.Todo,
                Priority = TaskPriority.Normal,
                AssignedToId = -1,
                AssignedToName = "John Developer",
                StartDate = DateTime.Now.AddDays(-5),
                DueDate = DateTime.Now.AddDays(10),
                EstimatedHours = 16,
                ActualHours = 8,
                Progress = 50m,
                CreatedAt = DateTime.Now.AddDays(-7),
                IsOverdue = false
            };
        }


        #endregion

        #region Helper Methor

        /// <summary>
        /// Validate CreateProjectRequest using Data Annotations
        /// </summary>
        private ValidationResultModel ValidateRequest(CreateProjectRequest request)
        {
            var context = new ValidationContext(request);
            var results = new List<ValidationResult>();

            bool isValid = Validator.TryValidateObject(request, context, results, validateAllProperties: true);

            return new ValidationResultModel
            {
                IsValid = isValid,
                Errors = results.Select(r => r.ErrorMessage).ToList()
            };
        }


        /// <summary>
        /// Business rules validation
        /// </summary>
        private async Task ValidateBusinessRules(CreateProjectRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), "Project data không được null");
            }

            if (request.StartDate.HasValue && request.PlannedEndDate.HasValue)
            {
                if (request.StartDate.Value >= request.PlannedEndDate.Value)
                {
                    throw new BusinessRuleException("Ngày bắt đầu phải nhỏ hơn ngày kết thúc");
                }
            }

            // Có thể kiểm tra với database nếu cần
            // var projectCodeExists = await _projectRepository.ProjectCodeExistsAsync(request.ProjectCode);
            // if (projectCodeExists)
            // {
            //     throw new BusinessRuleException($"Mã project '{request.ProjectCode}' đã tồn tại");
            // }
        }

        #region Update Task Helper Methods

        /// <summary>
        /// Validate UpdateTaskRequest using Data Annotations
        /// </summary>
        private ValidationResultModel ValidateUpdateTaskRequest(UpdateTaskRequest request)
        {
            var context = new ValidationContext(request);
            var results = new List<ValidationResult>();

            bool isValid = Validator.TryValidateObject(request, context, results, validateAllProperties: true);

            return new ValidationResultModel
            {
                IsValid = isValid,
                Errors = results.Select(r => r.ErrorMessage).ToList()
            };
        }

        /// <summary>
        /// Business rules validation for UpdateTaskRequest
        /// </summary>
        private async Task ValidateUpdateTaskBusinessRules(UpdateTaskRequest request)
        {
            if (request == null) return;

            // Validate Progress consistency with Status
            if (request.Progress == 100 && request.Status != TaskStatuss.Completed)
            {
                throw new BusinessRuleException("Khi tiến độ là 100%, trạng thái phải là 'Completed'");
            }

            if (request.Status == TaskStatuss.Completed && request.Progress < 100)
            {
                throw new BusinessRuleException("Khi trạng thái là 'Completed', tiến độ phải là 100%");
            }

            // Validate CompletedAt logic
            if (request.Status == TaskStatuss.Completed && !request.CompletedAt.HasValue)
            {
                // Auto-set CompletedAt if not provided
                request.CompletedAt = DateTime.UtcNow;
            }

            if (request.Status != TaskStatuss.Completed && request.CompletedAt.HasValue)
            {
                throw new BusinessRuleException("Chỉ có thể set thời gian hoàn thành khi task đã completed");
            }

            // Validate Block logic
            if (request.IsBlocked && string.IsNullOrWhiteSpace(request.BlockReason))
            {
                throw new BusinessRuleException("Phải cung cấp lý do khi đánh dấu task bị block");
            }

            // Validate date logic
            if (request.StartDate.HasValue && request.DueDate.HasValue && 
                request.StartDate.Value > request.DueDate.Value)
            {
                throw new BusinessRuleException("Ngày bắt đầu không thể sau ngày kết thúc");
            }

            // Validate assigned user exists (if needed)
            if (request.AssignedToId > 0)
            {
                // Could add user existence check here if needed
                // var userExists = await _userService.UserExistsAsync(request.AssignedToId);
                // if (!userExists) throw new BusinessRuleException("User được assign không tồn tại");
            }
        }

        #endregion
        // =============================================================================
        // SUPPORTING CLASSES
        // =============================================================================

        /// <summary>
        /// Validation result class
        /// </summary>
        public class ValidationResultModel
        {
            public bool IsValid { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
        }


        /// <summary>
        /// Business rule exception
        /// </summary>
        public class BusinessRuleException : Exception
        {
            public BusinessRuleException(string message) : base(message) { }
        }

        /// <summary>
        /// Validation exception
        /// </summary>
        public class ValidationException : Exception
        {
            public ValidationException(string message) : base(message) { }
        }

        #endregion
    }
}