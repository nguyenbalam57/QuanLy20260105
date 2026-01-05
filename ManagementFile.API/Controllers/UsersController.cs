using ManagementFile.API.Data;
using ManagementFile.Models.UserManagement;
using ManagementFile.Models.AuditAndLogging;
using ManagementFile.API.Services;
using ManagementFile.Contracts.DTOs.UserManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using ManagementFile.Contracts.Requests.UserManagement;
using ManagementFile.Contracts.DTOs.Common;
using ManagementFile.Contracts.Responses.UserManagement;

namespace ManagementFile.API.Controllers
{
    /// <summary>
    /// Controller quản lý Users và Authentication
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ManagementFileDbContext _context;
        private readonly ILogger<UsersController> _logger;

        public UsersController(ManagementFileDbContext context, ILogger<UsersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Authentication Endpoints

        /// <summary>
        /// Đăng nhập vào hệ thống
        /// POST: api/users/login
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(LoginRequest request)
        {
            try
            {
                // Tìm user theo username hoặc email
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => 
                        (u.Username == request.UsernameOrEmail || u.Email == request.UsernameOrEmail)
                        && u.IsActive && !u.IsDeleted);

                if (user == null)
                {
                    return BadRequest(ApiResponse<LoginResponse>.ErrorResult("Tên đăng nhập hoặc mật khẩu không chính xác"));
                }

                // Kiểm tra tài khoản có bị khóa không
                if (user.IsAccountLocked)
                {
                    return BadRequest(ApiResponse<LoginResponse>.ErrorResult($"Tài khoản bị khóa đến {user.LockedUntil:dd/MM/yyyy HH:mm}"));
                }

                // Verify password
                if (!VerifyPassword(request.Password, user.PasswordHash))
                {
                    // Ghi nhận đăng nhập thất bại
                    user.RecordLoginFailure();
                    await _context.SaveChangesAsync();
                    
                    return BadRequest(ApiResponse<LoginResponse>.ErrorResult("Tên đăng nhập hoặc mật khẩu không chính xác"));
                }

                // Đăng nhập thành công
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
                user.RecordLoginSuccess(ipAddress);

                // Tạo session
                var sessionExpiry = request.RememberMe ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddHours(8);
                var session = new UserSession
                {
                    UserId = user.Id,
                    SessionToken = GenerateSessionToken(),
                    LoginAt = DateTime.UtcNow,
                    ExpiresAt = sessionExpiry,
                    IPAddress = ipAddress,
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    IsActive = true,
                    CreatedBy = user.Id
                };

                _context.UserSessions.Add(session);
                await _context.SaveChangesAsync();

                var loginResponse = new LoginResponse
                {
                    Success = true,
                    Message = "Đăng nhập thành công",
                    User = UserService.MapToUserDto(user),
                    SessionToken = session.SessionToken,
                    ExpiresAt = session.ExpiresAt
                };

                _logger.LogInformation("User {UserId} logged in successfully from {IPAddress}", user.Id, ipAddress);

                return Ok(ApiResponse<LoginResponse>.SuccessResult(loginResponse, "Đăng nhập thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for {UsernameOrEmail}", request.UsernameOrEmail);
                return StatusCode(500, ApiResponse<LoginResponse>.ErrorResult("Lỗi hệ thống khi đăng nhập"));
            }
        }

        /// <summary>
        /// Đăng xuất khỏi hệ thống
        /// POST: api/users/logout
        /// </summary>
        [HttpPost("logout")]
        public async Task<ActionResult<ApiResponse<string>>> Logout(LogoutRequest request)
        {
            try
            {
                var session = await _context.UserSessions
                    .FirstOrDefaultAsync(s => s.SessionToken == request.SessionToken && s.IsActive);

                if (session != null)
                {
                    session.LogoutAt = DateTime.UtcNow;
                    session.IsActive = false;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("User {UserId} logged out", session.UserId);
                }

                return Ok(ApiResponse<string>.SuccessResult("", "Đăng xuất thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, ApiResponse<string>.ErrorResult("Lỗi hệ thống khi đăng xuất"));
            }
        }

        /// <summary>
        /// Lấy thông tin user hiện tại từ session
        /// GET: api/users/me
        /// </summary>
        [HttpGet("me")]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser([FromHeader(Name = "Authorization")] string? sessionToken)
        {
            try
            {
                if (string.IsNullOrEmpty(sessionToken))
                {
                    return Unauthorized(ApiResponse<UserDto>.ErrorResult("Session token không hợp lệ"));
                }

                // Loại bỏ "Bearer " prefix nếu có
                sessionToken = sessionToken.Replace("Bearer ", "").Trim();

                var session = await _context.UserSessions
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.SessionToken == sessionToken 
                                           && s.IsActive 
                                           && s.ExpiresAt > DateTime.UtcNow);

                if (session == null)
                {
                    return Unauthorized(ApiResponse<UserDto>.ErrorResult("Session đã hết hạn"));
                }

                // Cập nhật last activity
                session.LastActivityAt = DateTime.UtcNow;
                session.ActivityCount++;
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<UserDto>.SuccessResult(UserService.MapToUserDto(session.User)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, ApiResponse<UserDto>.ErrorResult("Lỗi hệ thống"));
            }
        }

        #endregion

        #region CRUD Operations

        /// <summary>
        /// Lấy danh sách users với tìm kiếm và phân trang
        /// GET: api/users
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<UserListResponse>>> GetUsers([FromQuery] UserSearchRequest request)
        {
            try
            {
                var query = _context.Users.Where(u => u.IsActive && !u.IsDeleted);

                // Áp dụng filters
                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    var searchTerm = request.SearchTerm.ToLower();
                    query = query.Where(u => 
                        u.FullName.ToLower().Contains(searchTerm) ||
                        u.Username.ToLower().Contains(searchTerm) ||
                        u.Email.ToLower().Contains(searchTerm) ||
                        u.PhoneNumber.Contains(searchTerm));
                }

                if (request.Role.HasValue)
                {
                    query = query.Where(u => u.Role == request.Role.Value);
                }

                if (request.Department.HasValue)
                {
                    query = query.Where(u => u.Department == request.Department.Value);
                }

                if (request.IsActive.HasValue)
                {
                    query = query.Where(u => u.IsActive == request.IsActive.Value);
                }

                // Đếm tổng số
                var totalCount = await query.CountAsync();

                // Áp dụng sorting
                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    var isAsc = request.SortDirection?.ToLower() != "desc";
                    query = request.SortBy.ToLower() switch
                    {
                        "username" => isAsc ? query.OrderBy(u => u.Username) : query.OrderByDescending(u => u.Username),
                        "email" => isAsc ? query.OrderBy(u => u.Email) : query.OrderByDescending(u => u.Email),
                        "createdat" => isAsc ? query.OrderBy(u => u.CreatedAt) : query.OrderByDescending(u => u.CreatedAt),
                        "lastloginat" => isAsc ? query.OrderBy(u => u.LastLoginAt) : query.OrderByDescending(u => u.LastLoginAt),
                        _ => isAsc ? query.OrderBy(u => u.FullName) : query.OrderByDescending(u => u.FullName)
                    };
                }
                else
                {
                    query = query.OrderBy(u => u.FullName);
                }

                // Phân trang
                var pageSize = Math.Min(request.PageSize, 100); // Giới hạn page size tối đa 100
                var skip = (request.PageNumber - 1) * pageSize;
                
                var users = await query
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();

                var userDtos = users.Select(UserService.MapToUserDto).ToList();
                
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var response = new UserListResponse
                {
                    Users = userDtos,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    HasPreviousPage = request.PageNumber > 1,
                    HasNextPage = request.PageNumber < totalPages
                };

                return Ok(ApiResponse<UserListResponse>.SuccessResult(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users list");
                return StatusCode(500, ApiResponse<UserListResponse>.ErrorResult("Lỗi hệ thống khi lấy danh sách users"));
            }
        }

        /// <summary>
        /// Lấy thông tin user theo ID
        /// GET: api/users/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);

                if (user == null || !user.IsActive || user.IsDeleted)
                {
                    return NotFound(ApiResponse<UserDto>.ErrorResult("Không tìm thấy user"));
                }

                return Ok(ApiResponse<UserDto>.SuccessResult(UserService.MapToUserDto(user)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId}", id);
                return StatusCode(500, ApiResponse<UserDto>.ErrorResult("Lỗi hệ thống khi lấy thông tin user"));
            }
        }

        [HttpGet("by-usernames")]
        public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetUsersByUsernames([FromQuery] List<string> usernames)
        {
            try
            {
                var users = await _context.Users
                    .Where(u => usernames.Contains(u.Username) && u.IsActive && !u.IsDeleted)
                    .ToListAsync();
                var userDtos = users.Select(UserService.MapToUserDto).ToList();
                return Ok(ApiResponse<List<UserDto>>.SuccessResult(userDtos));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users by usernames");
                return StatusCode(500, ApiResponse<List<UserDto>>.ErrorResult("Lỗi hệ thống khi lấy thông tin users"));
            }
        }

        [HttpGet("by-email/{email}")]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetUserByEmail(string email)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email && u.IsActive && !u.IsDeleted);
                if (user == null)
                {
                    return NotFound(ApiResponse<UserDto>.ErrorResult("Không tìm thấy user"));
                }
                return Ok(ApiResponse<UserDto>.SuccessResult(UserService.MapToUserDto(user)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email {Email}", email);
                return StatusCode(500, ApiResponse<UserDto>.ErrorResult("Lỗi hệ thống khi lấy thông tin user"));
            }
        }

