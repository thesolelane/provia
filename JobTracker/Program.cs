using Microsoft.EntityFrameworkCore;
using JobTracker.Data;
using JobTracker.Services;
using Microsoft.Extensions.FileProviders;
using System.IO;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// Enable legacy timestamp behavior for PostgreSQL
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Register services
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<InspectionTrackingService>();
builder.Services.AddScoped<JobTracker.Services.AI.AIAssistantService>();
builder.Services.AddScoped<JobTracker.Services.IEmailService, JobTracker.Services.EmailService>();
builder.Services.AddScoped<JobTracker.Services.GeoFencingService>();

builder.Services.AddScoped<JobTracker.Services.JobLocationService>();
builder.Services.AddScoped<JobTracker.Services.UserCodeService>();
builder.Services.AddScoped<JobTracker.Services.LocationTrackingService>();

// Add user deactivation service
builder.Services.AddScoped<JobTracker.Services.IUserDeactivationService, JobTracker.Services.UserDeactivationService>();

// Add security services
builder.Services.AddScoped<JobTracker.Security.ISecurityAuditService, JobTracker.Security.SecurityAuditService>();
builder.Services.AddScoped<JobTracker.Services.IEmailService, JobTracker.Services.EmailService>();
builder.Services.AddScoped<JobTracker.Services.SMSService>();
builder.Services.AddHttpClient();

// Register Database Seeder
builder.Services.AddScoped<DatabaseSeeder>();

// Add Tenant Context for multi-tenancy
builder.Services.AddScoped<JobTracker.Services.ITenantContext, JobTracker.Services.TenantContext>();

// Add Simple Authentication
builder.Services.AddAuthentication("Bearer")
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, SimpleAuthenticationHandler>(
        "Bearer", options => { });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure PostgreSQL connection
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
// SECURITY: Do not log database URL or credentials

// Configure database using environment variables directly
builder.Services.AddDbContext<JobTrackerContext>(options =>
{
    var pgHost = Environment.GetEnvironmentVariable("PGHOST");
    var pgPort = Environment.GetEnvironmentVariable("PGPORT");
    var pgDatabase = Environment.GetEnvironmentVariable("PGDATABASE");
    var pgUser = Environment.GetEnvironmentVariable("PGUSER");
    var pgPassword = Environment.GetEnvironmentVariable("PGPASSWORD");
    
    var connectionStr = $"Host={pgHost};Port={pgPort};Database={pgDatabase};Username={pgUser};Password={pgPassword};SSL Mode=Prefer;Trust Server Certificate=true;Connection Idle Lifetime=300;Command Timeout=60;Timeout=30";
    
    options.UseNpgsql(connectionStr, npgsqlOptions => 
    {
        npgsqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
    });
});

// Add CORS for the frontend - RESTRICTED
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", builder =>
    {
        builder.WithOrigins("http://localhost:3000", "http://localhost:5000", "http://0.0.0.0:5000")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

// Add rate limiting for brute force protection
builder.Services.AddMemoryCache();

// Register SMS Service for Twilio
builder.Services.AddHttpClient();
builder.Services.AddScoped<JobTracker.Services.IMessagingService, JobTracker.Services.SimpleSmsService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add rate limiting middleware
app.UseMiddleware<JobTracker.Middleware.RateLimitingMiddleware>();

// Disable caching for static HTML files to prevent preview cache issues
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;
    if (path != null && (path.EndsWith(".html") || path == "/"))
    {
        context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
        context.Response.Headers.Pragma = "no-cache";
        context.Response.Headers.Expires = "0";
    }
    await next();
});

// Serve static files for the React app
app.UseStaticFiles();

// Create uploads directory if it doesn't exist
var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

app.UseCors("AllowReactApp");

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Add tenant context middleware
app.UseMiddleware<JobTracker.Middleware.TenantContextMiddleware>();

app.MapControllers();

// Add simple test endpoint
app.MapGet("/api/test", () => new { Message = "Job Tracker API is working!", Timestamp = DateTime.UtcNow });

// Map specific routes that should not fallback to SPA
app.MapWhen(context => !context.Request.Path.StartsWithSegments("/api"), appBuilder =>
{
    appBuilder.UseRouting();
    appBuilder.UseEndpoints(endpoints =>
    {
        endpoints.MapFallbackToFile("index.html");
    });
});

// Create database and seed with initial data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<JobTrackerContext>();
        var seeder = services.GetRequiredService<DatabaseSeeder>();
        
        context.Database.EnsureCreated();
        await seeder.SeedAsync();
        
        Console.WriteLine("Database initialized and seeded successfully!");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

app.Run();
