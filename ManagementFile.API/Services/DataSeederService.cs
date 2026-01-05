using ManagementFile.API.Data;
using ManagementFile.Models.UserManagement;
using ManagementFile.Contracts.Enums;
using Microsoft.EntityFrameworkCore;

namespace ManagementFile.API.Services
{
    /// <summary>
    /// Service để seed dữ liệu ban đầu
    /// </summary>
    public class DataSeederService
    {
        private readonly ManagementFileDbContext _context;
        private readonly ILogger<DataSeederService> _logger;

        public DataSeederService(ManagementFileDbContext context, ILogger<DataSeederService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Seed dữ liệu admin user nếu chưa có
        /// </summary>
        public async Task SeedAdminUserAsync()
        {
            try
            {
                // Kiểm tra đã có admin chưa
                var existingAdmin = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == "admin" || u.Role == UserRole.Admin);

                if (existingAdmin != null)
                {
                    _logger.LogInformation("Admin user already exists");
                    return;
                }

                // Tạo admin user
                var adminUser = new User
                {
                    Username = "admin",
                    Email = "",
                    FullName = "System Administrator",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123", BCrypt.Net.BCrypt.GenerateSalt(12)),
                    Role = UserRole.Admin,
                    Department = Department.PM,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = 0,
                    Version = 1
                };

                _context.Users.Add(adminUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Admin user created successfully. Username: admin, Password: Admin@123");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding admin user");
            }
        }

        /// <summary>
        /// Seed dữ liệu demo users
        /// </summary>
        public async Task SeedDemoUsersAsync()
        {
            try
            {
                // Tạo một vài user demo
                //var demoUsers = new List<User>
                //{
                //    new User
                //    {
                //        Username = "manager",
                //        Email = "manager@managementfile.com",
                //        FullName = "Project Manager",
                //        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager@123"),
                //        Role = UserRole.Manager,
                //        Department = Department.PM,
                //        IsActive = true,
                //        CreatedAt = DateTime.UtcNow,
                //        CreatedBy = 0,
                //        Version = 1
                //    },
                //    new User
                //    {
                //        Username = "developer",
                //        Email = "developer@managementfile.com",
                //        FullName = "Developer User",
                //        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Dev@123"),
                //        Role = UserRole.Staff,
                //        Department = Department.UDC,
                //        IsActive = true,
                //        CreatedAt = DateTime.UtcNow,
                //        CreatedBy = 0,
                //        Version = 1
                //    },
                //    new User
                //    {
                //        Username = "tester",
                //        Email = "tester@managementfile.com",
                //        FullName = "Tester User",
                //        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@123"),
                //        Role = UserRole.Staff,
                //        Department = Department.UT,
                //        IsActive = true,
                //        CreatedAt = DateTime.UtcNow,
                //        CreatedBy = 0,
                //        Version = 1
                //    }
                //};

                // Tạo một vài user demo
                var tcvUsers = new List<User>
                {
                    new User
                    {
                        Username = "TCV190001",
                        Email = "",
                        FullName = "Tô Nhật Nam",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("TCV@000000"),
                        Role = UserRole.Director,
                        Department = Department.OTHER,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = 0,
                        Version = 1
                    },
                    new User
                    {
                        Username = "TCV190002",
                        Email = "",
                        FullName = "Tanaka Hiroyuki",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("TCV@000000"),
                        Role = UserRole.Director,
                        Department = Department.OTHER,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = 0,
                        Version = 1
                    },
                    new User
                    {
                        Username = "TCV220025",
                        Email = "",
                        FullName = "Hoàng Minh Thắng",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("TCV@000000"),
                        Role = UserRole.Manager,
                        Department = Department.OTHER,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = 0,
                        Version = 1
                    },
                    new User
                    {
                        Username = "TCV220026",
                        Email = "",
                        FullName = "Cao Khánh Duy",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("TCV@000000"),
                        Role = UserRole.TeamLead,
                        Department = Department.OTHER,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = 0,
                        Version = 1
                    },
                    new User
                    {
                        Username = "TCV220027",
                        Email = "",
                        FullName = "Nguyễn Bá Lam",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("TCV@000000"),
                        Role = UserRole.Senior,
                        Department = Department.OTHER,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = 0,
                        Version = 1
                    },
                    new User
                    {
                        Username = "TCV230036",
                        Email = "",
                        FullName = "Huỳnh Thanh Tú",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("TCV@000000"),
                        Role = UserRole.Staff,
                        Department = Department.OTHER,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = 0,
                        Version = 1
                    },
                    new User
                    {
                        Username = "TCV240038",
                        Email = "",
                        FullName = "Phạm Thị Kim Thư",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("TCV@000000"),
                        Role = UserRole.Staff,
                        Department = Department.OTHER,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = 0,
                        Version = 1
                    },
                    new User
                    {
                        Username = "TCV240039",
                        Email = "",
                        FullName = "Nguyễn Thị Thùy Dương",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("TCV@000000"),
                        Role = UserRole.Staff,
                        Department = Department.OTHER,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = 0,
                        Version = 1
                    },
                    new User
                    {
                        Username = "TCV250042",
                        Email = "",
                        FullName ="Nguyễn Công Phúc",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("TCV@000000"),
                        Role = UserRole.Staff,
                        Department = Department.OTHER,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = 0,
                        Version = 1
                    },
                    new User
                    {
                        Username = "TCV250043",
                        Email = "",
                        FullName = "Nguyễn Văn Hiếu",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("TCV@000000"),
                        Role = UserRole.Staff,
                        Department = Department.OTHER,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = 0,
                        Version = 1
                    },
                    new User
                    {
                        Username = "TCV250044",
                        Email = "",
                        FullName = "Phan Minh Tâm",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("TCV@000000"),
                        Role = UserRole.Staff,
                        Department = Department.OTHER,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = 0,
                        Version = 1
                    },
                    new User
                    {
                        Username = "TCV250045",
                        Email = "",
                        FullName = "Lê Chí Tâm",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("TCV@000000"),
                        Role = UserRole.Staff,
                        Department = Department.OTHER,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = 0,
                        Version = 1
                    }
                };

                foreach (var user in tcvUsers)
                {
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Username == user.Username);

                    if (existingUser == null)
                    {
                        _context.Users.Add(user);
                        _logger.LogInformation("Created demo user: {Username}", user.Username);
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Demo users seeded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding demo users");
            }
        }

        /// <summary>
        /// Seed tất cả dữ liệu ban đầu
        /// </summary>
        public async Task SeedAllAsync()
        {
            await SeedAdminUserAsync();
            await SeedDemoUsersAsync();
        }
    }
}