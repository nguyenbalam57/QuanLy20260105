using ManagementFile.Models.AuditAndLogging;
using ManagementFile.Models.FileManagement;
using ManagementFile.Models.NotificationsAndCommunications;
using ManagementFile.Models.ProjectManagement;
using ManagementFile.Models.TimeTracking;
using ManagementFile.Models.UserManagement;
using ManagementFile.Models;
using Microsoft.EntityFrameworkCore;

namespace ManagementFile.API.Data
{
    /// <summary>
    /// ManagementFileDbContext - Context chính của ứng dụng quản lý file
    /// Chứa tất cả DbSets và cấu hình database cho hệ thống
    /// </summary>
    public class ManagementFileDbContext : DbContext
    {
        public ManagementFileDbContext(DbContextOptions<ManagementFileDbContext> options) : base(options)
        {
        }

        #region DbSets - Các bảng trong database

        // User Management
        public DbSet<User> Users { get; set; }
        public DbSet<ProjectMember> ProjectMembers { get; set; }

        // Project Management  
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectTask> ProjectTasks { get; set; }

        // File Management
        public DbSet<ProjectFile> ProjectFiles { get; set; }
        public DbSet<ProjectFolder> ProjectFolders { get; set; }
        public DbSet<FileVersion> FileVersions { get; set; }
        public DbSet<FileComment> FileComments { get; set; }
        public DbSet<FilePermission> FilePermissions { get; set; }
        public DbSet<Models.FileManagement.FileShare> FileShares { get; set; }
        public DbSet<FileShareAccess> FileShareAccesses { get; set; }

        // Notifications & Communications
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<TaskComment> TaskComments { get; set; }

        // Time Tracking
        public DbSet<TaskTimeLog> TaskTimeLogs { get; set; }

        // Audit & Logging
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }

        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình các relationship và constraints
            ConfigureUserManagement(modelBuilder);
            ConfigureProjectManagement(modelBuilder);
            ConfigureFileManagement(modelBuilder);
            ConfigureNotificationsAndCommunications(modelBuilder);
            ConfigureTimeTracking(modelBuilder);
            ConfigureAuditAndLogging(modelBuilder);
            ConfigureTaskComment(modelBuilder);

            // Cấu hình Global Query Filters cho Soft Delete
            ConfigureSoftDeleteFilters(modelBuilder);

