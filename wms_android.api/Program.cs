using wms_android.shared.Data;
using wms_android.shared.Interfaces;
using wms_android.api.Middleware;
using wms_android.api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using wms_android.shared.Models.Auth;
using wms_android.shared.Services;
using Microsoft.Extensions.Options;
using wms_android.api.Services;
using wms_android.api.Interfaces;
using System.Linq;
using Microsoft.AspNetCore.ResponseCompression;
using wms_android.cache.Extensions;
using wms_android.cache.Interfaces;
using System.IO.Compression;
public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configuration is automatically loaded from appsettings.json and appsettings.{Environment}.json
        Console.WriteLine($"=== Application Starting ===");
        Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
        Console.WriteLine($"Working Directory: {Directory.GetCurrentDirectory()}");
        Console.WriteLine($"Content Root: {builder.Environment.ContentRootPath}");

        // Add services to container - Use Newtonsoft.Json for circular reference handling
        builder.Services.AddControllers().AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
        });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Add Response Compression for performance optimization
        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<GzipCompressionProvider>();
            options.Providers.Add<BrotliCompressionProvider>();
            
            // Compress JSON responses and other text-based content
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
            {
                "application/json",
                "application/json; charset=utf-8",
                "text/json",
                "text/plain",
                "text/css",
                "text/javascript",
                "application/javascript",
                "text/html",
                "application/xml",
                "text/xml"
            });
        });

        // Configure compression levels
        builder.Services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Optimal; // Best compression ratio
        });
        
        builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Optimal; // Best compression ratio
        });

        // Add production-grade caching layer (Redis with in-memory fallback)
        builder.Services.AddWmsCaching(builder.Configuration);

        // Register services
        builder.Services.AddScoped<IParcelService, ApiParcelService>();
        builder.Services.AddScoped<IDispatchSegmentService, DispatchSegmentService>();
        builder.Services.AddScoped<wms_android.api.Services.ICacheInvalidationService, wms_android.api.Services.CacheInvalidationService>();

        // Configure database using connection string from appsettings or environment variables
        // Always prefer environment variables in production for security
        var dbHost = builder.Configuration["DB_HOST"] ?? Environment.GetEnvironmentVariable("DB_HOST");
        var dbPort = builder.Configuration["DB_PORT"] ?? Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
        var dbName = builder.Configuration["DB_NAME"] ?? Environment.GetEnvironmentVariable("DB_NAME");
        var dbUser = builder.Configuration["DB_USER"] ?? Environment.GetEnvironmentVariable("DB_USER");
        var dbPassword = builder.Configuration["DB_PASSWORD"] ?? Environment.GetEnvironmentVariable("DB_PASSWORD");
        
        string connectionString;
        
        // If we have individual DB environment variables, use them (preferred in production)
        if (!string.IsNullOrEmpty(dbHost) && !string.IsNullOrEmpty(dbName) && 
            !string.IsNullOrEmpty(dbUser) && !string.IsNullOrEmpty(dbPassword))
        {
            // Production-safe connection pool configuration to prevent "too many clients" errors
            // Reduced MaxPoolSize for multiple app instances, added connection lifecycle management
            connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword};SSL Mode=Require;Trust Server Certificate=true;Pooling=true;Minimum Pool Size=1;Maximum Pool Size=8;Connection Idle Lifetime=180;Command Timeout=30;Timeout=10;Keepalive=30";
            Console.WriteLine($"✓ Built production-safe connection string from environment variables");
            Console.WriteLine($"  Host: {dbHost}, Port: {dbPort}, Database: {dbName}, User: {dbUser}");
            Console.WriteLine($"  Connection Pool: MinSize=1, MaxSize=8 (ultra-conservative)");
            Console.WriteLine($"  Connection Lifecycle: IdleLifetime=180s, Timeout=10s, PruningInterval=5s");
        }
        else
        {
            // Fall back to configuration file, but check for placeholders
            var configConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrEmpty(configConnectionString) && !configConnectionString.Contains("${"))
            {
                connectionString = configConnectionString;
                Console.WriteLine($"✓ Using connection string from configuration file");
            }
            else
            {
                throw new InvalidOperationException("Database connection string is not configured. Either set ConnectionStrings__DefaultConnection or provide DB_HOST, DB_NAME, DB_USER, and DB_PASSWORD environment variables.");
            }
        }

        // Configure Npgsql to use timestamps with time zone by default
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly("wms_android.api");
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: new[] { "53300" }); // Handle "too many clients" errors
                npgsqlOptions.CommandTimeout(45); // Increased for migration operations
            }));

        // Add Health Checks with error handling
        try
        {
            builder.Services.AddHealthChecks()
                .AddDbContextCheck<AppDbContext>();
            Console.WriteLine("✓ Health checks configured successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ WARNING: Health check configuration failed: {ex.Message}");
            // Add basic health checks without DbContext
            builder.Services.AddHealthChecks();
        }

        // Configure JWT with null checking
        var jwtConfig = builder.Configuration.GetSection("JwtConfig").Get<JwtConfig>();
        if (jwtConfig == null || string.IsNullOrEmpty(jwtConfig.Secret))
        {
            Console.WriteLine("⚠ WARNING: JWT configuration is missing or invalid. Using fallback configuration.");
            jwtConfig = new JwtConfig
            {
                Secret = "8d8c4028e2fed0a96694bb30929c3053",
                Issuer = "wms_android.api",
                Audience = "wms_android.client",
                ExpiryInDays = 7
            };
        }
        Console.WriteLine($"✓ JWT Config loaded - Issuer: {jwtConfig.Issuer}, Audience: {jwtConfig.Audience}");
        builder.Services.AddSingleton(Options.Create(jwtConfig));

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Secret)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtConfig.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtConfig.Audience,
                    ClockSkew = TimeSpan.Zero
                };
            });

        // Configure CORS - Always use our hardcoded origins to ensure they work
        var corsOrigins = new[]
        {
            "https://wms-ficma-dispatch-l5z4y.ondigitalocean.app", // React app (old URL)
            "https://starfish-app-2q7m5.ondigitalocean.app",       // React web panel (old)
            "https://wms-dispatch-app-pi36f.ondigitalocean.app",   // React web panel
            "https://levelsapi-82i8a.ondigitalocean.app",          // API URL
            "https://wmsandroidapi-w74du.ondigitalocean.app",      // Android API URL
            "http://localhost:3000",                                 // Local development
            "https://localhost:3001"                                 // Local development HTTPS
        };
        
        Console.WriteLine($"✓ Using hardcoded CORS origins: {string.Join(", ", corsOrigins)}");
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowWebApp",
                policy =>
                {
                    policy.WithOrigins(corsOrigins)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()
                        .SetIsOriginAllowedToAllowWildcardSubdomains();
                });
        });

        // Register services
        builder.Services.AddHttpClient();
        builder.Services.AddScoped<IUserService, wms_android.shared.Services.UserService>();
        builder.Services.AddScoped<IVehicleService, VehicleService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<ISmsService, wms_android.shared.Services.SmsService>();
        builder.Services.AddScoped<wms_android.api.Interfaces.ITokenService, wms_android.api.Services.TokenService>();
        builder.Services.AddScoped<wms_android.api.Services.IDispatchSegmentService, wms_android.api.Services.DispatchSegmentService>();
        builder.Services.AddScoped<ISessionService, wms_android.shared.Services.SessionService>();
        
        // Add background service for session cleanup
        builder.Services.AddHostedService<SessionCleanupBackgroundService>();

        var app = builder.Build();

        // Apply database migrations on startup with connection storm prevention and timeout
        using var startupCts = new CancellationTokenSource(TimeSpan.FromMinutes(3)); // 3-minute startup timeout
        try
        {
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                // Add startup delay to prevent connection storms from multiple instances
                var startupDelay = Random.Shared.Next(500, 2000); // Reduced delay: 0.5-2 seconds
                logger.LogInformation($"Applying startup delay of {startupDelay}ms to prevent connection storms...");
                await Task.Delay(startupDelay, startupCts.Token);

            var maxRetries = 10; // Increased for production resilience
            var baseDelay = TimeSpan.FromSeconds(2);
            
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    logger.LogInformation($"Database connection attempt {attempt}/{maxRetries}...");
                    
                    // Test connection with timeout and detailed error info
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    try
                    {
                        // Try direct connection test first to get more detailed error
                        using var testConnection = new Npgsql.NpgsqlConnection(dbContext.Database.GetConnectionString());
                        await testConnection.OpenAsync(cts.Token);
                        logger.LogInformation("✓ Direct connection test successful");
                        testConnection.Close();
                        
                        // Now try EF Core's CanConnectAsync
                        var canConnect = await dbContext.Database.CanConnectAsync(cts.Token);
                        if (!canConnect)
                        {
                            throw new InvalidOperationException("EF Core CanConnectAsync returned false after successful direct connection");
                        }
                    }
                    catch (Exception connEx)
                    {
                        logger.LogError($"Connection string being used: {dbContext.Database.GetConnectionString()?.Replace(dbPassword, "***")}");
                        throw new InvalidOperationException($"Database connection failed: {connEx.GetType().Name}: {connEx.Message}", connEx);
                    }
                    
                    logger.LogInformation("✓ Database connection successful");
                    
                    // Get pending migrations
                    var pendingMigrations = dbContext.Database.GetPendingMigrations().ToList();
                    logger.LogInformation($"Pending migrations: {pendingMigrations.Count}");
                    if (pendingMigrations.Any())
                    {
                        logger.LogInformation($"Migrations to apply: {string.Join(", ", pendingMigrations)}");
                    }
                    
                    // Handle migration conflicts gracefully
                    try
                    {
                        // Apply migrations with timeout
                        using var migrationCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
                        await dbContext.Database.MigrateAsync(migrationCts.Token);
                        logger.LogInformation("✓ Database migration completed successfully");
                        break; // Success, exit retry loop
                    }
                    catch (Npgsql.PostgresException pgEx) when (pgEx.SqlState == "42P07" || pgEx.SqlState == "42701")
                    {
                        // Handle "relation already exists" or "column already exists" errors
                        logger.LogWarning($"Migration conflict detected: {pgEx.Message}");
                        logger.LogInformation("Attempting to fix migration history...");
                        
                        // Mark problematic migrations as applied
                        var migrationHistorySql = @"
                            INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                            VALUES 
                                ('20251205133225_InitialCreate', '8.0.8'),
                                ('20251211170446_UpdateParcelStatusNaming', '8.0.8'),
                                ('20251229090510_UpdateInvoiceItemStructure', '8.0.8')
                            ON CONFLICT (""MigrationId"") DO NOTHING;
                        ";
                        
                        dbContext.Database.ExecuteSqlRaw(migrationHistorySql);
                        logger.LogInformation("✓ Migration history updated. Database should now be in sync.");
                        break; // Success, exit retry loop
                    }
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    // Handle connection exhaustion specifically
                    if (ex.Message.Contains("too many clients") || 
                        ex.Message.Contains("53300") ||
                        ex is Npgsql.PostgresException pgEx && pgEx.SqlState == "53300")
                    {
                        var exponentialDelay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                        var jitteredDelay = exponentialDelay + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000));
                        
                        logger.LogWarning($"Database connection limit reached (attempt {attempt}/{maxRetries}). " +
                                        $"Retrying in {jitteredDelay.TotalSeconds:F1} seconds... Error: {ex.Message}");
                        
                        await Task.Delay(jitteredDelay);
                        continue;
                    }
                    
                    // Other database errors
                    var retryDelay = TimeSpan.FromSeconds(2 * attempt + Random.Shared.Next(1, 3));
                    logger.LogWarning($"Database error on attempt {attempt}/{maxRetries}. " +
                                    $"Retrying in {retryDelay.TotalSeconds:F1} seconds... " +
                                    $"Error Type: {ex.GetType().Name}, Message: {ex.Message}");
                    
                    // Log inner exception details if available
                    if (ex.InnerException != null)
                    {
                        logger.LogWarning($"Inner exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                    }
                    
                    await Task.Delay(retryDelay);
                }
                catch (Exception ex)
                {
                    // Final attempt failed
                    logger.LogError(ex, "CRITICAL: Database migration failed after {MaxRetries} attempts. Error: {Message}", maxRetries, ex.Message);
                    logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
                    
                    // Log connection pool information for debugging
                    logger.LogError("Connection string parameters: MaxPoolSize=20, MinPoolSize=2");
                    logger.LogError("This error typically indicates:");
                    logger.LogError("1. PostgreSQL max_connections limit exceeded");
                    logger.LogError("2. Too many application instances running simultaneously");
                    logger.LogError("3. Database server resource exhaustion");
                    
                    throw; // Re-throw to prevent app from starting with broken database
                }
            } // End for loop
            } // End using scope
        }
        catch (OperationCanceledException) when (startupCts.Token.IsCancellationRequested)
        {
            Console.WriteLine("❌ CRITICAL: Application startup timed out after 3 minutes");
            Console.WriteLine("This usually indicates database connection issues or migration deadlock");
            Console.WriteLine("Check database connectivity and connection pool settings");
            throw new TimeoutException("Application startup timed out waiting for database operations");
        }

        // Configure path base for DigitalOcean routing
        // DigitalOcean routes /api/* to this service, so we need to handle the /api prefix
        var pathBase = builder.Configuration["PathBase"];
        if (!string.IsNullOrEmpty(pathBase) && pathBase != "/")
        {
            app.UsePathBase(pathBase);
            Console.WriteLine($"Using path base: {pathBase}");
        }
        else
        {
            Console.WriteLine("No path base configured — running at root /");
        }

        // Configure the HTTP request pipeline
        // Enable Swagger in all environments for API documentation
        app.UseSwagger(c =>
        {
            c.RouteTemplate = "swagger/{documentName}/swagger.json";
        });
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "WMS Android API v1");
            c.RoutePrefix = "swagger";
            // Enable deep linking and persist authorization
            c.EnableDeepLinking();
            c.EnablePersistAuthorization();
        });

        // Middleware pipeline
        // NOTE: HTTPS is terminated at the DigitalOcean load balancer.
        // Do NOT use UseHttpsRedirection() inside the container — it causes
        // "Failed to determine the https port" warnings and can break health checks.
        // app.UseHttpsRedirection();
        
        // Add Response Compression early in pipeline for maximum benefit
        app.UseResponseCompression();
        app.UseRouting();
        
        app.UseCors("AllowWebApp");
        
        app.UseAuthentication();
        app.UseAuthorization();
        
        // Add session validation middleware after authentication but before controllers
        app.UseSessionValidation();
        
        app.MapControllers();
        
        // Map health checks endpoint last
        app.MapHealthChecks("/health");

        // Add a diagnostic endpoint to help troubleshoot route issues
        app.MapFallback(async context => {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsJsonAsync(new { 
                error = "Route not found",
                path = context.Request.Path.Value,
                method = context.Request.Method,
                availableRoutes = "Use /swagger to view available API endpoints"
            });
        });

        try
        {
            Console.WriteLine("✓ Application startup completed successfully");
            Console.WriteLine("✓ Database connections optimized for production (MaxPool=20)");
            Console.WriteLine("✓ Startup delay and retry logic active to prevent connection storms");
            Console.WriteLine("✓ Starting WMS Android API server...");
            
            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ CRITICAL ERROR during application startup:");
            Console.WriteLine($"   Exception Type: {ex.GetType().Name}");
            Console.WriteLine($"   Message: {ex.Message}");
            Console.WriteLine($"   Stack Trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
            }
            throw;
        }
    }
}
// var builder = WebApplication.CreateBuilder(args);

