using ManagementFile.API.Data;
using ManagementFile.Contracts.Enums;
using ManagementFile.Models.AuditAndLogging;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ManagementFile.API.Middleware
{
    /// <summary>
    /// Middleware xử lý authentication và authorization
    /// Chịu trách nhiệm xác thực session token và set user info vào HttpContext
    /// </summary>
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthenticationMiddleware> _logger;

        public AuthenticationMiddleware(RequestDelegate next, ILogger<AuthenticationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ManagementFileDbContext dbContext)
        {
            try
            {
                // Log thông tin request để debug
                _logger.LogDebug("Auth middleware xử lý: {Method} {Path}",
                    context.Request.Method, context.Request.Path);

                // Kiểm tra response đã started chưa - ngăn chặn lỗi "StatusCode cannot be set"
                if (context.Response.HasStarted)
                {
                    _logger.LogWarning("Response đã started, bỏ qua auth middleware");
                    return;
                }

                // Kiểm tra xem có phải endpoint công khai không
                if (IsPublicEndpoint(context.Request.Path))
                {
                    _logger.LogDebug("Endpoint công khai, bỏ qua authentication");
                    await _next(context);
                    return;
                }

                // Chỉ xử lý authentication cho các API endpoints
                var path = context.Request.Path.Value?.ToLower() ?? "";
                if (!path.StartsWith("/api/"))
                {
                    _logger.LogDebug("Không phải API endpoint, bỏ qua authentication");
                    await _next(context);
                    return;
                }

                // Lấy session token từ request
                var sessionToken = GetSessionToken(context);
                if (string.IsNullOrEmpty(sessionToken))
                {
                    _logger.LogInformation("Không tìm thấy session token cho {Path}", context.Request.Path);
                    await WriteUnauthorizedResponseSafe(context, "Session token không được cung cấp");
                    return;
                }

                // Validate session token với database
                var validationResult = await ValidateSessionAsync(dbContext, sessionToken);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Session validation thất bại: {Error} cho token {TokenPrefix}...",
                        validationResult.ErrorMessage,
                        sessionToken.Length > 10 ? sessionToken.Substring(0, 10) : sessionToken);

                    await WriteUnauthorizedResponseSafe(context, validationResult.ErrorMessage);
                    return;
                }

                // Session hợp lệ - cập nhật activity và set context
                await UpdateSessionActivity(dbContext, validationResult.Session!);
                SetUserContextInfo(context, validationResult.Session!);

                _logger.LogDebug("Authentication thành công cho user {UserId}", validationResult.Session!.UserId);

                // Tiếp tục pipeline
                await _next(context);

                // Lưu thay đổi activity sau khi request hoàn thành (nếu response chưa started)
                if (!context.Response.HasStarted)
                {
                    await SaveSessionChanges(dbContext);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không mong đợi trong authentication middleware");

                // Chỉ write error response nếu response chưa started
                if (!context.Response.HasStarted)
                {
                    await WriteErrorResponseSafe(context, "Lỗi xác thực hệ thống");
                }
            }
        }

        /// <summary>
        /// Kiểm tra xem endpoint có phải là public (không cần authentication) không
        /// </summary>
        private bool IsPublicEndpoint(PathString path)
        {
            var publicPaths = new[]
            {
                "/health",           // Health check endpoint
                "/swagger",          // Swagger documentation  
                "/api/users/login",  // Login endpoint
                "/api/users/logout", // Logout endpoint
                "/api/users/register", // Registration (nếu có)
                "/favicon.ico"       // Browser favicon requests
            };

            var pathValue = path.Value?.ToLower() ?? "";
            return publicPaths.Any(p => pathValue.StartsWith(p));
        }

        /// <summary>
        /// Lấy session token từ Authorization header hoặc query parameter
        /// </summary>
        private string? GetSessionToken(HttpContext context)
        {
            try
            {
                // Ưu tiên lấy từ Authorization header (Bearer token)
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    var token = authHeader.Substring(7).Trim(); // Bỏ "Bearer " prefix
                    _logger.LogDebug("Tìm thấy Bearer token với độ dài {Length}", token.Length);
                    return token;
                }

                // Fallback: tìm trong query parameter (cho các trường hợp đặc biệt)
                var queryToken = context.Request.Query["token"].FirstOrDefault();
                if (!string.IsNullOrEmpty(queryToken))
                {
                    _logger.LogDebug("Tìm thấy token trong query parameter");
                    return queryToken;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy session token");
                return null;
            }
        }

        /// <summary>
        /// Validate session token với database và trả về kết quả validation
        /// </summary>
        private async Task<SessionValidationResult> ValidateSessionAsync(ManagementFileDbContext dbContext, string sessionToken)
        {
            try
            {
                _logger.LogDebug("Đang validate session token...");

                // Query session từ database với eager loading User
                var session = await dbContext.UserSessions
                    .Include(s => s.User)
                    .Where(s => s.SessionToken == sessionToken)
                    .FirstOrDefaultAsync();

                // Kiểm tra session có tồn tại không
                if (session == null)
                {
                    return SessionValidationResult.Invalid("Session token không tồn tại");
                }

                // Kiểm tra session có active không
                if (!session.IsActive)
                {
                    return SessionValidationResult.Invalid("Session đã bị vô hiệu hóa");
                }

                // Kiểm tra session có hết hạn không
                if (session.ExpiresAt <= DateTime.UtcNow)
                {
                    // Tự động deactivate session hết hạn
                    session.MarkAsExpired();
                    session.DeactivationReason = "Expired";

                    return SessionValidationResult.Invalid("Session đã hết hạn");
                }

                // Kiểm tra user có active không
                if (session.User == null || !session.User.IsActive)
                {
                    return SessionValidationResult.Invalid("Tài khoản người dùng không hoạt động");
                }

                _logger.LogDebug("Session validation thành công cho user {UserId}", session.UserId);
                return SessionValidationResult.Valid(session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi validate session");
                return SessionValidationResult.Invalid("Lỗi xác thực session");
            }
        }

        /// <summary>
        /// Cập nhật thông tin activity của session
        /// </summary>
        private async Task UpdateSessionActivity(ManagementFileDbContext dbContext, UserSession session)
        {
            try
            {
                // Cập nhật last activity time và tăng activity count
                session.LastActivityAt = DateTime.UtcNow;
                session.ActivityCount++;

                // Mark entity đã được modified để EF Core track changes
                dbContext.Entry(session).State = EntityState.Modified;

                _logger.LogDebug("Cập nhật activity cho session {SessionId}", session.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật session activity");
            }
        }

        /// <summary>
        /// Set thông tin user và session vào HttpContext.Items để controller sử dụng
        /// </summary>
        private void SetUserContextInfo(HttpContext context, UserSession session)
        {
            try
            {
                // Set các thông tin cần thiết vào HttpContext.Items
                context.Items["CurrentUser"] = session.User;
                context.Items["CurrentSession"] = session;
                context.Items["UserId"] = session.UserId; // Quan trọng: Controller cần UserId

                _logger.LogDebug("Set user context info cho UserId: {UserId}", session.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi set user context info");
            }
        }

        /// <summary>
        /// Lưu thay đổi session vào databaseư
        /// </summary>
        private async Task SaveSessionChanges(ManagementFileDbContext dbContext)
        {
            try
            {
                await dbContext.SaveChangesAsync();
                _logger.LogDebug("Lưu session changes thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu session changes");
                // Không throw exception để không ảnh hưởng đến response
            }
        }

        /// <summary>
        /// Viết unauthorized response một cách an toàn (kiểm tra HasStarted)
        /// </summary>
        private async Task WriteUnauthorizedResponseSafe(HttpContext context, string message)
        {
            try
            {
                // Kiểm tra lại response chưa started
                if (context.Response.HasStarted)
                {
                    _logger.LogWarning("Không thể viết unauthorized response - response đã started");
                    return;
                }

                // Set status code và content type
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json; charset=utf-8";

                // Tạo response object theo format chuẩn
                var response = new
                {
                    success = false,
                    message = message ?? "Unauthorized",
                    data = (object?)null,
                    errors = new[] { message ?? "Unauthorized" },
                    statusCode = 401,
                    timestamp = DateTime.UtcNow
                };

                // Serialize và write response
                var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await context.Response.WriteAsync(json);
                _logger.LogInformation("Đã viết 401 Unauthorized response");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("response has already started"))
            {
                _logger.LogError("Không thể set status code - response đã started: {Message}", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi viết unauthorized response");
            }
        }

        /// <summary>
        /// Viết error response một cách an toàn cho các lỗi hệ thống
        /// </summary>
        private async Task WriteErrorResponseSafe(HttpContext context, string message)
        {
            try
            {
                if (context.Response.HasStarted)
                {
                    _logger.LogWarning("Không thể viết error response - response đã started");
                    return;
                }

                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json; charset=utf-8";

                var response = new
                {
                    success = false,
                    message = message ?? "Internal server error",
                    data = (object?)null,
                    errors = new[] { message ?? "Internal server error" },
                    statusCode = 500,
                    timestamp = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await context.Response.WriteAsync(json);
                _logger.LogInformation("Đã viết 500 Error response");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi viết error response");
            }
        }

        /// <summary>
        /// Class chứa kết quả validation session
        /// </summary>
        private class SessionValidationResult
        {
            public bool IsValid { get; set; }
            public UserSession? Session { get; set; }
            public string ErrorMessage { get; set; } = "";

            public static SessionValidationResult Valid(UserSession session) => new()
            {
                IsValid = true,
                Session = session
            };

            public static SessionValidationResult Invalid(string errorMessage) => new()
            {
                IsValid = false,
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// Middleware ghi lại audit trail cho các request
    /// Chạy sau AuthenticationMiddleware để có thông tin user
    /// </summary>
    public class AuditLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuditLoggingMiddleware> _logger;

        public AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ManagementFileDbContext dbContext)
        {
            // Bắt đầu đo thời gian xử lý request
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var requestTime = DateTime.UtcNow;

            try
            {
                // Tiếp tục pipeline
                await _next(context);
            }
            finally
            {
                // Dừng đo thời gian
                stopwatch.Stop();

                // Ghi log audit nếu cần thiết
                var path = context.Request.Path.Value?.ToLower() ?? "";
                if (path.StartsWith("/api/") && ShouldLogRequest(context))
                {
                    await LogRequestAsync(context, dbContext, requestTime, stopwatch.ElapsedMilliseconds);
                }
            }
        }

        /// <summary>
        /// Xác định có nên ghi log request này không
        /// Để giảm noise, chỉ log các request quan trọng và đã authenticated
        /// </summary>
        private static bool ShouldLogRequest(HttpContext context)
        {
            var method = context.Request.Method.ToUpper();

            // Luôn log các method thay đổi dữ liệu
            var modifyingMethods = new[] { "POST", "PUT", "PATCH", "DELETE" };
            if (modifyingMethods.Contains(method))
            {
                return true;
            }

            // Log các request có lỗi (status >= 400)
            if (context.Response.StatusCode >= 400)
            {
                return true;
            }

            // Chỉ log các request đã authenticated để tránh noise từ public endpoints
            var currentUser = context.Items["CurrentUser"] as ManagementFile.Models.UserManagement.User;
            if (currentUser != null && method == "GET")
            {
                return true;
            }

            // Không log GET và OPTIONS thông thường để giảm noise
            return false;
        }

        /// <summary>
        /// Ghi log request vào audit trail
        /// </summary>
        private async Task LogRequestAsync(HttpContext context, ManagementFileDbContext dbContext,
            DateTime requestTime, long durationMs)
        {
            try
            {
                // Lấy thông tin user từ context (đã được set bởi AuthenticationMiddleware)
                var currentUser = context.Items["CurrentUser"] as ManagementFile.Models.UserManagement.User;
                var session = context.Items["CurrentSession"] as UserSession;

                // Kiểm tra xem user và session có tồn tại không
                // Một số request có thể không có authentication (public endpoints)
                if (currentUser == null || session == null)
                {
                    _logger.LogDebug("Bỏ qua audit log cho request không có authentication: {Method} {Path}",
                        context.Request.Method, context.Request.Path);
                    return;
                }

                // Tạo audit log entry
                var auditLog = new AuditLog
                {
                    UserId = currentUser.Id,
                    EntityType = "HttpRequest",
                    EntityId = -1, // Random ID cho HTTP request
                    Action = GetAuditAction(context.Request.Method),
                    OldValues = "", // HTTP request không có old values
                    NewValues = JsonSerializer.Serialize(new
                    {
                        Method = context.Request.Method,
                        Path = context.Request.Path.Value,
                        QueryString = context.Request.QueryString.Value,
                        StatusCode = context.Response.StatusCode,
                        Duration = durationMs,
                        UserAgent = context.Request.Headers["User-Agent"].FirstOrDefault()
                    }),
                    Changes = $"HTTP {context.Request.Method} {context.Request.Path} -> {context.Response.StatusCode}",
                    IPAddress = GetClientIpAddress(context),
                    UserAgent = context.Request.Headers["User-Agent"].FirstOrDefault() ?? "",
                    SessionId = session.Id,
                    CreatedAt = requestTime,
                    CreatedBy = currentUser.Id
                };

                // Thêm vào database
                dbContext.AuditLogs.Add(auditLog);
                await dbContext.SaveChangesAsync();

                _logger.LogDebug("Ghi audit log thành công cho {Method} {Path}",
                    context.Request.Method, context.Request.Path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi ghi audit log");
                // Không throw exception để không ảnh hưởng đến response
            }
        }

        /// <summary>
        /// Chuyển đổi HTTP method thành AuditAction enum
        /// </summary>
        private static AuditAction GetAuditAction(string httpMethod)
        {
            return httpMethod.ToUpper() switch
            {
                "GET" => AuditAction.Read,
                "POST" => AuditAction.Create,
                "PUT" => AuditAction.Update,
                "PATCH" => AuditAction.Update,
                "DELETE" => AuditAction.Delete,
                _ => AuditAction.Other
            };
        }

        /// <summary>
        /// Lấy IP address thật của client (xử lý proxy/load balancer)
        /// </summary>
        private static string GetClientIpAddress(HttpContext context)
        {
            try
            {
                // Kiểm tra các header proxy phổ biến
                var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrEmpty(forwardedFor))
                {
                    // X-Forwarded-For có thể chứa nhiều IP, lấy IP đầu tiên
                    return forwardedFor.Split(',')[0].Trim();
                }

                var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
                if (!string.IsNullOrEmpty(realIp))
                {
                    return realIp.Trim();
                }

                // Fallback về RemoteIpAddress
                return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            }
            catch
            {
                return "unknown";
            }
        }
    }

    /// <summary>
    /// Extension methods để đăng ký middleware dễ dàng
    /// </summary>
    public static class MiddlewareExtensions
    {
        /// <summary>
        /// Đăng ký custom authentication middleware
        /// </summary>
        public static IApplicationBuilder UseCustomAuthentication(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuthenticationMiddleware>();
        }

        /// <summary>
        /// Đăng ký audit logging middleware
        /// </summary>
        public static IApplicationBuilder UseAuditLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuditLoggingMiddleware>();
        }
    }
}