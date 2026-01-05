using ManagementFile.App.Models;
using ManagementFile.App.Models.Users;
using ManagementFile.App.ViewModels;
using ManagementFile.Contracts.DTOs.Common;
using ManagementFile.Contracts.DTOs.ProjectManagement;
using ManagementFile.Contracts.DTOs.UserManagement;
using ManagementFile.Contracts.Enums;
using ManagementFile.Contracts.Enums.Extensions;
using ManagementFile.Contracts.Requests.UserManagement;
using ManagementFile.Contracts.Responses.UserManagement;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace ManagementFile.App.Services
{
    /// <summary>
    /// Service để quản lý trạng thái user hiện tại và session
    /// </summary>
    public class UserManagementService
    {
        private static readonly object _lock = new object();

        private UserDto _currentUser;
        private UserModel _currentUserModel;
        private string _sessionToken;
        private DateTime _sessionExpiresAt;
        private readonly ApiService _apiService;
        private readonly string _apiBaseUrl = "/api/users";

        public UserManagementService(
            ApiService apiService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        }

        #region Properties

        /// <summary>
        /// User hiện tại
        /// </summary>
        public UserDto CurrentUser
        {
            get => _currentUser;
            private set
            {
                _currentUser = value;
                OnCurrentUserChanged?.Invoke(value);
            }
        }

        public UserModel CurrentUserModel
        {
            get => _currentUserModel;
            private set => _currentUserModel = value;
        }

        /// <summary>
        /// Session token hiện tại
        /// </summary>
        public string SessionToken
        {
            get => _sessionToken;
            private set
            {
                _sessionToken = value;
                _apiService.SetSessionToken(value);
            }
        }

        /// <summary>
        /// Thời gian hết hạn session
        /// </summary>
        public DateTime SessionExpiresAt
        {
            get => _sessionExpiresAt;
            private set => _sessionExpiresAt = value;
        }

        /// <summary>
        /// Kiểm tra user đã đăng nhập chưa
        /// </summary>
        public bool IsLoggedIn => CurrentUser != null && !string.IsNullOrEmpty(SessionToken) && SessionExpiresAt > DateTime.UtcNow;

        /// <summary>
        /// Kiểm tra session sắp hết hạn (trong 5 phút)
        /// </summary>
        public bool IsSessionExpiringSoon => SessionExpiresAt <= DateTime.UtcNow.AddMinutes(5);

        #endregion

        #region Events

        /// <summary>
        /// Event khi user hiện tại thay đổi
        /// </summary>
        public event Action<UserDto> OnCurrentUserChanged;

        /// <summary>
        /// Event khi user logout
        /// </summary>
        public event Action OnUserLoggedOut;

        /// <summary>
        /// Event khi session hết hạn
        /// </summary>
        public event Action OnSessionExpired;

        #endregion

        #region Methods

        /// <summary>
        /// Đăng nhập user
        /// </summary>
        public async Task<UserDto> LoginAsync(string usernameOrEmail, string password, bool rememberMe = false)
        {
            try
            {
                var result = await _apiService.LoginAsync(usernameOrEmail, password, rememberMe);
                
                if (result?.Success == true && result.User != null)
                {
                    SetCurrentUser(result.User, result.SessionToken, result.ExpiresAt);
                    return result.User;
                }
                
                return null;
            }
            catch (Exception)
            {
                throw;
            }
        }

        // Thay doi mat khau
        public async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword, string commitPassword)
        {
            try
            {
                var request = new ChangePasswordRequest
                {
                    CurrentPassword = currentPassword,
                    NewPassword = newPassword,
                    ConfirmPassword = commitPassword
                };

                var endpoint = $"{_apiBaseUrl}/{CurrentUser.Id}/change-password";
                var result = await _apiService.PostToEndpointAsync(endpoint, request);

                if(result.IsSuccessStatusCode)
                {
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Đăng nhập user (set thông tin từ bên ngoài)
        /// </summary>
        public void SetCurrentUser(UserDto user, string sessionToken, DateTime expiresAt)
        {
            CurrentUser = user;
            SessionToken = sessionToken;
            SessionExpiresAt = expiresAt;

            SerCurrentUserModel(CurrentUser);
        }

        public void SerCurrentUserModel(UserDto userDto)
        {
            CurrentUserModel = UserModel.FromDto(userDto);
        }

        /// <summary>
        /// Đăng xuất user (đồng bộ)
        /// </summary>
        public void Logout()
        {
            try
            {
                if (!string.IsNullOrEmpty(SessionToken))
                {
                    Task.Run(async () => await _apiService.LogoutAsync());
                }
            }
            catch
            {
                // Ignore logout errors
            }
            finally
            {
                ClearSession();
                OnUserLoggedOut?.Invoke();
            }
        }

        /// <summary>
        /// Đăng xuất user (bất đồng bộ)
        /// </summary>
        public async Task LogoutAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(SessionToken))
                {
                    await _apiService.LogoutAsync();
                }
            }
            catch
            {
                // Ignore logout errors
            }
            finally
            {
                ClearSession();
                OnUserLoggedOut?.Invoke();
            }
        }

        /// <summary>
        /// Làm mới thông tin user hiện tại
        /// </summary>
        public async Task<bool> RefreshCurrentUserAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(SessionToken))
                    return false;

                var user = await _apiService.GetCurrentUserAsync();
                CurrentUser = user;
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                // Session expired
                ClearSession();
                OnSessionExpired?.Invoke();
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Kiểm tra quyền của user
        /// </summary>
        public bool HasPermission(UserRole requiredRole)
        {
            if (!IsLoggedIn)
                return false;

            return (int)CurrentUser.Role >= (int)requiredRole;
        }

        /// <summary>
        /// Kiểm tra user có phải là admin không
        /// </summary>
        public bool IsAdmin => IsLoggedIn && CurrentUser.Role == UserRole.Admin;

        /// <summary>
        /// Kiểm tra user có phải là project manager không
        /// </summary>
        public bool IsProjectManager => IsLoggedIn && (CurrentUser.Role == UserRole.Admin || CurrentUser.Role == UserRole.Manager);

        /// <summary>
        /// Lấy display name của user
        /// </summary>
        public string GetDisplayName()
        {
            return IsLoggedIn ? CurrentUser.DisplayName : "Guest";
        }

        /// <summary>
        /// Lấy thông tin role dạng text
        /// </summary>
        public string GetRoleDisplayName()
        {
            if (!IsLoggedIn)
                return "Guest";

            return EnumExtensions.GetDescription(CurrentUser.Role);

        }

        /// <summary>
        /// Lấy thông tin department dạng text
        /// </summary>
        public string GetDepartmentDisplayName()
        {
            if (!IsLoggedIn)
                return "";

            return EnumExtensions.GetDescription(CurrentUser.Department);

        }

        /// <summary>
        /// Xóa session hiện tại
        /// </summary>
        private void ClearSession()
        {
            CurrentUser = null;
            SessionToken = "";
            SessionExpiresAt = DateTime.MinValue;
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            _apiService?.Dispose();
        }

        /// <summary>
        /// Get current logged-in user (mock implementation for now)
        /// </summary>
        public UserDto GetCurrentUser()
        {
            // TODO: Implement proper current user retrieval from session/token
            return CurrentUser;
        }

        public UserModel GetCurrentUserModel()
        {
            return CurrentUserModel;
        }

        #endregion

        #region User Search Methods (BỔ SUNG MỚI)

        /// <summary>
        /// Tìm kiếm users với điều kiện filter
        /// </summary>
        public async Task<List<UserModel>> SearchUsersAsync(string searchText, UserSearchOptions options = null)
        {
            try
            {
                // Initialize options if null
                if (options == null)
                {
                    options = new UserSearchOptions();
                }

                var queryParams = new List<string>();

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    queryParams.Add($"searchTerm={Uri.EscapeDataString(searchText)}");
                }
                if (options.ActiveUsersOnly)
                {
                    queryParams.Add("active=true");
                }
                if (options.Department != null)
                {
                    queryParams.Add($"department={options.Department}");
                }
                if (options.Role != null)
                {
                    queryParams.Add($"role={options.Role}");
                }
                if (options.MaxResults > 0)
                {
                    queryParams.Add($"maxResults={options.MaxResults}");
                }
                queryParams.Add($"pageSize={options.MaxResults}");
                if(options.PageNumber > 0)
                {
                    queryParams.Add($"pageNumber={options.PageNumber}");
                }
                else
                    queryParams.Add("pageNumber=1"); // Always start from first page for search

                
                // Log the search request
                System.Diagnostics.Debug.WriteLine($"Searching users with text: '{searchText}', ActiveOnly: {options.ActiveUsersOnly}");

                // Send the search request as JSON in the request body
                var endpoint = $"{_apiBaseUrl}?{string.Join("&",queryParams)}";
                var response = await _apiService.GetFromEndpointAsync(endpoint);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<UserListResponse>>(content);
                    
                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        var users = apiResponse.Data.Users.Select(dto => new UserModel
                        {
                            Id = dto.Id,
                            UserName = dto.Username, // Fixed: UserName -> Username
                            FullName = dto.FullName,
                            Email = dto.Email,
                            Role = dto.Role,
                            Department = dto.Department,
                            IsActive = dto.IsActive,
                            PhoneNumber = dto.PhoneNumber,
                            LastLogin = dto.LastLoginAt, // Fixed: LastLogin -> LastLoginAt
                            CreatedAt = dto.CreatedAt
                        }).ToList();

                        System.Diagnostics.Debug.WriteLine($"API returned {users.Count} users for search '{searchText}'");

                        // Apply additional filtering based on FilterMode
                        return await ApplyFilterModeAsync(users, options);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"API search failed or returned no users: {apiResponse?.Message ?? "Unknown error"}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"API search request failed with status code: {response.StatusCode}");
                }

                // Fallback to mock data if API fails
                System.Diagnostics.Debug.WriteLine("Falling back to mock user data");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error searching users: {ex.Message}");
                // Return mock data on error
                return null;
            }
        }

        /// <summary>
        /// Lấy danh sách users với pagination (overload mới)
        /// </summary>
        public async Task<List<UserModel>> GetUsersAsync(int pageNumber = 1, int pageSize = 50, string searchTerm = "")
        {
            var options = new UserSearchOptions
            {
                MaxResults = pageSize,
                ActiveUsersOnly = true
            };

            return await SearchUsersAsync(searchTerm, options);
        }

        /// <summary>
        /// Lấy user theo ID
        /// </summary>
        public async Task<UserModel> GetUserByIdAsync(int userId)
        {
            try
            {
                if (userId == null || userId < 0)
                    return null;

                var endpoint = $"{_apiBaseUrl}/{userId}";
                var response = await _apiService.GetFromEndpointAsync(endpoint);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<UserDto>>(content);
                    
                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        var dto = apiResponse.Data;
                        return new UserModel
                        {
                            Id = dto.Id,
                            UserName = dto.Username, // Fixed: UserName -> Username
                            FullName = dto.FullName,
                            Email = dto.Email,
                            Role = dto.Role,
                            Department = dto.Department,
                            IsActive = dto.IsActive,
                            PhoneNumber = dto.PhoneNumber,
                            LastLogin = dto.LastLoginAt, // Fixed: LastLogin -> LastLoginAt
                            CreatedAt = dto.CreatedAt
                        };
                    }
                }

                // Fallback to mock data
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting user by ID: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Lấy danh sách users theo usernames
        /// </summary>
        public async Task<List<UserModel>> GetUsersByUsernamesAsync(IEnumerable<string> usernames)

        {
            try
            {
                if (usernames == null || !usernames.Any())
                {
                    return new List<UserModel>();
                }

                var usernameList = usernames.Where(u => !string.IsNullOrWhiteSpace(u)).ToList();
                if (!usernameList.Any())
                {
                    return new List<UserModel>();
                }

                System.Diagnostics.Debug.WriteLine($"Getting users by usernames: {string.Join(", ", usernameList)}");

                // Build query parameters
                var queryParams = new List<string>();
                foreach (var username in usernameList)
                {
                    queryParams.Add($"usernames={Uri.EscapeDataString(username)}");
                }

                var endpoint = $"{_apiBaseUrl}/by-usernames?{string.Join("&", queryParams)}";
                var response = await _apiService.GetFromEndpointAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<List<UserDto>>>(content);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        var users = apiResponse.Data.Select(dto => new UserModel
                        {
                            Id = dto.Id,
                            UserName = dto.Username,
                            FullName = dto.FullName,
                            Email = dto.Email,
                            Role = dto.Role,
                            Department = dto.Department,
                            IsActive = dto.IsActive,
                            PhoneNumber = dto.PhoneNumber,
                            LastLogin = dto.LastLoginAt,
                            CreatedAt = dto.CreatedAt
                        }).ToList();

                        System.Diagnostics.Debug.WriteLine($"API returned {users.Count} users for usernames");
                        return users;
                    }
                }

                // Fallback to mock data if API fails
                System.Diagnostics.Debug.WriteLine("API failed, falling back to mock data");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting users by usernames: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Lấy user theo username
        /// </summary>
        public async Task<UserModel> GetUserByUsernameAsync(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                    return null;

                var users = await GetUsersByUsernamesAsync(new[] { username });
                return users.FirstOrDefault();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting user by username: {ex.Message}");
                return null;
            }
        }


        /// <summary>
        /// Áp dụng filter mode cho danh sách users
        /// </summary>
        private async Task<List<UserModel>> ApplyFilterModeAsync(List<UserModel> users, UserSearchOptions options)
        {
            switch (options.FilterMode)
            {
                case UserFilterMode.AllUsers:
                    return users;

                case UserFilterMode.AvailableUsersForProject:
                    if (options.ProjectId.HasValue)
                    {
                        // Lấy danh sách members hiện tại của project
                        var existingMembers = await App.GetRequiredService<ProjectApiService>().GetProjectMembersAsync(options.ProjectId.Value);
                        var existingUserIds = existingMembers.Select(m => m.UserId).ToHashSet();
                        
                        // Lọc ra những user chưa thuộc project
                        return users.Where(u => !existingUserIds.Contains(u.Id)).ToList();
                    }
                    return users;

                case UserFilterMode.ProjectMembersOnly:
                    if (options.ProjectId.HasValue)
                    {
                        // Chỉ lấy users đã thuộc project
                        var existingMembers = await App.GetRequiredService<ProjectApiService>().GetProjectMembersAsync(options.ProjectId.Value);
                        var existingUserIds = existingMembers.Select(m => m.UserId).ToHashSet();
                        
                        return users.Where(u => existingUserIds.Contains(u.Id)).ToList();
                    }
                    return users;

                case UserFilterMode.ByDepartment:
                    if (options.Department != null)
                    {
                        var deparString = options.Department.ToString();
                        return users.Where(u => string.Equals(u.Department.ToString(), deparString, StringComparison.OrdinalIgnoreCase)).ToList();
                    }
                    return users;

                case UserFilterMode.ByRole:
                    if (options.Role != null)
                    {
                        var roleString = options.Role.ToString();
                        var filteredUsers = users.Where(u => 
                            string.Equals(u.Role.ToString(), roleString, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                        System.Diagnostics.Debug.WriteLine($"Filtered to {filteredUsers.Count} users with role '{roleString}'");
                        return filteredUsers;
                    }
                    return users;

                default:
                    return users;
            }
        }

        #endregion

        #region Methods

        #endregion
    }
}