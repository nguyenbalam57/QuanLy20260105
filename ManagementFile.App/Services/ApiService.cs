using ManagementFile.App.Models;
using ManagementFile.Contracts.DTOs.Common;
using ManagementFile.Contracts.DTOs.UserManagement;
using ManagementFile.Contracts.Requests.UserManagement;
using ManagementFile.Contracts.Responses.UserManagement;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ManagementFile.App.Services
{
    /// <summary>
    /// Service để tương tác với ManagementFile API
    /// </summary>
    public class ApiService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ApiConfiguration _config;
        protected readonly string _baseUrl;
        private string _sessionToken;

        public ApiService(HttpClient httpClient, ApiConfiguration config)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            // ✅ Sử dụng config
            _baseUrl = _config.BaseUrl.TrimEnd('/');

            // ✅ Đảm bảo HttpClient sử dụng đúng BaseAddress
            var baseUri = _baseUrl.EndsWith("/") ? _baseUrl : _baseUrl + "/";
            _httpClient.BaseAddress = new Uri(baseUri);

            System.Diagnostics.Debug.WriteLine("=== ApiService Constructor Debug ===");
            System.Diagnostics.Debug.WriteLine($"Config BaseUrl: {_config.BaseUrl}");
            System.Diagnostics.Debug.WriteLine($"Final _baseUrl: {_baseUrl}");
            System.Diagnostics.Debug.WriteLine($"HttpClient.BaseAddress: {_httpClient.BaseAddress}");
            System.Diagnostics.Debug.WriteLine("=== End Constructor Debug ===");

        }

        #region Public API Methods for backwards compatibility

        /// <summary>
        /// Generic GET method for endpoints
        /// </summary>
        public async Task<HttpResponseMessage> GetFromEndpointAsync(string endpoint)
        {
            return await GetAsync(endpoint);
        }

        /// <summary>
        /// Generic POST method for endpoints
        /// </summary>
        public async Task<HttpResponseMessage> PostToEndpointAsync(string endpoint, object data)
        {
            return await PostAsync(endpoint, data);
        }

        /// <summary>
        /// Generic PUT method for endpoints
        /// </summary>
        public async Task<HttpResponseMessage> PutToEndpointAsync(string endpoint, object data)
        {
            return await PutAsync(endpoint, data);
        }

        /// <summary>
        /// Generic PATCH method for endpoints
        /// </summary>
        public async Task<HttpResponseMessage> PatchToEndpointAsync(string endpoint, object data)
        {
            return await PatchAsync(endpoint, data);
        }

        /// <summary>
        /// Generic DELETE method for endpoints
        /// </summary>
        public async Task<HttpResponseMessage> DeleteFromEndpointAsync(string endpoint)
        {
            return await DeleteAsync(endpoint);
        }

        /// <summary>
        /// Generic DELETE method for endpoints with request body
        /// </summary>
        public async Task<HttpResponseMessage> DeleteFromEndpointAsync(string endpoint, object data)
        {
            return await DeleteAsync(endpoint, data);
        }

        #endregion

        #region Protected Helper Methods for Inheritance

        /// <summary>
        /// Protected method để thực hiện GET request - có thể được kế thừa
        /// </summary>
        protected async Task<HttpResponseMessage> GetAsync(string endpoint)
        {
            const int maxRetries = 3;
            int retryCount = 0;

            while (retryCount <= maxRetries)
            {
                try
                {
                    var url = BuildUrl(endpoint);

                    System.Diagnostics.Debug.WriteLine($"📤 GET Request: {url}");

                    using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                    {
                        AddDefaultHeaders(request);

                        var response = await _httpClient.SendAsync(request);

                        System.Diagnostics.Debug.WriteLine($"📥 GET Response: {response.StatusCode}");

                        return response;
                    }
                }
                catch (Exception ex) when (retryCount < maxRetries && IsRetryableException(ex))
                {
                    retryCount++;
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                    System.Diagnostics.Debug.WriteLine($"🔄 GET Retry {retryCount}/{maxRetries} after {delay.TotalSeconds}s - Error: {ex.Message}");
                    await Task.Delay(delay);
                    continue;
                }
                catch (HttpRequestException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ GET HttpRequestException: {ex.Message}");

                    if (ex.InnerException is IOException)
                    {
                        throw new Exception("Kết nối đến server bị gián đoạn khi thực hiện GET request. Vui lòng kiểm tra kết nối mạng.");
                    }

                    throw new Exception($"Không thể thực hiện GET request đến server: {ex.Message}");
                }
                catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                {
                    throw new Exception("GET request timeout - Server không phản hồi trong thời gian cho phép.");
                }
                catch (TaskCanceledException)
                {
                    throw new Exception("GET request bị hủy - Vui lòng thử lại.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ GET Unexpected error: {ex}");
                    throw new Exception($"Lỗi không xác định khi thực hiện GET request: {ex.Message}");
                }
            }

            throw new Exception($"Không thể thực hiện GET request sau {maxRetries + 1} lần thử");
        }

        /// <summary>
        /// Protected method để thực hiện POST request - có thể được kế thừa
        /// </summary>
        protected async Task<HttpResponseMessage> PostAsync(string endpoint, object data)
        {
            const int maxRetries = 3;
            int retryCount = 0;

            while (retryCount <= maxRetries)
            {
                HttpContent content = null;
                HttpRequestMessage request = null;
                try
                {
                    var url = BuildUrl(endpoint);
                    content = await CreateHttpContent(data);

                    if (content == null)
                    {
                        throw new InvalidOperationException("HttpContent không được tạo thành công");
                    }

                    System.Diagnostics.Debug.WriteLine($"📤 POST Request: {url}");
                    LogRequestContent(content);

                    request = new HttpRequestMessage(HttpMethod.Post, url)
                    {
                        Content = content
                    };

                    AddDefaultHeaders(request);

                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                    {
                        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);

                        System.Diagnostics.Debug.WriteLine($"📥 POST Response: {response.StatusCode}");

                        // Log chi tiết cho HTTP 400 Bad Request
                        if (response.StatusCode == HttpStatusCode.BadRequest)
                        {
                            await LogBadRequestDetails(response);
                        }

                        // Log chi tiết cho tất cả non-success responses
                        if (!response.IsSuccessStatusCode)
                        {
                            await LogErrorResponseDetails(response);
                        }

                        return response;
                    }
                }
                catch (JsonException jsonEx)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ POST JSON Error: {jsonEx.Message}");
                    throw new Exception($"Lỗi định dạng dữ liệu JSON: {jsonEx.Message}", jsonEx);
                }
                catch (ObjectDisposedException disposedEx)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ POST ObjectDisposed Error: {disposedEx.Message}");

                    if (retryCount < maxRetries)
                    {
                        retryCount++;
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                        System.Diagnostics.Debug.WriteLine($"🔄 POST Retry {retryCount}/{maxRetries} after {delay.TotalSeconds}s - ObjectDisposed");
                        await Task.Delay(delay);
                        continue;
                    }
                    throw new Exception("HttpContent bị disposed không mong muốn", disposedEx);
                }
                catch (Exception ex) when (retryCount < maxRetries && IsRetryableException(ex))
                {
                    retryCount++;
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                    System.Diagnostics.Debug.WriteLine($"🔄 POST Retry {retryCount}/{maxRetries} after {delay.TotalSeconds}s - Error: {ex.Message}");

                    // ✅ Dispose content cũ trước khi retry
                    content?.Dispose();
                    request?.Dispose();

                    await Task.Delay(delay);
                    continue;
                }
                catch (HttpRequestException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ POST HttpRequestException: {ex.Message}");

                    if (ex.InnerException is IOException)
                    {
                        throw new Exception("Kết nối đến server bị gián đoạn khi thực hiện POST request. Vui lòng kiểm tra kết nối mạng.");
                    }

                    throw new Exception($"Không thể thực hiện POST request đến server: {ex.Message}");
                }
                catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                {
                    throw new Exception("POST request timeout - Server không phản hồi trong thời gian cho phép.");
                }
                catch (TaskCanceledException)
                {
                    throw new Exception("POST request bị hủy - Vui lòng thử lại.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ POST Unexpected error: {ex}");
                    throw new Exception($"Lỗi không xác định khi thực hiện POST request: {ex.Message}");
                }
                finally
                {
                    content?.Dispose();
                    request?.Dispose();
                }
            }

            throw new Exception($"Không thể thực hiện POST request sau {maxRetries + 1} lần thử");
        }

        /// <summary>
        /// Protected method để thực hiện PUT request - có thể được kế thừa
        /// </summary>
        protected async Task<HttpResponseMessage> PutAsync(string endpoint, object data)
        {
            const int maxRetries = 3;
            int retryCount = 0;

            while (retryCount <= maxRetries)
            {
                HttpContent content = null;
                HttpRequestMessage request = null;
                try
                {
                    var url = BuildUrl(endpoint);
                    content = await CreateHttpContent(data);

                    System.Diagnostics.Debug.WriteLine($"📤 PUT Request: {url}");
                    LogRequestContent(content);

                    request = new HttpRequestMessage(HttpMethod.Put, url)
                    {
                        Content = content
                    };

                    AddDefaultHeaders(request);

                    var response = await _httpClient.SendAsync(request);

                    System.Diagnostics.Debug.WriteLine($"📥 PUT Response: {response.StatusCode}");

                    // Log error details for non-success responses
                    if (!response.IsSuccessStatusCode)
                    {
                        await LogErrorResponseDetails(response);
                    }

                    return response;
                }
                catch (Exception ex) when (retryCount < maxRetries && IsRetryableException(ex))
                {
                    retryCount++;
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                    System.Diagnostics.Debug.WriteLine($"🔄 PUT Retry {retryCount}/{maxRetries} after {delay.TotalSeconds}s - Error: {ex.Message}");

                    content?.Dispose();
                    request?.Dispose();

                    await Task.Delay(delay);
                    continue;
                }
                catch (HttpRequestException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ PUT HttpRequestException: {ex.Message}");

                    if (ex.InnerException is IOException)
                    {
                        throw new Exception("Kết nối đến server bị gián đoạn khi thực hiện PUT request. Vui lòng kiểm tra kết nối mạng.");
                    }

                    throw new Exception($"Không thể thực hiện PUT request đến server: {ex.Message}");
                }
                catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                {
                    throw new Exception("PUT request timeout - Server không phản hồi trong thời gian cho phép.");
                }
                catch (TaskCanceledException)
                {
                    throw new Exception("PUT request bị hủy - Vui lòng thử lại.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ PUT Unexpected error: {ex}");
                    throw new Exception($"Lỗi không xác định khi thực hiện PUT request: {ex.Message}");
                }
                finally
                {
                    content?.Dispose();
                    request?.Dispose();
                }
            }

            throw new Exception($"Không thể thực hiện PUT request sau {maxRetries + 1} lần thử");
        }

        /// <summary>
        /// Protected method để thực hiện PATCH request - có thể được kế thừa
        /// </summary>
        protected async Task<HttpResponseMessage> PatchAsync(string endpoint, object data)
        {
            const int maxRetries = 3;
            int retryCount = 0;

            while (retryCount <= maxRetries)
            {
                HttpContent content = null;
                HttpRequestMessage request = null;
                try
                {
                    var url = BuildUrl(endpoint);
                    content = await CreateHttpContent(data);

                    System.Diagnostics.Debug.WriteLine($"📤 PATCH Request: {url}");
                    LogRequestContent(content);

                    request = new HttpRequestMessage(new HttpMethod("PATCH"), url)
                    {
                        Content = content
                    };

                    AddDefaultHeaders(request);

                    var response = await _httpClient.SendAsync(request);

                    System.Diagnostics.Debug.WriteLine($"📥 PATCH Response: {response.StatusCode}");

                    // Log error details for non-success responses
                    if (!response.IsSuccessStatusCode)
                    {
                        await LogErrorResponseDetails(response);
                    }

                    return response;
                }
                catch (Exception ex) when (retryCount < maxRetries && IsRetryableException(ex))
                {
                    retryCount++;
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                    System.Diagnostics.Debug.WriteLine($"🔄 PATCH Retry {retryCount}/{maxRetries} after {delay.TotalSeconds}s - Error: {ex.Message}");

                    content?.Dispose();
                    request?.Dispose();

                    await Task.Delay(delay);
                    continue;
                }
                catch (HttpRequestException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ PATCH HttpRequestException: {ex.Message}");

                    if (ex.InnerException is IOException)
                    {
                        throw new Exception("Kết nối đến server bị gián đoạn khi thực hiện PATCH request. Vui lòng kiểm tra kết nối mạng.");
                    }

                    throw new Exception($"Không thể thực hiện PATCH request đến server: {ex.Message}");
                }
                catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                {
                    throw new Exception("PATCH request timeout - Server không phản hồi trong thời gian cho phép.");
                }
                catch (TaskCanceledException)
                {
                    throw new Exception("PATCH request bị hủy - Vui lòng thử lại.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ PATCH Unexpected error: {ex}");
                    throw new Exception($"Lỗi không xác định khi thực hiện PATCH request: {ex.Message}");
                }
                finally
                {
                    content?.Dispose();
                    request?.Dispose();
                }
            }

            throw new Exception($"Không thể thực hiện PATCH request sau {maxRetries + 1} lần thử");
        }

        /// <summary>
        /// Protected method để thực hiện DELETE request - có thể được kế thừa
        /// </summary>
        protected async Task<HttpResponseMessage> DeleteAsync(string endpoint)
        {
            const int maxRetries = 3;
            int retryCount = 0;

            while (retryCount <= maxRetries)
            {
                try
                {
                    var url = BuildUrl(endpoint);

                    System.Diagnostics.Debug.WriteLine($"📤 DELETE Request: {url}");

                    using (var request = new HttpRequestMessage(HttpMethod.Delete, url))
                    {
                        AddDefaultHeaders(request);

                        var response = await _httpClient.SendAsync(request);

                        System.Diagnostics.Debug.WriteLine($"📥 DELETE Response: {response.StatusCode}");

                        // Log error details for non-success responses
                        if (!response.IsSuccessStatusCode)
                        {
                            await LogErrorResponseDetails(response);
                        }

                        return response;
                    }
                }
                catch (Exception ex) when (retryCount < maxRetries && IsRetryableException(ex))
                {
                    retryCount++;
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                    System.Diagnostics.Debug.WriteLine($"🔄 DELETE Retry {retryCount}/{maxRetries} after {delay.TotalSeconds}s - Error: {ex.Message}");
                    await Task.Delay(delay);
                    continue;
                }
                catch (HttpRequestException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ DELETE HttpRequestException: {ex.Message}");

                    if (ex.InnerException is IOException)
                    {
                        throw new Exception("Kết nối đến server bị gián đoạn khi thực hiện DELETE request. Vui lòng kiểm tra kết nối mạng.");
                    }

                    throw new Exception($"Không thể thực hiện DELETE request đến server: {ex.Message}");
                }
                catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                {
                    throw new Exception("DELETE request timeout - Server không phản hồi trong thời gian cho phép.");
                }
                catch (TaskCanceledException)
                {
                    throw new Exception("DELETE request bị hủy - Vui lòng thử lại.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ DELETE Unexpected error: {ex}");
                    throw new Exception($"Lỗi không xác định khi thực hiện DELETE request: {ex.Message}");
                }
            }

            throw new Exception($"Không thể thực hiện DELETE request sau {maxRetries + 1} lần thử");
        }

        /// <summary>
        /// Overload DELETE method với request body (cho trường hợp đặc biệt)
        /// </summary>
        protected async Task<HttpResponseMessage> DeleteAsync(string endpoint, object data)
        {
            const int maxRetries = 3;
            int retryCount = 0;

            while (retryCount <= maxRetries)
            {
                HttpContent content = null;
                HttpRequestMessage request = null;
                try
                {
                    var url = BuildUrl(endpoint);
                    content = await CreateHttpContent(data);

                    System.Diagnostics.Debug.WriteLine($"📤 DELETE Request with body: {url}");
                    LogRequestContent(content);

                    request = new HttpRequestMessage(HttpMethod.Delete, url)
                    {
                        Content = content
                    };

                    AddDefaultHeaders(request);

                    var response = await _httpClient.SendAsync(request);

                    System.Diagnostics.Debug.WriteLine($"📥 DELETE Response: {response.StatusCode}");

                    // Log error details for non-success responses
                    if (!response.IsSuccessStatusCode)
                    {
                        await LogErrorResponseDetails(response);
                    }

                    return response;
                }
                catch (Exception ex) when (retryCount < maxRetries && IsRetryableException(ex))
                {
                    retryCount++;
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                    System.Diagnostics.Debug.WriteLine($"🔄 DELETE Retry {retryCount}/{maxRetries} after {delay.TotalSeconds}s - Error: {ex.Message}");

                    content?.Dispose();
                    request?.Dispose();

                    await Task.Delay(delay);
                    continue;
                }
                catch (HttpRequestException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ DELETE HttpRequestException: {ex.Message}");

                    if (ex.InnerException is IOException)
                    {
                        throw new Exception("Kết nối đến server bị gián đoạn khi thực hiện DELETE request. Vui lòng kiểm tra kết nối mạng.");
                    }

                    throw new Exception($"Không thể thực hiện DELETE request đến server: {ex.Message}");
                }
                catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                {
                    throw new Exception("DELETE request timeout - Server không phản hồi trong thời gian cho phép.");
                }
                catch (TaskCanceledException)
                {
                    throw new Exception("DELETE request bị hủy - Vui lòng thử lại.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ DELETE Unexpected error: {ex}");
                    throw new Exception($"Lỗi không xác định khi thực hiện DELETE request: {ex.Message}");
                }
                finally
                {
                    content?.Dispose();
                    request?.Dispose();
                }
            }

            throw new Exception($"Không thể thực hiện DELETE request sau {maxRetries + 1} lần thử");
        }

        #region Private Helper Methods

        /// <summary>
        /// Tạo HttpContent từ object data
        /// </summary>
        private async Task<HttpContent> CreateHttpContent(object data)
        {
            try
            {
                if (data is HttpContent httpContent)
                {
                    return httpContent;
                }

                if (data != null)
                {
                    // ✅ Thêm ReferenceLoopHandling để tránh circular references
                    var jsonSettings = new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        DateFormatHandling = DateFormatHandling.IsoDateFormat,
                        DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                        ContractResolver = new CamelCasePropertyNamesContractResolver(),
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore, // ✅ QUAN TRỌNG
                        DefaultValueHandling = DefaultValueHandling.Ignore,
                        Formatting = Formatting.None
                    };

                    string json;
                    try
                    {
                        json = JsonConvert.SerializeObject(data, jsonSettings);

                        // ✅ Log JSON content để debug
                        System.Diagnostics.Debug.WriteLine($"📋 Serialized JSON: {json}");

                        // ✅ Kiểm tra JSON size
                        var jsonBytes = Encoding.UTF8.GetBytes(json);
                        if (jsonBytes.Length > 10 * 1024 * 1024) // 10MB
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ Large JSON payload: {jsonBytes.Length / 1024}KB");
                        }
                    }
                    catch (JsonSerializationException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ JSON Serialization Error: {ex.Message}");
                        throw new Exception($"Không thể serialize object thành JSON: {ex.Message}", ex);
                    }

                    return new StringContent(json, Encoding.UTF8, "application/json");
                }

                return new StringContent("", Encoding.UTF8, "application/json");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ CreateHttpContent Error: {ex.Message}");
                throw new Exception($"Lỗi tạo HttpContent: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Xây dựng URL từ endpoint
        /// </summary>
        private string BuildUrl(string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint))
                throw new ArgumentException("Endpoint không được để trống", nameof(endpoint));

            return endpoint.StartsWith("/") ? $"{_baseUrl}{endpoint}" : $"{_baseUrl}/{endpoint}";
        }

        /// <summary>
        /// Thêm headers mặc định cho request
        /// </summary>
        private void AddDefaultHeaders(HttpRequestMessage request)
        {
            request.Headers.Add("Cache-Control", "no-cache");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");

            // Thêm correlation ID cho tracking
            request.Headers.Add("X-Correlation-ID", Guid.NewGuid().ToString());

        }

        /// <summary>
        /// Kiểm tra xem exception có thể retry được không
        /// </summary>
        private bool IsRetryableException(Exception ex)
        {
            return ex is HttpRequestException ||
                   ex is TaskCanceledException ||
                   ex is IOException ||
                   (ex is WebException webEx && IsRetryableWebException(webEx));
        }

        /// <summary>
        /// Kiểm tra WebException có thể retry được không
        /// </summary>
        private bool IsRetryableWebException(WebException ex)
        {
            return ex.Status == WebExceptionStatus.Timeout ||
                   ex.Status == WebExceptionStatus.ConnectFailure ||
                   ex.Status == WebExceptionStatus.ReceiveFailure ||
                   ex.Status == WebExceptionStatus.SendFailure ||
                   ex.Status == WebExceptionStatus.NameResolutionFailure;
        }

        /// <summary>
        /// Log nội dung request (chỉ trong Debug mode)
        /// </summary>
        private async void LogRequestContent(HttpContent content)
        {
#if DEBUG
            // Log thông tin cơ bản mà không consume HttpContent
            if (content != null)
            {
                var headers = string.Join(", ", content.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}"));
                System.Diagnostics.Debug.WriteLine($"📄 Request Content-Type: {content.Headers.ContentType}");
                if (!string.IsNullOrEmpty(headers))
                {
                    System.Diagnostics.Debug.WriteLine($"📄 Request Headers: {headers}");
                }
            }
#endif
        }

        #endregion

        #endregion

        /// <summary>
        /// Đặt session token cho các request tiếp theo
        /// </summary>
        public void SetSessionToken(string token)
        {
            _sessionToken = token;
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            }
        }

        /// <summary>
        /// Đăng nhập vào hệ thống
        /// </summary>
        public async Task<LoginResponse> LoginAsync(string usernameOrEmail, string password, bool rememberMe = false)
        {
            try
            {
                // ✅ Debug trước khi gọi API
                System.Diagnostics.Debug.WriteLine("=== LOGIN DEBUG ===");
                System.Diagnostics.Debug.WriteLine($"HttpClient.BaseAddress: {_httpClient.BaseAddress}");
                System.Diagnostics.Debug.WriteLine($"_baseUrl: {_baseUrl}");

                var request = new LoginRequest
                {
                    UsernameOrEmail = usernameOrEmail,
                    Password = password,
                    RememberMe = rememberMe
                };

                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/users/login", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<LoginResponse>>(responseContent);
                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        // Lưu session token
                        SetSessionToken(apiResponse.Data.SessionToken);
                        return apiResponse.Data;
                    }
                    else
                    {
                        throw new Exception(apiResponse?.Message ?? "Đăng nhập thất bại");
                    }
                }
                else
                {
                    var errorResponse = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent);
                    throw new Exception(errorResponse?.Message ?? $"HTTP Error: {response.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Không thể kết nối đến server: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                throw new Exception("Request timeout - Vui lòng thử lại");
            }
        }

        /// <summary>
        /// Đăng xuất khỏi hệ thống
        /// </summary>
        public async Task<bool> LogoutAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_sessionToken))
                    return true;

                var request = new { SessionToken = _sessionToken };
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/users/logout", content);

                // Clear session token
                SetSessionToken("");

                return response.IsSuccessStatusCode;
            }
            catch
            {
                // Even if logout fails, clear local session
                SetSessionToken("");
                return true;
            }
        }

        /// <summary>
        /// Lấy thông tin user hiện tại
        /// </summary>
        public async Task<UserDto> GetCurrentUserAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_sessionToken))
                    throw new Exception("Chưa đăng nhập");

                var response = await _httpClient.GetAsync($"{_baseUrl}/api/users/me");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<UserDto>>(responseContent);
                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }
                    else
                    {
                        throw new Exception(apiResponse?.Message ?? "Không thể lấy thông tin user");
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    SetSessionToken(""); // Clear invalid token
                    throw new UnauthorizedAccessException("Session đã hết hạn, vui lòng đăng nhập lại");
                }
                else
                {
                    var errorResponse = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent);
                    throw new Exception(errorResponse?.Message ?? $"HTTP Error: {response.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Không thể kết nối đến server: {ex.Message}");
            }
        }

        /// <summary>
        /// Kiểm tra kết nối đến server
        /// </summary>
        public async Task<bool> CheckServerConnectionAsync()
        {
            try
            {
                var response = await GetAsync("/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get current session token
        /// </summary>
        public string GetSessionToken()
        {
            return _sessionToken ?? "";
        }

        /// <summary>
        /// Check if user is logged in
        /// </summary>
        public bool IsLoggedIn()
        {
            return !string.IsNullOrEmpty(_sessionToken);
        }

        public virtual void Dispose()
        {
            SetSessionToken("");
        }

        /// <summary>
        /// Log chi tiết HTTP 400 Bad Request errors
        /// </summary>
        private async Task LogBadRequestDetails(HttpResponseMessage response)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔍 === HTTP 400 BAD REQUEST DETAILS ===");

                // Log response headers
                System.Diagnostics.Debug.WriteLine("📋 Response Headers:");
                foreach (var header in response.Headers)
                {
                    System.Diagnostics.Debug.WriteLine($"   {header.Key}: {string.Join(", ", header.Value)}");
                }

                if (response.Content != null)
                {
                    foreach (var contentHeader in response.Content.Headers)
                    {
                        System.Diagnostics.Debug.WriteLine($"   {contentHeader.Key}: {string.Join(", ", contentHeader.Value)}");
                    }
                }

                // Log response content
                if (response.Content != null)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"📄 Error Content: {errorContent}");

                    // Try to parse as JSON for better readability
                    try
                    {
                        var errorJson = JsonConvert.DeserializeObject(errorContent);
                        var formattedJson = JsonConvert.SerializeObject(errorJson, Formatting.Indented);
                        System.Diagnostics.Debug.WriteLine($"📄 Formatted Error JSON:\n{formattedJson}");
                    }
                    catch
                    {
                        // If not valid JSON, just log as-is
                        System.Diagnostics.Debug.WriteLine($"📄 Raw Error Content: {errorContent}");
                    }
                }

                System.Diagnostics.Debug.WriteLine("🔍 === END BAD REQUEST DETAILS ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error logging bad request details: {ex.Message}");
            }
        }

        /// <summary>
        /// Log chi tiết tất cả error responses
        /// </summary>
        private async Task LogErrorResponseDetails(HttpResponseMessage response)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🚨 === ERROR RESPONSE {response.StatusCode} ===");
                System.Diagnostics.Debug.WriteLine($"📊 Status: {(int)response.StatusCode} {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"📊 Reason: {response.ReasonPhrase}");

                if (response.Content != null)
                {
                    var contentType = response.Content.Headers.ContentType?.MediaType;
                    System.Diagnostics.Debug.WriteLine($"📊 Content-Type: {contentType}");

                    // Read and log error content based on content type
                    var errorContent = await response.Content.ReadAsStringAsync();

                    if (contentType?.Contains("application/json") == true ||
                        contentType?.Contains("application/problem+json") == true)
                    {
                        System.Diagnostics.Debug.WriteLine("📄 JSON Error Content:");
                        try
                        {
                            var errorObj = JsonConvert.DeserializeObject(errorContent);
                            var formatted = JsonConvert.SerializeObject(errorObj, Formatting.Indented);
                            System.Diagnostics.Debug.WriteLine(formatted);
                        }
                        catch
                        {
                            System.Diagnostics.Debug.WriteLine(errorContent);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"📄 Error Content: {errorContent}");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"🚨 === END ERROR RESPONSE ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error logging response details: {ex.Message}");
            }
        }
    }
}