        /// <summary>
        /// Tạo user mới
        /// POST: api/users
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser(CreateUserRequest request)
        {
            try
            {
                // Kiểm tra username đã tồn tại
                if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                {
                    return BadRequest(ApiResponse<UserDto>.ErrorResult("Username đã tồn tại"));
                }

                // Kiểm tra email đã tồn tại
                if ( string.IsNullOrWhiteSpace(request.Email) && await _context.Users.AnyAsync(u => u.Email == request.Email))
                {
                    return BadRequest(ApiResponse<UserDto>.ErrorResult("Email đã tồn tại"));
                }

                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    FullName = request.FullName,
                    PasswordHash = HashPassword(request.Password),
                    Role = request.Role,
                    Department = request.Department,
                    PhoneNumber = request.PhoneNumber ?? "",
                    Position = request.Position ?? "",
                    ManagerId = request.ManagerId ?? 0,
                    Language = request.Language ?? "vi-VN",
                    IsActive = true,
                    CreatedBy = GetCurrentUserId()
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} created successfully", user.Id);

                return CreatedAtAction(nameof(GetUser), 
                    new { id = user.Id }, 
                    ApiResponse<UserDto>.SuccessResult(UserService.MapToUserDto(user), "Tạo user thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, ApiResponse<UserDto>.ErrorResult("Lỗi hệ thống khi tạo user"));
            }
        }

        /// <summary>
        /// Cập nhật thông tin user
        /// PUT: api/users/{id}
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(int id, UpdateUserRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null || !user.IsActive || user.IsDeleted)
                {
                    return NotFound(ApiResponse<UserDto>.ErrorResult("Không tìm thấy user"));
                }

                // Kiểm tra email unique (trừ user hiện tại)
                if (await _context.Users.AnyAsync(u => u.Email == request.Email && u.Id != id))
                {
                    return BadRequest(ApiResponse<UserDto>.ErrorResult("Email đã tồn tại"));
                }

                // Cập nhật thông tin
                user.Email = request.Email;
                user.FullName = request.FullName;
                user.PhoneNumber = request.PhoneNumber ?? "";
                user.Position = request.Position ?? "";
                user.Role = request.Role;
                user.Department = request.Department;
                user.ManagerId = request.ManagerId ?? 0;
                user.Language = request.Language ?? user.Language;
                user.Avatar = request.Avatar ?? user.Avatar;
                user.MarkAsUpdated(GetCurrentUserId());

                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} updated successfully", user.Id);

