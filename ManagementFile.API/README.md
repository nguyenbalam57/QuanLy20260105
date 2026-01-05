# Hướng Dẫn Sử Dụng Management File API - User Authentication & CRUD

## Tổng Quan Hệ Thống

Hệ thống ManagementFile API đã được xây dựng hoàn chỉnh với các tính năng chính:

- **Authentication System**: Đăng nhập/đăng xuất với session token
- **User CRUD Operations**: Tạo, đọc, cập nhật, xóa user
- **Role-based Authorization**: Phân quyền theo role user
- **Audit Logging**: Ghi log tất cả hoạt động
- **Password Security**: Mã hóa password bằng BCrypt
- **Session Management**: Quản lý phiên đăng nhập

## Cấu Trúc Dự Án

```
ManagementFile.API/
├── Controllers/
│   └── UsersController.cs          # API endpoints cho User management
├── Data/
│   └── ManagementFileDbContext.cs  # Entity Framework DbContext
├── Middleware/
│   └── AuthMiddleware.cs           # Authentication & Audit middleware
├── Models/
│   └── UserDTOs.cs                 # Data Transfer Objects
├── Services/
│   └── DataSeederService.cs        # Service seed dữ liệu ban đầu
└── Program.cs                      # Application startup
```

## API Endpoints

### 1. Authentication Endpoints

#### Đăng Nhập
```http
POST /api/users/login
Content-Type: application/json

{
    "usernameOrEmail": "admin",
    "password": "Admin@123",
    "rememberMe": false
}
```

**Response thành công:**
```json
{
    "success": true,
    "message": "Đăng nhập thành công",
    "data": {
        "success": true,
        "message": "Đăng nhập thành công",
        "user": {
            "id": "user-id",
            "username": "admin",
            "email": "admin@managementfile.com",
            "fullName": "System Administrator",
            "role": 0,
            "department": 0,
            "isActive": true
        },
        "sessionToken": "session-token-here",
        "expiresAt": "2024-01-01T08:00:00Z"
    }
}
```

#### Đăng Xuất
```http
POST /api/users/logout
Content-Type: application/json

{
    "sessionToken": "your-session-token"
}
```

#### Lấy Thông Tin User Hiện Tại
```http
GET /api/users/me
Authorization: Bearer your-session-token
```

### 2. User CRUD Operations

#### Lấy Danh Sách Users (có phân trang và tìm kiếm)
```http
GET /api/users?searchTerm=admin&role=0&pageNumber=1&pageSize=20
Authorization: Bearer your-session-token
```

**Query Parameters:**
- `searchTerm`: Tìm theo tên, username, email
- `role`: Lọc theo role (0=Admin, 1=ProjectManager, 2=Staff, 3=Client)
- `department`: Lọc theo phòng ban
- `isActive`: Lọc theo trạng thái active
- `pageNumber`: Số trang (bắt đầu từ 1)
- `pageSize`: Số records mỗi trang (tối đa 100)
- `sortBy`: Sắp xếp theo field (username, email, fullName, createdAt)
- `sortDirection`: Hướng sắp xếp (asc, desc)

#### Lấy Thông Tin User Theo ID
```http
GET /api/users/{id}
Authorization: Bearer your-session-token
```

#### Tạo User Mới
```http
POST /api/users
Authorization: Bearer your-session-token
Content-Type: application/json

{
    "username": "newuser",
    "email": "newuser@example.com",
    "fullName": "New User",
    "password": "NewUser@123",
    "role": 2,
    "department": 3,
    "phoneNumber": "0123456789",
    "position": "Developer",
    "managerId": "manager-user-id",
    "timeZone": "UTC",
    "language": "vi-VN"
}
```

#### Cập Nhật User
```http
PUT /api/users/{id}
Authorization: Bearer your-session-token
Content-Type: application/json

{
    "email": "updated@example.com",
    "fullName": "Updated Name",
    "department": 2,
    "phoneNumber": "0987654321",
    "position": "Senior Developer",
    "managerId": "new-manager-id",
    "timeZone": "Asia/Ho_Chi_Minh",
    "language": "vi-VN",
    "avatar": "path/to/avatar.jpg"
}
```

#### Xóa User (Soft Delete)
```http
DELETE /api/users/{id}
Authorization: Bearer your-session-token
```

### 3. Password Management

#### Đổi Mật Khẩu
```http
POST /api/users/{id}/change-password
Authorization: Bearer your-session-token
Content-Type: application/json

{
    "currentPassword": "OldPassword@123",
    "newPassword": "NewPassword@123",
    "confirmPassword": "NewPassword@123"
}
```

#### Mở Khóa Tài Khoản
```http
POST /api/users/{id}/unlock
Authorization: Bearer your-session-token
```

## Enums & Constants

