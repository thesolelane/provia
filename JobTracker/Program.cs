using Microsoft.EntityFrameworkCore;
using JobTracker.Data;
using JobTracker.Services;
using Microsoft.Extensions.FileProviders;
using System.IO;
using System.Text.Json.Serialization;

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

// Add security services
builder.Services.AddScoped<JobTracker.Security.ISecurityAuditService, JobTracker.Security.SecurityAuditService>();
builder.Services.AddScoped<JobTracker.Services.IEmailService, JobTracker.Services.EmailService>();
builder.Services.AddScoped<JobTracker.Services.SMSService>();
builder.Services.AddHttpClient();

// Add Authentication
builder.Services.AddAuthentication("Bearer")
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, SimpleAuthenticationHandler>(
        "Bearer", options => { });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure PostgreSQL connection
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
Console.WriteLine($"Database URL: {connectionString}");

// Configure database using environment variables directly
builder.Services.AddDbContext<JobTrackerContext>(options =>
{
    var pgHost = Environment.GetEnvironmentVariable("PGHOST");
    var pgPort = Environment.GetEnvironmentVariable("PGPORT");
    var pgDatabase = Environment.GetEnvironmentVariable("PGDATABASE");
    var pgUser = Environment.GetEnvironmentVariable("PGUSER");
    var pgPassword = Environment.GetEnvironmentVariable("PGPASSWORD");
    
    var connectionStr = $"Host={pgHost};Port={pgPort};Database={pgDatabase};Username={pgUser};Password={pgPassword};SSL Mode=Prefer;Trust Server Certificate=true;Connection Idle Lifetime=300;Command Timeout=60;Timeout=30";
    Console.WriteLine("Using environment variable configuration for database");
    
    options.UseNpgsql(connectionStr, npgsqlOptions => 
    {
        npgsqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
    });
});

// Add CORS for the frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Serve static files for the React app
app.UseStaticFiles();

// Create uploads directory if it doesn't exist
var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

app.UseCors("AllowReactApp");

app.UseAuthorization();

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

// Create database and tables on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<JobTrackerContext>();
        context.Database.EnsureCreated();
        Console.WriteLine("Database and tables created successfully!");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while creating the database.");
    }
}

app.Run();