                return Ok(ApiResponse<UserDto>.SuccessResult(UserService.MapToUserDto(user), "Cập nhật user thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                return StatusCode(500, ApiResponse<UserDto>.ErrorResult("Lỗi hệ thống khi cập nhật user"));
            }
        }

        /// <summary>
        /// Xóa user (soft delete)
        /// DELETE: api/users/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteUser(string id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(ApiResponse<string>.ErrorResult("Không tìm thấy user"));
                }

                user.SoftDelete(GetCurrentUserId());
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} deleted successfully", user.Id);

                return Ok(ApiResponse<string>.SuccessResult("", "Xóa user thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                return StatusCode(500, ApiResponse<string>.ErrorResult("Lỗi hệ thống khi xóa user"));
            }
        }

        #endregion

        #region Password Management

        /// <summary>
        /// Đổi mật khẩu
        /// POST: api/users/{id}/change-password
        /// </summary>
        [HttpPost("{id}/change-password")]
        public async Task<ActionResult<ApiResponse<string>>> ChangePassword(int id, ChangePasswordRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null || !user.IsActive || user.IsDeleted)
                {
                    return NotFound(ApiResponse<string>.ErrorResult("Không tìm thấy user"));
                }

                // Verify current password
                if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
                {
                    return BadRequest(ApiResponse<string>.ErrorResult("Mật khẩu hiện tại không chính xác"));
                }

                // Update password
                user.PasswordHash = HashPassword(request.NewPassword);
                user.MarkAsUpdated(GetCurrentUserId());

                // Vô hiệu hóa tất cả sessions hiện tại (buộc đăng nhập lại)
                var activeSessions = await _context.UserSessions
                    .Where(s => s.UserId == id && s.IsActive)
                    .ToListAsync();

                foreach (var session in activeSessions)
                {
                    session.IsActive = false;
                    session.LogoutAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Password changed for user {UserId}", user.Id);

                return Ok(ApiResponse<string>.SuccessResult("", "Đổi mật khẩu thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", id);
                return StatusCode(500, ApiResponse<string>.ErrorResult("Lỗi hệ thống khi đổi mật khẩu"));
            }
        }

        /// <summary>
        /// Mở khóa tài khoản
        /// POST: api/users/{id}/unlock
        /// </summary>
        [HttpPost("{id}/unlock")]
        public async Task<ActionResult<ApiResponse<string>>> UnlockUser(string id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(ApiResponse<string>.ErrorResult("Không tìm thấy user"));
                }

                user.UnlockAccount(GetCurrentUserId());
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} unlocked successfully", user.Id);

                return Ok(ApiResponse<string>.SuccessResult("", "Mở khóa tài khoản thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlocking user {UserId}", id);
                return StatusCode(500, ApiResponse<string>.ErrorResult("Lỗi hệ thống khi mở khóa tài khoản"));
            }
        }

        /// <summary>
        /// reset mật khẩu
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("{id}/reset-password")]
        public async Task<ActionResult<ApiResponse<string>>> ResetPassword(int id, ResetPasswordRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(ApiResponse<string>.ErrorResult("Không tìm thấy user"));
                }
                user.PasswordHash = HashPassword(request.NewPassword);
                user.MarkAsUpdated(GetCurrentUserId());
                // Vô hiệu hóa tất cả sessions hiện tại (buộc đăng nhập lại)
                var activeSessions = await _context.UserSessions
                    .Where(s => s.UserId == id && s.IsActive)
                    .ToListAsync();
                foreach (var session in activeSessions)
                {
                    session.IsActive = false;
                    session.LogoutAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();
                _logger.LogInformation("Password reset for user {UserId}", user.Id);
                return Ok(ApiResponse<string>.SuccessResult("", "Đặt lại mật khẩu thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user {UserId}", id);
                return StatusCode(500, ApiResponse<string>.ErrorResult("Lỗi hệ thống khi đặt lại mật khẩu"));
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Hash password sử dụng BCrypt
        /// </summary>
        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
        }

        /// <summary>
        /// Verify password với hash
        /// </summary>
        private bool VerifyPassword(string password, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Tạo session token
        /// </summary>
        private string GenerateSessionToken()
        {
            using var rng = RandomNumberGenerator.Create();
            byte[] bytes = new byte[32];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }

        /// <summary>
        /// Lấy User ID hiện tại từ session
        /// </summary>
        private int GetCurrentUserId()
        {
            var sessionToken = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            if (string.IsNullOrEmpty(sessionToken))
                return -1;

            var session = _context.UserSessions
                .FirstOrDefault(s => s.SessionToken == sessionToken && s.IsActive && s.ExpiresAt > DateTime.UtcNow);
            
            return session.UserId;
        }

        #endregion
    }
}