### UserRole
- `Admin = 0`: Quản trị viên hệ thống
- `ProjectManager = 1`: Quản lý dự án  
- `Staff = 2`: Nhân viên
- `Client = 3`: Khách hàng

### Department
- `PM = 0`: Project Management
- `SRA = 1`: System Requirements Analysis
- `SD = 2`: System Design
- `UDC = 3`: Unit Development & Construction
- `UT = 4`: Unit Test
- `ITST = 5`: Integration Test & System Test
- `STST = 6`: System Test & Stress Test
- `IM = 7`: Implementation
- `OTHER = 8`: Khác

## Tài Khoản Mặc Định

Hệ thống tự động tạo các tài khoản demo khi khởi động:

1. **Admin User**
   - Username: `admin`
   - Password: `Admin@123`
   - Role: Admin

2. **Project Manager**
   - Username: `manager` 
   - Password: `Manager@123`
   - Role: ProjectManager

3. **Developer**
   - Username: `developer`
   - Password: `Dev@123`
   - Role: Staff

4. **Tester**
   - Username: `tester`
   - Password: `Test@123`
   - Role: Staff

## Security Features

### Authentication
- Session-based authentication với token
- Token tự động expire (8 giờ mặc định, 30 ngày nếu "Remember Me")
- Tự động logout khi token hết hạn

### Password Security
- Mã hóa password bằng BCrypt với salt
- Validation độ mạnh password (tối thiểu 6 ký tự)
- Khóa tài khoản sau 5 lần đăng nhập sai (30 phút)

### Authorization
- Middleware tự động check authentication cho tất cả API endpoints
- Bỏ qua authentication cho public endpoints (/health, /swagger, /login, /logout)
- Support Authorization header hoặc query parameter ?token=

### Audit Logging
- Tự động log tất cả API calls
- Ghi nhận user, timestamp, IP, user agent
- Lưu trong database table AuditLogs

## Cách Chạy Hệ Thống

### 1. Chuẩn Bị Database
Cập nhật connection string trong `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ManagementFileDB;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

### 2. Chạy Migration
```bash
cd ManagementFile.API
dotnet ef database update
```

### 3. Khởi Động API
```bash
dotnet run
```

API sẽ chạy tại: http://localhost:5190

### 4. Truy Cập Swagger UI
Mở browser và vào: http://localhost:5190

## Cách Sử Dụng Trong Client Application

### 1. Đăng Nhập
```csharp
public async Task<LoginResponse> LoginAsync(string username, string password)
{
    var request = new LoginRequest 
    { 
        UsernameOrEmail = username, 
        Password = password 
    };
    
    var response = await httpClient.PostAsJsonAsync("/api/users/login", request);
    var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
    
    if (result.Success)
    {
        // Lưu session token để sử dụng cho các API call khác
        sessionToken = result.Data.SessionToken;
        return result.Data;
    }
    
    throw new Exception(result.Message);
}
```

### 2. Gọi API Có Authentication
```csharp
httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", sessionToken);

var response = await httpClient.GetAsync("/api/users");
var users = await response.Content.ReadFromJsonAsync<ApiResponse<UserListResponse>>();
```

### 3. Xử Lý Lỗi Authentication
```csharp
if (response.StatusCode == HttpStatusCode.Unauthorized)
{
    // Token hết hạn, yêu cầu user đăng nhập lại
    RedirectToLogin();
}
```

## Health Checks

- **General Health**: `GET /health`
- **Database Health**: `GET /health/database`

## Lưu Ý Quan Trọng

1. **Session Token**: Cần include trong header Authorization cho tất cả API calls
2. **Password Policy**: Tối thiểu 6 ký tự, nên có chữ hoa, chữ thường, số, ký tự đặc biệt
3. **Account Lockout**: Tài khoản sẽ bị khóa 30 phút sau 5 lần đăng nhập sai
4. **Soft Delete**: Users bị xóa vẫn tồn tại trong DB nhưng có flag IsDeleted = true
5. **Audit Trail**: Tất cả hoạt động đều được ghi log để audit

## Troubleshooting

### Lỗi Database Connection
- Kiểm tra connection string trong appsettings.json
- Đảm bảo SQL Server đang chạy
- Chạy `dotnet ef database update` để apply migration

### Lỗi 401 Unauthorized
- Kiểm tra session token có hợp lệ không
- Token có thể đã hết hạn, cần đăng nhập lại
- Kiểm tra format header: `Authorization: Bearer {token}`

### Lỗi Build
- Chạy `dotnet restore` để restore packages
- Kiểm tra .NET 8 SDK đã được install
- Clear bin/obj folders và rebuild

Hệ thống ManagementFile API đã sẵn sàng để sử dụng với đầy đủ tính năng User Management và Authentication!