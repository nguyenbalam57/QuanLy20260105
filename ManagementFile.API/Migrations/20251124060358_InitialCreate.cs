using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManagementFile.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Changes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IPAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SessionId = table.Column<int>(type: "int", maxLength: 450, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectParentId = table.Column<int>(type: "int", nullable: true),
                    ProjectCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProjectName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    ClientName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProjectManagerId = table.Column<int>(type: "int", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlannedEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstimatedBudget = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ActualBudget = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EstimatedHours = table.Column<int>(type: "int", nullable: false),
                    ActualHours = table.Column<int>(type: "int", nullable: false),
                    CompletionPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CustomProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    VersionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsCurrentVersion = table.Column<bool>(type: "bit", nullable: false),
                    ParentVersionId = table.Column<int>(type: "int", nullable: false),
                    MasterEntityId = table.Column<int>(type: "int", nullable: false),
                    VersionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    LockedBy = table.Column<int>(type: "int", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: false),
                    DeleteReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_Projects_ProjectParentId",
                        column: x => x.ProjectParentId,
                        principalTable: "Projects",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Salt = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Department = table.Column<int>(type: "int", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Position = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ManagerId = table.Column<int>(type: "int", nullable: false),
                    Avatar = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLoginIP = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    LoginFailureCount = table.Column<int>(type: "int", nullable: false),
                    LockedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    VersionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsCurrentVersion = table.Column<bool>(type: "bit", nullable: false),
                    ParentVersionId = table.Column<int>(type: "int", nullable: false),
                    MasterEntityId = table.Column<int>(type: "int", nullable: false),
                    VersionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    LockedBy = table.Column<int>(type: "int", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: false),
                    DeleteReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActivatedBy = table.Column<int>(type: "int", nullable: false),
                    DeactivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeactivatedBy = table.Column<int>(type: "int", nullable: false),
                    DeactivateReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IPAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    ChangeNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectFolders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    ParentFolderId = table.Column<int>(type: "int", nullable: false),
                    FolderName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FolderPath = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    FolderLevel = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    IsReadOnly = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IconName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CustomProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    VersionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsCurrentVersion = table.Column<bool>(type: "bit", nullable: false),
                    ParentVersionId = table.Column<int>(type: "int", nullable: false),
                    MasterEntityId = table.Column<int>(type: "int", nullable: false),
                    VersionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    LockedBy = table.Column<int>(type: "int", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: false),
                    DeleteReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectFolders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectFolders_ProjectFolders_ParentFolderId",
                        column: x => x.ParentFolderId,
                        principalTable: "ProjectFolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectFolders_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    ParentTaskId = table.Column<int>(type: "int", nullable: true),
                    TaskCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    AssignedToId = table.Column<int>(type: "int", nullable: true),
                    AssignedToIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReporterId = table.Column<int>(type: "int", nullable: true),
                    EstimatedHours = table.Column<int>(type: "int", nullable: false),
                    ActualHours = table.Column<int>(type: "int", nullable: false),
                    Progress = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedBy = table.Column<int>(type: "int", nullable: true),
                    IsBlocked = table.Column<bool>(type: "bit", nullable: false),
                    BlockReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TaskType = table.Column<int>(type: "int", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    VersionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsCurrentVersion = table.Column<bool>(type: "bit", nullable: false),
                    ParentVersionId = table.Column<int>(type: "int", nullable: false),
                    MasterEntityId = table.Column<int>(type: "int", nullable: false),
                    VersionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    LockedBy = table.Column<int>(type: "int", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: false),
                    DeleteReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectTasks_ProjectTasks_ParentTaskId",
                        column: x => x.ParentTaskId,
                        principalTable: "ProjectTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectTasks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RelatedEntityId = table.Column<int>(type: "int", nullable: true),
                    ActionUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    VersionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsCurrentVersion = table.Column<bool>(type: "bit", nullable: false),
                    ParentVersionId = table.Column<int>(type: "int", nullable: false),
                    MasterEntityId = table.Column<int>(type: "int", nullable: false),
                    VersionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    LockedBy = table.Column<int>(type: "int", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: false),
                    DeleteReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProjectMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ProjectRole = table.Column<int>(type: "int", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LeftAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AllocationPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    HourlyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectMembers_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    SessionToken = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    LoginAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LogoutAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DeactivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeactivationReason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IPAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DeviceInfo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActivityCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    FolderId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FileExtension = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CurrentFileSize = table.Column<long>(type: "bigint", nullable: false),
                    CurrentFileHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RelativePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    IsReadOnly = table.Column<bool>(type: "bit", nullable: false),
                    RequireApproval = table.Column<bool>(type: "bit", nullable: false),
                    ApprovalStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ApprovedBy = table.Column<int>(type: "int", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastAccessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastAccessedBy = table.Column<int>(type: "int", nullable: false),
                    DownloadCount = table.Column<int>(type: "int", nullable: false),
                    ViewCount = table.Column<int>(type: "int", nullable: false),
                    ShareCount = table.Column<int>(type: "int", nullable: false),
                    CheckoutBy = table.Column<int>(type: "int", nullable: false),
                    CheckoutAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpectedCheckinAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AutoCheckinHours = table.Column<int>(type: "int", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CustomProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ThumbnailPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PreviewPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    VersionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsCurrentVersion = table.Column<bool>(type: "bit", nullable: false),
                    ParentVersionId = table.Column<int>(type: "int", nullable: false),
                    MasterEntityId = table.Column<int>(type: "int", nullable: false),
                    VersionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    LockedBy = table.Column<int>(type: "int", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: false),
                    DeleteReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectFiles_ProjectFolders_FolderId",
                        column: x => x.FolderId,
                        principalTable: "ProjectFolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectFiles_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaskComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParentCommentId = table.Column<int>(type: "int", nullable: true),
                    CommentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CommentStatus = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReviewerId = table.Column<int>(type: "int", nullable: true),
                    AssignedToId = table.Column<int>(type: "int", nullable: true),
                    IssueTitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SuggestedFix = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RelatedModule = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RelatedFiles = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "[]"),
                    RelatedScreenshots = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "[]"),
                    RelatedDocuments = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "[]"),
                    Attachments = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "[]"),
                    MentionedUsers = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "[]"),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedBy = table.Column<int>(type: "int", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResolutionCommitId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedBy = table.Column<int>(type: "int", nullable: true),
                    VerificationNotes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    EstimatedFixTime = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    ActualFixTime = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsBlocking = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RequiresDiscussion = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsAgreed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AgreedBy = table.Column<int>(type: "int", nullable: true),
                    AgreedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "[]"),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "{}"),
                    IsSystemComment = table.Column<bool>(type: "bit", nullable: false),
                    ProjectTaskId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    VersionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsCurrentVersion = table.Column<bool>(type: "bit", nullable: false),
                    ParentVersionId = table.Column<int>(type: "int", nullable: false),
                    MasterEntityId = table.Column<int>(type: "int", nullable: false),
                    VersionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    LockedBy = table.Column<int>(type: "int", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: false),
                    DeleteReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskComments_ProjectTasks_ProjectTaskId",
                        column: x => x.ProjectTaskId,
                        principalTable: "ProjectTasks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TaskComments_ProjectTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "ProjectTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskComments_TaskComments_ParentCommentId",
                        column: x => x.ParentCommentId,
                        principalTable: "TaskComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaskTimeLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsBillable = table.Column<bool>(type: "bit", nullable: false),
                    HourlyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskTimeLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskTimeLogs_ProjectTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "ProjectTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FilePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectFileId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PermissionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CanRead = table.Column<bool>(type: "bit", nullable: false),
                    CanWrite = table.Column<bool>(type: "bit", nullable: false),
                    CanDelete = table.Column<bool>(type: "bit", nullable: false),
                    CanShare = table.Column<bool>(type: "bit", nullable: false),
                    CanManagePermissions = table.Column<bool>(type: "bit", nullable: false),
                    CanDownload = table.Column<bool>(type: "bit", nullable: false),
                    CanPrint = table.Column<bool>(type: "bit", nullable: false),
                    CanComment = table.Column<bool>(type: "bit", nullable: false),
                    CanCheckout = table.Column<bool>(type: "bit", nullable: false),
                    CanApprove = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GrantedBy = table.Column<int>(type: "int", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedBy = table.Column<int>(type: "int", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    VersionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsCurrentVersion = table.Column<bool>(type: "bit", nullable: false),
                    ParentVersionId = table.Column<int>(type: "int", nullable: false),
                    MasterEntityId = table.Column<int>(type: "int", nullable: false),
                    VersionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    LockedBy = table.Column<int>(type: "int", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: false),
                    DeleteReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FilePermissions_ProjectFiles_ProjectFileId",
                        column: x => x.ProjectFileId,
                        principalTable: "ProjectFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileShares",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectFileId = table.Column<int>(type: "int", nullable: false),
                    ShareToken = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ShareType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SharedWithUserId = table.Column<int>(type: "int", nullable: false),
                    SharedWithEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SharedWithName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ShareTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ShareMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RequirePassword = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AllowDownload = table.Column<bool>(type: "bit", nullable: false),
                    AllowPreview = table.Column<bool>(type: "bit", nullable: false),
                    AllowComment = table.Column<bool>(type: "bit", nullable: false),
                    AllowPrint = table.Column<bool>(type: "bit", nullable: false),
                    TrackAccess = table.Column<bool>(type: "bit", nullable: false),
                    MaxDownloads = table.Column<int>(type: "int", nullable: false),
                    CurrentDownloads = table.Column<int>(type: "int", nullable: false),
                    MaxViews = table.Column<int>(type: "int", nullable: false),
                    CurrentViews = table.Column<int>(type: "int", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastAccessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastAccessedIP = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    LastAccessedUserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NotifyOnAccess = table.Column<bool>(type: "bit", nullable: false),
                    NotifyOnDownload = table.Column<bool>(type: "bit", nullable: false),
                    ShareUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    QRCodeData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CustomProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    VersionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsCurrentVersion = table.Column<bool>(type: "bit", nullable: false),
                    ParentVersionId = table.Column<int>(type: "int", nullable: false),
                    MasterEntityId = table.Column<int>(type: "int", nullable: false),
                    VersionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    LockedBy = table.Column<int>(type: "int", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: false),
                    DeleteReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileShares_ProjectFiles_ProjectFileId",
                        column: x => x.ProjectFileId,
                        principalTable: "ProjectFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectFileId = table.Column<int>(type: "int", nullable: false),
                    ChangeType = table.Column<int>(type: "int", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    FileHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PhysicalPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DiffFromPrevious = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VersionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsCurrentVersion = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    VersionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ParentVersionId = table.Column<int>(type: "int", nullable: false),
                    MasterEntityId = table.Column<int>(type: "int", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    LockedBy = table.Column<int>(type: "int", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: false),
                    DeleteReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileVersions_ProjectFiles_ProjectFileId",
                        column: x => x.ProjectFileId,
                        principalTable: "ProjectFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileShareAccesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileShareId = table.Column<int>(type: "int", maxLength: 450, nullable: false),
                    AccessType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AccessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IPAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AccessedBy = table.Column<int>(type: "int", nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    IsSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileShareAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileShareAccesses_FileShares_FileShareId",
                        column: x => x.FileShareId,
                        principalTable: "FileShares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileVersionId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: true),
                    StartColumn = table.Column<int>(type: "int", nullable: true),
                    EndColumn = table.Column<int>(type: "int", nullable: true),
                    CommentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ParentCommentId = table.Column<int>(type: "int", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    ResolvedBy = table.Column<int>(type: "int", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FileCommentId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    VersionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsCurrentVersion = table.Column<bool>(type: "bit", nullable: false),
                    ParentVersionId = table.Column<int>(type: "int", nullable: false),
                    MasterEntityId = table.Column<int>(type: "int", nullable: false),
                    VersionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    LockedBy = table.Column<int>(type: "int", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: false),
                    DeleteReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActivatedBy = table.Column<int>(type: "int", nullable: false),
                    DeactivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeactivatedBy = table.Column<int>(type: "int", nullable: false),
                    DeactivateReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileComments_FileComments_FileCommentId",
                        column: x => x.FileCommentId,
                        principalTable: "FileComments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FileComments_FileComments_ParentCommentId",
                        column: x => x.ParentCommentId,
                        principalTable: "FileComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FileComments_FileVersions_FileVersionId",
                        column: x => x.FileVersionId,
                        principalTable: "FileVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId_Action",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId", "Action" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FileComments_FileCommentId",
                table: "FileComments",
                column: "FileCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_FileComments_FileVersionId_CreatedAt",
                table: "FileComments",
                columns: new[] { "FileVersionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FileComments_ParentCommentId",
                table: "FileComments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_FilePermissions_ProjectFileId_RoleName_IsActive",
                table: "FilePermissions",
                columns: new[] { "ProjectFileId", "RoleName", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_FilePermissions_ProjectFileId_UserId_IsActive",
                table: "FilePermissions",
                columns: new[] { "ProjectFileId", "UserId", "IsActive" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileShareAccesses_FileShareId_AccessedAt",
                table: "FileShareAccesses",
                columns: new[] { "FileShareId", "AccessedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FileShares_ProjectFileId_IsActive",
                table: "FileShares",
                columns: new[] { "ProjectFileId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_FileShares_SharedWithEmail_IsActive",
                table: "FileShares",
                columns: new[] { "SharedWithEmail", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_FileShares_ShareToken",
                table: "FileShares",
                column: "ShareToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileVersions_ProjectFileId_VersionNumber",
                table: "FileVersions",
                columns: new[] { "ProjectFileId", "VersionNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFiles_FileType_IsActive",
                table: "ProjectFiles",
                columns: new[] { "FileType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFiles_FolderId_IsActive",
                table: "ProjectFiles",
                columns: new[] { "FolderId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFiles_ProjectId_FileName",
                table: "ProjectFiles",
                columns: new[] { "ProjectId", "FileName" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFolders_ParentFolderId_IsActive",
                table: "ProjectFolders",
                columns: new[] { "ParentFolderId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFolders_ProjectId_FolderName",
                table: "ProjectFolders",
                columns: new[] { "ProjectId", "FolderName" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_ProjectId_UserId",
                table: "ProjectMembers",
                columns: new[] { "ProjectId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_UserId",
                table: "ProjectMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ClientId_IsActive",
                table: "Projects",
                columns: new[] { "ClientId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProjectCode",
                table: "Projects",
                column: "ProjectCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProjectParentId",
                table: "Projects",
                column: "ProjectParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Status_IsActive",
                table: "Projects",
                columns: new[] { "Status", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_AssignedToId_Status",
                table: "ProjectTasks",
                columns: new[] { "AssignedToId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_ParentTaskId_IsActive",
                table: "ProjectTasks",
                columns: new[] { "ParentTaskId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_Priority_DueDate",
                table: "ProjectTasks",
                columns: new[] { "Priority", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_ProjectId_Status",
                table: "ProjectTasks",
                columns: new[] { "ProjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskComments_ParentCommentId",
                table: "TaskComments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskComments_ProjectTaskId",
                table: "TaskComments",
                column: "ProjectTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskComments_ReviewerId_CreatedAt",
                table: "TaskComments",
                columns: new[] { "ReviewerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskComments_TaskId_CommentStatus",
                table: "TaskComments",
                columns: new[] { "TaskId", "CommentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskComments_TaskId_CreatedAt",
                table: "TaskComments",
                columns: new[] { "TaskId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskTimeLogs_TaskId_UserId_StartTime",
                table: "TaskTimeLogs",
                columns: new[] { "TaskId", "UserId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_SessionToken",
                table: "UserSessions",
                column: "SessionToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId_IsActive",
                table: "UserSessions",
                columns: new[] { "UserId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "FileComments");

            migrationBuilder.DropTable(
                name: "FilePermissions");

            migrationBuilder.DropTable(
                name: "FileShareAccesses");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "ProjectMembers");

            migrationBuilder.DropTable(
                name: "TaskComments");

            migrationBuilder.DropTable(
                name: "TaskTimeLogs");

            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DropTable(
                name: "FileVersions");

            migrationBuilder.DropTable(
                name: "FileShares");

            migrationBuilder.DropTable(
                name: "ProjectTasks");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "ProjectFiles");

            migrationBuilder.DropTable(
                name: "ProjectFolders");

            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}