            // Seed initial data
            SeedInitialData(modelBuilder);
        }

        #region Configuration Methods

        /// <summary>
        /// Cấu hình User Management entities
        /// </summary>
        private void ConfigureUserManagement(ModelBuilder modelBuilder)
        {
            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
                entity.Property(u => u.FullName).IsRequired().HasMaxLength(200);
            });

            // ProjectMember configuration
            modelBuilder.Entity<ProjectMember>(entity =>
            {
                entity.HasIndex(pm => new { pm.ProjectId, pm.UserId }).IsUnique();
                
                // Foreign key relationships với NoAction để tránh cascade conflicts
                entity.HasOne(pm => pm.Project)
                      .WithMany(p => p.ProjectMembers)
                      .HasForeignKey(pm => pm.ProjectId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(pm => pm.User)
                      .WithMany(u => u.ProjectMembers)
                      .HasForeignKey(pm => pm.UserId)
                      .OnDelete(DeleteBehavior.NoAction);
            });
        }

        /// <summary>
        /// Cấu hình Project Management entities
        /// </summary>
        private void ConfigureProjectManagement(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasIndex(p => p.ProjectCode).IsUnique();
                entity.Property(p => p.ProjectCode).IsRequired().HasMaxLength(50);
                entity.Property(p => p.ProjectName).IsRequired().HasMaxLength(200);
                
                // Configure decimal properties
                entity.Property(p => p.EstimatedBudget).HasColumnType("decimal(18,2)");
                entity.Property(p => p.ActualBudget).HasColumnType("decimal(18,2)");
                entity.Property(p => p.CompletionPercentage).HasColumnType("decimal(5,2)");
            });

            // ProjectTask configuration
            modelBuilder.Entity<ProjectTask>(entity =>
            {
                entity.HasIndex(pt => new { pt.ProjectId, pt.Status });
                entity.HasIndex(pt => new { pt.AssignedToId, pt.Status });
                entity.HasIndex(pt => new { pt.Priority, pt.DueDate });
                entity.HasIndex(pt => new { pt.ParentTaskId, pt.IsActive });

                entity.Property(pt => pt.Progress).HasColumnType("decimal(5,2)");

                // Configure relationships
                entity.HasOne(pt => pt.Project)
                      .WithMany(p => p.ProjectTasks)
                      .HasForeignKey(pt => pt.ProjectId)
                      .OnDelete(DeleteBehavior.Restrict);

                // ParentTaskId có thể null cho root tasks
                entity.HasOne(pt => pt.ParentTask)
                      .WithMany(pt => pt.SubTasks)
                      .HasForeignKey(pt => pt.ParentTaskId)
                      .IsRequired(false)  // Parent task là optional
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }

        /// <summary>
        /// Cấu hình File Management entities
        /// </summary>
        private void ConfigureFileManagement(ModelBuilder modelBuilder)
        {
            // ProjectFile configuration
            modelBuilder.Entity<ProjectFile>(entity =>
            {
                entity.HasIndex(pf => new { pf.ProjectId, pf.FileName });
                entity.HasIndex(pf => new { pf.FolderId, pf.IsActive });
                entity.HasIndex(pf => new { pf.FileType, pf.IsActive });

                entity.HasOne(pf => pf.Project)
                      .WithMany(p => p.ProjectFiles)
                      .HasForeignKey(pf => pf.ProjectId)
                      .OnDelete(DeleteBehavior.Restrict);

                // FolderId có thể null, sử dụng Restrict thay vì SetNull
                entity.HasOne(pf => pf.Folder)
                      .WithMany(pf => pf.ProjectFiles)
                      .HasForeignKey(pf => pf.FolderId)
                      .IsRequired(false)  // Đặt là optional
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ProjectFolder configuration
            modelBuilder.Entity<ProjectFolder>(entity =>
            {
                entity.HasIndex(pf => new { pf.ProjectId, pf.FolderName });
                entity.HasIndex(pf => new { pf.ParentFolderId, pf.IsActive });

                entity.HasOne(pf => pf.Project)
                      .WithMany(p => p.ProjectFolders)
                      .HasForeignKey(pf => pf.ProjectId)
                      .OnDelete(DeleteBehavior.Restrict);

                // ParentFolderId có thể null, sử dụng Restrict
                entity.HasOne(pf => pf.ParentFolder)
                      .WithMany(pf => pf.SubFolders)
                      .HasForeignKey(pf => pf.ParentFolderId)
                      .IsRequired(false)  // Đặt là optional
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // FileVersion configuration
            modelBuilder.Entity<FileVersion>(entity =>
            {
                entity.HasIndex(fv => new { fv.ProjectFileId, fv.VersionNumber });

                entity.HasOne(fv => fv.ProjectFile)
                      .WithMany(pf => pf.FileVersions)
                      .HasForeignKey(fv => fv.ProjectFileId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // FileComment configuration
            modelBuilder.Entity<FileComment>(entity =>
            {
                entity.HasIndex(fc => new { fc.FileVersionId, fc.CreatedAt });

                entity.HasOne(fc => fc.FileVersion)
                      .WithMany(fv => fv.FileComments)
                      .HasForeignKey(fc => fc.FileVersionId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(fc => fc.ParentComment)
                      .WithMany()
                      .HasForeignKey(fc => fc.ParentCommentId)
                      .IsRequired(false)  // Parent comment là optional
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // FilePermission configuration
            modelBuilder.Entity<FilePermission>(entity =>
            {
                entity.HasIndex(fp => new { fp.ProjectFileId, fp.UserId, fp.IsActive }).IsUnique();
                entity.HasIndex(fp => new { fp.ProjectFileId, fp.RoleName, fp.IsActive });

                entity.HasOne(fp => fp.ProjectFile)
                      .WithMany(pf => pf.FilePermissions)
                      .HasForeignKey(fp => fp.ProjectFileId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // FileShare configuration
            modelBuilder.Entity<ManagementFile.Models.FileManagement.FileShare>(entity =>
            {
                entity.HasIndex(fs => new { fs.ProjectFileId, fs.IsActive });
                entity.HasIndex(fs => fs.ShareToken).IsUnique();
                entity.HasIndex(fs => new { fs.SharedWithEmail, fs.IsActive });

                entity.HasOne(fs => fs.ProjectFile)
                      .WithMany(pf => pf.FileShares)
                      .HasForeignKey(fs => fs.ProjectFileId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // FileShareAccess configuration
            modelBuilder.Entity<FileShareAccess>(entity =>
            {
                entity.HasIndex(fsa => new { fsa.FileShareId, fsa.AccessedAt });

                entity.HasOne(fsa => fsa.FileShare)
                      .WithMany(fs => fs.ShareAccesses)
                      .HasForeignKey(fsa => fsa.FileShareId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureTaskComment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaskComment>(entity =>
            {
                // Table name
                entity.ToTable("TaskComments");

                // Primary Key
                entity.HasKey(e => e.Id);

                // Indexes (đã có trong model qua Attribute, nhưng có thể cấu hình thêm ở đây)
                entity.HasIndex(e => new { e.TaskId, e.CreatedAt });
                entity.HasIndex(e => new { e.TaskId, e.CommentStatus });
                entity.HasIndex(e => new { e.ReviewerId, e.CreatedAt });

                // Required fields
                entity.Property(e => e.TaskId)
                    .IsRequired();

                entity.Property(e => e.Content)
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                entity.Property(e => e.CommentType)
                    .IsRequired()
                    .HasConversion<string>(); // Store enum as string

                entity.Property(e => e.CommentStatus)
                    .IsRequired()
                    .HasConversion<string>(); // Store enum as string

                entity.Property(e => e.Priority)
                    .HasConversion<string>(); // Store enum as string

                // String length constraints
                entity.Property(e => e.IssueTitle)
                    .HasMaxLength(500);

                entity.Property(e => e.ResolutionCommitId)
                    .HasMaxLength(100);

                // Default values
                entity.Property(e => e.RelatedFiles)
                    .HasDefaultValue("[]");

                entity.Property(e => e.RelatedScreenshots)
                    .HasDefaultValue("[]");

                entity.Property(e => e.RelatedDocuments)
                    .HasDefaultValue("[]");

                entity.Property(e => e.Attachments)
                    .HasDefaultValue("[]");

                entity.Property(e => e.MentionedUsers)
                    .HasDefaultValue("[]");

                entity.Property(e => e.Tags)
                    .HasDefaultValue("[]");

                entity.Property(e => e.Metadata)
                    .HasDefaultValue("{}");

                entity.Property(e => e.IsBlocking)
                    .HasDefaultValue(false);

                entity.Property(e => e.RequiresDiscussion)
                    .HasDefaultValue(false);

                entity.Property(e => e.IsAgreed)
                    .HasDefaultValue(false);

                entity.Property(e => e.IsVerified)
                    .HasDefaultValue(false);

                entity.Property(e => e.EstimatedFixTime)
                    .HasDefaultValue(0)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.ActualFixTime)
                    .HasDefaultValue(0)
                    .HasColumnType("decimal(18,2)");

                // Relationships

                // TaskComment -> ProjectTask (Many-to-One)
                entity.HasOne(e => e.ProjectTask)
                    .WithMany()
                    .HasForeignKey(e => e.TaskId)
                    .OnDelete(DeleteBehavior.Restrict);

                // TaskComment -> ParentComment (Self-referencing, Many-to-One)
                entity.HasOne(e => e.ParentComment)
                    .WithMany(e => e.Replies)
                    .HasForeignKey(e => e.ParentCommentId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Soft Delete Query Filter
                entity.HasQueryFilter(e => !e.IsDeleted);
            });
        }

        /// <summary>
        /// Cấu hình Notifications & Communications entities
        /// </summary>
        private void ConfigureNotificationsAndCommunications(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });

                // Cấu hình relationship với User rõ ràng
                entity.HasOne(n => n.User)
                      .WithMany(u => u.Notifications)
                      .HasForeignKey(n => n.UserId)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<TaskComment>(entity =>
            {
                entity.HasIndex(tc => new { tc.TaskId, tc.CreatedAt });

                entity.HasOne(tc => tc.ProjectTask)
                      .WithMany(pt => pt.TaskComments)
                      .HasForeignKey(tc => tc.TaskId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(tc => tc.ParentComment)
                      .WithMany(tc => tc.Replies)
                      .HasForeignKey(tc => tc.ParentCommentId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }

        /// <summary>
        /// Cấu hình Time Tracking entities
        /// </summary>
        private void ConfigureTimeTracking(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaskTimeLog>(entity =>
            {
                entity.HasIndex(ttl => new { ttl.TaskId, ttl.UserId, ttl.StartTime });
                entity.Property(ttl => ttl.HourlyRate).HasColumnType("decimal(18,2)");

                entity.HasOne(ttl => ttl.ProjectTask)
                      .WithMany(pt => pt.TaskTimeLogs)
                      .HasForeignKey(ttl => ttl.TaskId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        /// <summary>
        /// Cấu hình Audit & Logging entities
        /// </summary>
        private void ConfigureAuditAndLogging(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasIndex(al => new { al.EntityType, al.EntityId, al.Action });
                entity.HasIndex(al => new { al.UserId, al.CreatedAt });
            });

            modelBuilder.Entity<UserSession>(entity =>
            {
                entity.HasIndex(us => new { us.UserId, us.IsActive });
                entity.HasIndex(us => us.SessionToken).IsUnique();

                // Cấu hình relationship với User
                entity.HasOne(us => us.User)
                      .WithMany(u => u.UserSessions)
                      .HasForeignKey(us => us.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        /// <summary>
        /// Cấu hình Global Query Filters cho Soft Delete
        /// </summary>
        private void ConfigureSoftDeleteFilters(ModelBuilder modelBuilder)
        {
            // Áp dụng filter cho tất cả entities kế thừa từ SoftDeletableEntity
            modelBuilder.Entity<ProjectFile>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<ProjectFolder>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<FileComment>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<FilePermission>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<ManagementFile.Models.FileManagement.FileShare>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Notification>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Project>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<ProjectTask>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<TaskComment>().HasQueryFilter(e => !e.IsDeleted);

            modelBuilder.Entity<UserSession>()
                .HasQueryFilter(us => !us.User.IsDeleted);

            modelBuilder.Entity<FileShareAccess>()
                .HasQueryFilter(fsa => !fsa.FileShare.IsDeleted);

            modelBuilder.Entity<FileVersion>()
                .HasQueryFilter(fv => !fv.ProjectFile.IsDeleted);

            modelBuilder.Entity<TaskTimeLog>()
                .HasQueryFilter(ttl => !ttl.ProjectTask.IsDeleted);

            modelBuilder.Entity<ProjectMember>()
                .HasQueryFilter(pm => !pm.Project.IsDeleted && !pm.User.IsDeleted);
        }

        /// <summary>
        /// Seed dữ liệu ban đầu
        /// </summary>
        private void SeedInitialData(ModelBuilder modelBuilder)
        {
            // Tạm thời bỏ seed data để tạo migration cơ bản
            // Sẽ add seed data sau bằng cách khác
            
            /*
            // Tạo Admin user mặc định với password hash thật
            var adminUserId = Guid.NewGuid().ToString();
            var adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123", BCrypt.Net.BCrypt.GenerateSalt(12));
            
            modelBuilder.Entity<User>().HasData(new User
            {
                Id = adminUserId,
                Username = "admin",
                Email = "admin@managementfile.com",
                FullName = "System Administrator",
                PasswordHash = adminPasswordHash,
                Role = ManagementFile.Models.Enums.UserRole.Admin,
                Department = ManagementFile.Models.Enums.Department.PM,
                IsActive = true,
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = adminUserId,
                Version = 1
            });

            // Tạo project mẫu
            var sampleProjectId = Guid.NewGuid().ToString();
            modelBuilder.Entity<Project>().HasData(new Project
            {
                Id = sampleProjectId,
                ProjectCode = "SAMPLE-001",
                ProjectName = "Sample Project",
                Description = "This is a sample project for demonstration purposes",
                Status = ManagementFile.Models.Enums.ProjectStatus.Planning,
                Priority = ManagementFile.Models.Enums.TaskPriority.Medium,
                ProjectManagerId = adminUserId,
                IsActive = true,
                IsPublic = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = adminUserId,
                Version = 1
            });

            // Gán admin làm project member
            modelBuilder.Entity<ProjectMember>().HasData(new ProjectMember
            {
                Id = Guid.NewGuid().ToString(),
                ProjectId = sampleProjectId,
                UserId = adminUserId,
                ProjectRole = "ProjectManager",
                JoinedAt = DateTime.UtcNow,
                IsActive = true,
                AllocationPercentage = 100,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = adminUserId,
                Version = 1
            });

            // Tạo user staff mẫu
            var staffUserId = Guid.NewGuid().ToString();
            var staffPasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff@123", BCrypt.Net.BCrypt.GenerateSalt(12));
            
            modelBuilder.Entity<User>().HasData(new User
            {
                Id = staffUserId,
                Username = "staff",
                Email = "staff@managementfile.com",
                FullName = "Staff User",
                PasswordHash = staffPasswordHash,
                Role = ManagementFile.Models.Enums.UserRole.Staff,
                Department = ManagementFile.Models.Enums.Department.UDC,
                IsActive = true,
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = adminUserId,
                Version = 1
            });

            // Thêm staff vào project
            modelBuilder.Entity<ProjectMember>().HasData(new ProjectMember
            {
                Id = Guid.NewGuid().ToString(),
                ProjectId = sampleProjectId,
                UserId = staffUserId,
                ProjectRole = "Developer",
                JoinedAt = DateTime.UtcNow,
                IsActive = true,
                AllocationPercentage = 80,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = adminUserId,
                Version = 1
            });
            */
        }

        #endregion

        #region Override Methods

        /// <summary>
        /// Override SaveChanges để tự động cập nhật audit fields
        /// </summary>
        public override int SaveChanges()
        {
            UpdateAuditFields();
            return base.SaveChanges();
        }

        /// <summary>
        /// Override SaveChangesAsync để tự động cập nhật audit fields
        /// </summary>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateAuditFields();
            return await base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Tự động cập nhật audit fields cho entities
        /// </summary>
        private void UpdateAuditFields()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is ManagementFile.Models.BaseModels.BaseEntity &&
                           (e.State == EntityState.Added || e.State == EntityState.Modified));

            var currentTime = DateTime.UtcNow;
            
            foreach (var entry in entries)
            {
                var entity = (ManagementFile.Models.BaseModels.BaseEntity)entry.Entity;

                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = currentTime;
                    // entity.CreatedBy sẽ được set từ application layer
                }
                else if (entry.State == EntityState.Modified)
                {
                    entity.UpdatedAt = currentTime;
                    // entity.UpdatedBy sẽ được set từ application layer
                    entity.Version++;
                }
            }
        }

        #endregion
    }
}