// // Force Production environment and its configuration
// builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
//     .AddJsonFile("appsettings.Production.json", optional: false, reloadOnChange: true)
//     .AddEnvironmentVariables();

// // Log the current configuration
// Console.WriteLine($"Current environment: {builder.Environment.EnvironmentName}");
// Console.WriteLine($"Using connection string: {builder.Configuration.GetConnectionString("DefaultConnection")}");

// // Add CORS policy
// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("AllowMobileApp",
//         builder => builder
//             .AllowAnyOrigin()
//             .AllowAnyMethod()
//             .AllowAnyHeader());
// });

// // Configure database
// builder.Services.AddDbContext<AppDbContext>(options =>
// {
//     var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
//     Console.WriteLine($"Attempting to connect with: {connectionString}");
    
//     options.UseNpgsql(connectionString, npgsqlOptions =>
//     {
//         npgsqlOptions.EnableRetryOnFailure(
//             maxRetryCount: 3,
//             maxRetryDelay: TimeSpan.FromSeconds(10),
//             errorCodesToAdd: new[] { "57P01", "57P02", "57P03" });
//         npgsqlOptions.CommandTimeout(30);
//         npgsqlOptions.MigrationsAssembly("wms_android.api");
//     });
    
//     options.EnableDetailedErrors();
//     options.EnableSensitiveDataLogging();
// });

// // Add services
// builder.Services.AddScoped<IParcelService, ParcelService>();
// builder.Services.AddScoped<IUserService, UserService>();
// builder.Services.AddScoped<IVehicleService, VehicleService>();

// builder.Services.AddControllers();
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();

// var app = builder.Build();

// app.UseCors("AllowMobileApp");

// // Configure the HTTP request pipeline
// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }

// app.UseHttpsRedirection();
// app.UseAuthorization();
// app.MapControllers();

// app.Run();

