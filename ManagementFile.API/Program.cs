using ManagementFile.API.Data;
using ManagementFile.API.Middleware;
using ManagementFile.API.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog;

// ========================================
// SETUP SERILOG LOGGING TRƯỚC KHI BUILD
// ========================================
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("C:\\Logs\\ManagerFileAPI\\log-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("=".PadRight(80, '='));
    Log.Information("MANAGEMENT FILE API - STARTING");
    Log.Information("=".PadRight(80, '='));

    var builder = WebApplication.CreateBuilder(args);

    // Thêm Serilog
    builder.Host.UseSerilog();

    // Thêm dòng này để App hiểu nó đang chạy như một Service
    builder.Host.UseWindowsService();

    // Log environment information
    Log.Information("Environment: {Environment}", builder.Environment.EnvironmentName);
    Log.Information("Content Root: {ContentRoot}", builder.Environment.ContentRootPath);
    Log.Information("Base Directory: {BaseDirectory}", AppContext.BaseDirectory);
    Log.Information("Current Directory: {CurrentDirectory}", Directory.GetCurrentDirectory());

    // Configure URLs to listen on specific IP
    builder.WebHost.UseUrls("http://0.0.0.0:5190", "http://localhost:5190");
    Log.Information("Configured URLs: http://0.0.0.0:5190, http://localhost:5190");

    // Add services to the container.
    builder.Services.AddControllers();

    // ========================================
    // LOG CONNECTION STRING
    // ========================================
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    Log.Information("=".PadRight(80, '='));
    Log.Information("DATABASE CONNECTION STRING:");
    Log.Information(connectionString ?? "NULL - CONNECTION STRING NOT FOUND!");
    Log.Information("=".PadRight(80, '='));

    if (string.IsNullOrEmpty(connectionString))
    {
        Log.Error("⚠️ CONNECTION STRING IS NULL OR EMPTY!  Check appsettings.json");
        throw new InvalidOperationException("Connection String 'DefaultConnection' not found in configuration!");
    }

    // Configure Entity Framework
    builder.Services.AddDbContext<ManagementFileDbContext>(options =>
    {
        options.UseSqlServer(connectionString);

        // Enable sensitive data logging in development
        if (builder.Environment.IsDevelopment())
        {
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        }

        // Log SQL queries
        options.LogTo(Log.Information, LogLevel.Information);
    });

    // Register services
    builder.Services.AddScoped<DataSeederService>();

    // Register File Management services
    builder.Services.AddScoped<IFilePermissionService, FilePermissionService>();
    builder.Services.AddScoped<IProjectFileService, ProjectFileService>();
    builder.Services.AddScoped<IProjectFolderService, ProjectFolderService>();
    builder.Services.AddScoped<IFileVersionService, FileVersionService>();
    builder.Services.AddScoped<IFileShareService, FileShareService>();
    builder.Services.AddScoped<IFileCommentService, FileCommentService>();

    // Register NEW core services
    builder.Services.AddScoped<ProjectService>();
    builder.Services.AddScoped<ProjectTaskService>();
    builder.Services.AddScoped<TimeTrackingService>();
    builder.Services.AddScoped<NotificationService>();
    builder.Services.AddScoped<AdminDashboardService>();

    // Register BaseDirectory Service - CENTRAL STORAGE MANAGEMENT
    builder.Services.Configure<BaseDirectoriesOptions>(
        builder.Configuration.GetSection("BaseDirectories"));
    builder.Services.AddSingleton<IBaseDirectoryService, BaseDirectoryService>();

    // Register Background Services
    builder.Services.AddHostedService<FileManagementBackgroundService>();

    // Configure file storage settings (legacy compatibility)
    builder.Services.Configure<FileStorageOptions>(
        builder.Configuration.GetSection("FileStorage"));

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "ManagementFile API",
            Version = "v1",
            Description = "API cho hệ thống quản lý file dự án",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "Development Team",
                Email = "dev@managementfile.com"
            }
        });

        // Include XML comments if available
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }

        // Add Authorization header support
        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Description = "Session token authorization header.  Enter your session token in the text input below.",
            Name = "Authorization",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi. Models.OpenApiReference
                    {
                        Type = Microsoft. OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                new string[] {}
            }
        });
    });

    // Add CORS - Updated for specific IP
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(corsBuilder =>
        {
            corsBuilder.WithOrigins(
                    "http://192.168.249.8:5190",
                    "http://localhost:5190",
                    "http://localhost:3000",
                    "http://192.168.249.8:3000"
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
    });

    var app = builder.Build();

    // ========================================
    // TEST DATABASE CONNECTION
    // ========================================
    Log.Information("=".PadRight(80, '='));
    Log.Information("TESTING DATABASE CONNECTION");
    Log.Information("=".PadRight(80, '='));

    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ManagementFileDbContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<DataSeederService>();

        try
        {
            Log.Information("Attempting to connect to database...");

            // Test connection
            var canConnect = await context.Database.CanConnectAsync();

            if (canConnect)
            {
                Log.Information("✓ Database connection SUCCESSFUL");

                // Get database info
                var dbConnection = context.Database.GetDbConnection();
                Log.Information("✓ Database Name: {DatabaseName}", dbConnection.Database);
                Log.Information("✓ Server: {ServerName}", dbConnection.DataSource);

                // Run migrations
                Log.Information("Running database migrations...");
                await context.Database.MigrateAsync();
                Log.Information("✓ Migrations completed successfully");

                // Seed data
                Log.Information("Seeding initial data...");
                await seeder.SeedAllAsync();
                Log.Information("✓ Data seeding completed");

                // Count tables
                var tableCount = await context.Database.ExecuteSqlRawAsync(
                    "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'");
                Log.Information("✓ Database has tables configured");
            }
            else
            {
                Log.Error("✗ Database connection FAILED - CanConnect returned false");
                Log.Error("This usually means:");
                Log.Error("  1. SQL Server is not running");
                Log.Error("  2. Server name is incorrect");
                Log.Error("  3. Database doesn't exist and cannot be created");
                Log.Error("  4. Permission issues");
            }
        }
        catch (SqlException sqlEx)
        {
            Log.Error("✗ SQL SERVER ERROR:");
            Log.Error("  Error Number: {ErrorNumber}", sqlEx.Number);
            Log.Error("  Error Message: {Message}", sqlEx.Message);
            Log.Error("  Server: {Server}", sqlEx.Server);
            Log.Error("  Procedure: {Procedure}", sqlEx.Procedure);

            // Common SQL errors
            switch (sqlEx.Number)
            {
                case 2:
                case 53:
                    Log.Error("→ Cannot connect to SQL Server.  Check if SQL Server service is running:");
                    Log.Error("   net start MSSQL$SQLEXPRESS");
                    break;
                case 4060:
                    Log.Error("→ Cannot open database. Database may not exist.");
                    break;
                case 18456:
                    Log.Error("→ Login failed. Check authentication settings.");
                    break;
                case 1225:
                    Log.Error("→ Database is in use. Try restarting SQL Server service.");
                    break;
            }

            // Don't throw - let app start so we can see logs
        }
        catch (Exception ex)
        {
            Log.Error(ex, "✗ UNEXPECTED ERROR during database initialization:");
            Log.Error("  Type: {ExceptionType}", ex.GetType().Name);
            Log.Error("  Message: {Message}", ex.Message);

            if (ex.InnerException != null)
            {
                Log.Error("  Inner Exception: {InnerMessage}", ex.InnerException.Message);
            }
        }
    }

    Log.Information("=".PadRight(80, '='));

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "ManagementFile API v1");
            options.RoutePrefix = string.Empty;
            options.EnableTryItOutByDefault();
        });

        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler("/Error");
        // Removed HSTS since we're using HTTP only
    }

    // REMOVED: app.UseHttpsRedirection(); - Since we're only using HTTP

    app.UseCors();

    // Add custom middleware for request logging
    app.Use(async (context, next) =>
    {
        Log.Debug("Request: {Method} {Path} from {IP}",
            context.Request.Method,
            context.Request.Path,
            context.Connection.RemoteIpAddress);

        await next();
    });

    // Add custom authentication middleware
    app.UseCustomAuthentication();

    // Add audit logging middleware
    app.UseAuditLogging();

    app.UseAuthorization();

    app.MapControllers();

    // Add health check endpoint
    app.MapGet("/health", () =>
    {
        Log.Debug("Health check requested");

        return Results.Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Environment = app.Environment.EnvironmentName,
            Version = "1.0.0"
        });
    });

    // Add database health check
    app.MapGet("/health/database", async (ManagementFileDbContext context) =>
    {
        try
        {
            var canConnect = await context.Database.CanConnectAsync();

            if (canConnect)
            {
                var dbConnection = context.Database.GetDbConnection();
                return Results.Ok(new
                {
                    Status = "Database Connected",
                    Database = dbConnection.Database,
                    Server = dbConnection.DataSource,
                    Timestamp = DateTime.UtcNow
                });
            }
            else
            {
                return Results.Json(new
                {
                    Status = "Database Connection Failed",
                    Timestamp = DateTime.UtcNow
                }, statusCode: 500);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Database health check failed");
            return Results.Json(new
            {
                Status = "Database Error",
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            }, statusCode: 500);
        }
    });

    Log.Information("=".PadRight(80, '='));
    Log.Information("APPLICATION STARTED SUCCESSFULLY");
    Log.Information("Listening on: http://0.0.0.0:5190 and http://localhost:5190");
    Log.Information("=".PadRight(80, '='));

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "APPLICATION START-UP FAILED");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Configuration class for file storage options
public class FileStorageOptions
{
    public string BasePath { get; set; } = "uploads";
    public long MaxFileSize { get; set; } = 10 * 1024 * 1024;
    public string[] AllowedExtensions { get; set; } = new[] { ".jpg", ".jpeg", ".png", ".pdf", ". docx", ".xlsx" };
    public string ThumbnailPath { get; set; } = "thumbnails";
    public string PreviewPath { get; set; } = "previews";
    public bool EnableThumbnailGeneration { get; set; } = true;
    public bool EnablePreviewGeneration { get; set; } = true;
    public bool VirusScanEnabled { get; set; } = false;
    public bool BackupEnabled { get; set; } = true;
    public string BackupPath { get; set; } = "backup";
    public int RetentionDays { get; set; } = 365;
}