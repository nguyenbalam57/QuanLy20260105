using ManagementFile.Contracts.DTOs.Common;
using ManagementFile.Contracts.DTOs.UserManagement;
using ManagementFile.Contracts.Requests.UserManagement;
using ManagementFile.Contracts.Responses.UserManagement;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ManagementFile.AdminManagement.Services
{
    /// <summary>
    /// Service để tương tác với ManagementFile API
    /// </summary>
    public class ApiService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private string _sessionToken;

        public ApiService()
        {
            _httpClient = new HttpClient();
            _baseUrl = "http://192.168.249.54:5190"; // API URL
            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AdminManagement/1.0");
        }

        /// <summary>
        /// Current session token
        /// </summary>
        public string SessionToken
        {
            get => _sessionToken;
            set
            {
                _sessionToken = value;
                UpdateAuthorizationHeader();
            }
        }

        /// <summary>
        /// Current user information
        /// </summary>
        public UserDto CurrentUser { get; private set; }

        /// <summary>
        /// Check if user is logged in
        /// </summary>
        public bool IsLoggedIn => !string.IsNullOrEmpty(_sessionToken) && CurrentUser != null;

        #region Authentication

        /// <summary>
        /// Đăng nhập vào hệ thống
        /// </summary>
        public async Task<LoginResponse> LoginAsync(string usernameOrEmail, string password, bool rememberMe = false)
        {
            try
            {
                var request = new LoginRequest
                {
                    UsernameOrEmail = usernameOrEmail,
                    Password = password,
                    RememberMe = rememberMe
                };

                var response = await PostAsync<ApiResponse<LoginResponse>>("api/users/login", request);
                
                if (response?.Success == true && response.Data?.Success == true)
                {
                    SessionToken = response.Data.SessionToken;
                    CurrentUser = response.Data.User;
                    return response.Data;
                }

                return new LoginResponse 
                { 
                    Success = false, 
                    Message = response?.Message ?? "Đăng nhập thất bại" 
                };
            }
            catch (Exception ex)
            {
                return new LoginResponse 
                { 
                    Success = false, 
                    Message = $"Lỗi kết nối: {ex.Message}" 
                };
            }
        }

        /// <summary>
        /// Đăng xuất khỏi hệ thống
        /// </summary>
        public async Task<bool> LogoutAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(_sessionToken))
                {
                    var request = new LogoutRequest { SessionToken = _sessionToken };
                    await PostAsync<ApiResponse<string>>("api/users/logout", request);
                }
                
                SessionToken = null;
                CurrentUser = null;
                return true;
            }
            catch
            {
                // Still clear local session even if API call fails
                SessionToken = null;
                CurrentUser = null;
                return false;
            }
        }

        /// <summary>
        /// Lấy thông tin user hiện tại
        /// </summary>
        public async Task<UserDto> GetCurrentUserAsync()
        {
            try
            {
                var response = await GetAsync<ApiResponse<UserDto>>("api/users/me");
                if (response?.Success == true)
                {
                    CurrentUser = response.Data;
                    return response.Data;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region User Management

        /// <summary>
        /// Lấy danh sách users với tìm kiếm và phân trang
        /// </summary>
        public async Task<UserListResponse> GetUsersAsync(UserSearchRequest searchRequest)
        {
            try
            {
                var queryParams = BuildQueryString(searchRequest);
                var response = await GetAsync<ApiResponse<UserListResponse>>($"api/users?{queryParams}");
                
                return response?.Success == true ? response.Data : new UserListResponse();
            }
            catch
            {
                return new UserListResponse();
            }
        }

        /// <summary>
        /// Lấy thông tin user theo ID
        /// </summary>
        public async Task<UserDto> GetUserAsync(int userId)
        {
            try
            {
                var response = await GetAsync<ApiResponse<UserDto>>($"api/users/{userId}");
                return response?.Success == true ? response.Data : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Tạo user mới
        /// </summary>
        public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
        {
            try
            {
                var response = await PostAsync<ApiResponse<UserDto>>("api/users", request);
                return response?.Success == true ? response.Data : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Cập nhật thông tin user
        /// </summary>
        public async Task<UserDto> UpdateUserAsync(int userId, UpdateUserRequest request)
        {
            try
            {
                var response = await PutAsync<ApiResponse<UserDto>>($"api/users/{userId}", request);
                return response?.Success == true ? response.Data : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Xóa user
        /// </summary>
        public async Task<bool> DeleteUserAsync(int userId)
        {
            try
            {
                var response = await DeleteAsync<ApiResponse<string>>($"api/users/{userId}");
                return response?.Success == true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Đổi mật khẩu user
        /// </summary>
        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request)
        {
            try
            {
                var response = await PostAsync<ApiResponse<string>>($"api/users/{userId}/change-password", request);
                return response?.Success == true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Mở khóa tài khoản user
        /// </summary>
        public async Task<bool> UnlockUserAsync(int userId)
        {
            try
            {
                var response = await PostAsync<ApiResponse<string>>($"api/users/{userId}/unlock", null);
                return response?.Success == true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// reset mật khẩu user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<bool> ResetUserPasswordAsync(int userId, ResetPasswordRequest request)
        {
            try
            {
                var response = await PostAsync<ApiResponse<string>>($"api/users/{userId}/reset-password", request);
                return response?.Success == true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Helper Methods

        private void UpdateAuthorizationHeader()
        {
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            if (!string.IsNullOrEmpty(_sessionToken))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_sessionToken}");
            }
        }

        private string BuildQueryString(object obj)
        {
            var properties = obj.GetType().GetProperties();
            var queryParams = new System.Collections.Generic.List<string>();

            foreach (var prop in properties)
            {
                var value = prop.GetValue(obj);
                if (value != null)
                {
                    var valueStr = value.ToString();
                    if (!string.IsNullOrEmpty(valueStr))
                    {
                        queryParams.Add($"{prop.Name}={Uri.EscapeDataString(valueStr)}");
                    }
                }
            }

            return string.Join("&", queryParams);
        }

        private async Task<T> GetAsync<T>(string endpoint)
        {
            var response = await _httpClient.GetAsync(endpoint);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(content);
            }
            return default(T);
        }

        private async Task<T> PostAsync<T>(string endpoint, object data)
        {


            var json = data != null ? JsonConvert.SerializeObject(data) : string.Empty;
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(endpoint, content);

            // Kiểm tra lỗi 401
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // Token hết hạn, cần refresh hoặc login lại
                return default(T);
            }

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(responseContent);
            }
            return default(T);
        }

        private async Task<T> PutAsync<T>(string endpoint, object data)
        {
            var json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync(endpoint, content);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(responseContent);
            }
            return default(T);
        }

        private async Task<T> DeleteAsync<T>(string endpoint)
        {
            var response = await _httpClient.DeleteAsync(endpoint);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(content);
            }
            return default(T);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        #endregion
    }
}