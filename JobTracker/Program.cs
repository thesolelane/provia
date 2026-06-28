using Microsoft.EntityFrameworkCore;
using JobTracker.Data;
using JobTracker.Services;
using Microsoft.Extensions.FileProviders;
using System.IO;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.HttpOverrides;

// Enable legacy timestamp behavior for PostgreSQL
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
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

// Add JWT Token Service
builder.Services.AddScoped<JobTracker.Services.IJwtTokenService, JobTracker.Services.JwtTokenService>();

// Add Sub-Contractor Service for multi-company support
builder.Services.AddScoped<JobTracker.Services.ISubContractorService, JobTracker.Services.SubContractorService>();

// Add Mobile Sync Service
builder.Services.AddScoped<JobTracker.Services.IMobileSyncService, JobTracker.Services.MobileSyncService>();

// Add Client Info Service for role-based data access
builder.Services.AddScoped<JobTracker.Services.IClientInfoService, JobTracker.Services.ClientInfoService>();

// Add Job Bid Service for sub-contractor bidding
builder.Services.AddScoped<JobTracker.Services.IJobBidService, JobTracker.Services.JobBidService>();

// Add Inspection Service for multi-stage inspections
builder.Services.AddScoped<JobTracker.Services.IInspectionService, JobTracker.Services.InspectionService>();

// Add Code Engine Service for scope-based permit/inspection generation
builder.Services.AddScoped<JobTracker.Services.ICodeEngineService, JobTracker.Services.CodeEngineService>();

// Add MassGIS and Permit Document Services for PDF auto-fill
builder.Services.AddScoped<JobTracker.Services.IMassGISService, JobTracker.Services.MassGISService>();
builder.Services.AddScoped<JobTracker.Services.IPermitDocumentService, JobTracker.Services.PermitDocumentService>();

// Background service: auto-mark overdue invoices hourly
builder.Services.AddHostedService<JobTracker.Services.OverdueInvoiceService>();

// SECURITY: Configure JWT Authentication
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? "PROVIA-Production-SecureKey-MinimumLength-32Chars";
var key = Encoding.ASCII.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "Bearer";
    options.DefaultChallengeScheme = "Bearer";
})
.AddJwtBearer("Bearer", options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

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

// Add CORS for the frontend - RESTRICTED (from environment variable)
var allowedOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS") ?? "http://localhost:3000,http://localhost:5000,http://0.0.0.0:5000";
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", builder =>
    {
        builder.WithOrigins(allowedOrigins.Split(","))
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

// Add rate limiting for brute force protection
builder.Services.AddMemoryCache();

// Register SMS Service
builder.Services.AddScoped<JobTracker.Services.SMSService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Trust X-Forwarded-Proto from Nginx reverse proxy (prevents redirect loops in Docker)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

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
app.MapGet("/api/test", () => new { Message = "PROVIA API is working!", Timestamp = DateTime.UtcNow });

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

        // Add new columns to existing tables (idempotent)
        var startupLogger = services.GetRequiredService<ILogger<Program>>();
        var addColumns = new[]
        {
            "ALTER TABLE \"Jobs\" ADD COLUMN IF NOT EXISTS \"ContactId\" INTEGER;",

            // Invoices table (JSON line-items model)
            @"CREATE TABLE IF NOT EXISTS ""Invoices"" (
                ""Id""             SERIAL PRIMARY KEY,
                ""CompanyId""      INTEGER NOT NULL,
                ""InvoiceNumber""  VARCHAR(50) NOT NULL,
                ""JobId""          INTEGER,
                ""ContactId""      INTEGER,
                ""ClientName""     VARCHAR(200),
                ""ClientEmail""    VARCHAR(200),
                ""ClientAddress""  VARCHAR(300),
                ""ClientPhone""    VARCHAR(30),
                ""Notes""          VARCHAR(2000),
                ""Terms""          VARCHAR(2000),
                ""LineItemsJson""  TEXT NOT NULL DEFAULT '[]',
                ""Subtotal""       NUMERIC(12,2) NOT NULL DEFAULT 0,
                ""TaxRate""        NUMERIC(5,2)  NOT NULL DEFAULT 0,
                ""TaxAmount""      NUMERIC(12,2) NOT NULL DEFAULT 0,
                ""Total""          NUMERIC(12,2) NOT NULL DEFAULT 0,
                ""AmountPaid""     NUMERIC(12,2) NOT NULL DEFAULT 0,
                ""BalanceDue""     NUMERIC(12,2) NOT NULL DEFAULT 0,
                ""Status""         VARCHAR(30)   NOT NULL DEFAULT 'draft',
                ""IssuedAt""       TIMESTAMPTZ,
                ""DueAt""          TIMESTAMPTZ,
                ""SentAt""         TIMESTAMPTZ,
                ""PaidAt""         TIMESTAMPTZ,
                ""CreatedBy""      VARCHAR(100),
                ""CreatedAt""      TIMESTAMPTZ NOT NULL DEFAULT now(),
                ""UpdatedAt""      TIMESTAMPTZ NOT NULL DEFAULT now()
            );",

            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Invoices_CompanyId_InvoiceNumber""
                ON ""Invoices"" (""CompanyId"", ""InvoiceNumber"");",

            @"CREATE INDEX IF NOT EXISTS ""IX_Invoices_CompanyId""  ON ""Invoices"" (""CompanyId"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_Invoices_Status""      ON ""Invoices"" (""CompanyId"", ""Status"");",

            // InvoicePayments table
            @"CREATE TABLE IF NOT EXISTS ""InvoicePayments"" (
                ""Id""           SERIAL PRIMARY KEY,
                ""InvoiceId""    INTEGER NOT NULL REFERENCES ""Invoices""(""Id"") ON DELETE CASCADE,
                ""CompanyId""    INTEGER NOT NULL,
                ""Amount""       NUMERIC(12,2) NOT NULL,
                ""Method""       VARCHAR(50)   NOT NULL DEFAULT 'check',
                ""Reference""    VARCHAR(200),
                ""Notes""        VARCHAR(1000),
                ""PaidAt""       TIMESTAMPTZ NOT NULL DEFAULT now(),
                ""RecordedBy""   VARCHAR(100),
                ""CreatedAt""    TIMESTAMPTZ NOT NULL DEFAULT now()
            );",

            @"CREATE INDEX IF NOT EXISTS ""IX_InvoicePayments_InvoiceId""  ON ""InvoicePayments"" (""InvoiceId"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_InvoicePayments_CompanyId""  ON ""InvoicePayments"" (""CompanyId"");",

            // Vendors table
            @"CREATE TABLE IF NOT EXISTS ""Vendors"" (
                ""Id""             SERIAL PRIMARY KEY,
                ""CompanyId""      INTEGER NOT NULL,
                ""VendorNumber""   VARCHAR(50) NOT NULL,
                ""Name""           VARCHAR(200) NOT NULL,
                ""Category""       VARCHAR(100),
                ""ContactName""    VARCHAR(200),
                ""Email""          VARCHAR(200),
                ""Phone""          VARCHAR(30),
                ""Address""        VARCHAR(300),
                ""City""           VARCHAR(100),
                ""State""          VARCHAR(50),
                ""ZipCode""        VARCHAR(20),
                ""Website""        VARCHAR(200),
                ""AccountNumber""  VARCHAR(100),
                ""PaymentTerms""   VARCHAR(30),
                ""CreditLimit""    NUMERIC(12,2),
                ""Notes""          VARCHAR(2000),
                ""IsActive""       BOOLEAN NOT NULL DEFAULT TRUE,
                ""IsPreferred""    BOOLEAN NOT NULL DEFAULT FALSE,
                ""CreatedBy""      VARCHAR(100),
                ""CreatedAt""      TIMESTAMPTZ NOT NULL DEFAULT now(),
                ""UpdatedAt""      TIMESTAMPTZ NOT NULL DEFAULT now()
            );",
            @"CREATE INDEX IF NOT EXISTS ""IX_Vendors_CompanyId""  ON ""Vendors"" (""CompanyId"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_Vendors_Name""        ON ""Vendors"" (""CompanyId"", ""Name"");",

            // VendorPurchases table
            @"CREATE TABLE IF NOT EXISTS ""VendorPurchases"" (
                ""Id""                    SERIAL PRIMARY KEY,
                ""VendorId""              INTEGER NOT NULL REFERENCES ""Vendors""(""Id"") ON DELETE CASCADE,
                ""CompanyId""             INTEGER NOT NULL,
                ""JobId""                 INTEGER,
                ""PurchaseOrderNumber""   VARCHAR(100),
                ""Description""           VARCHAR(2000),
                ""Amount""                NUMERIC(12,2) NOT NULL,
                ""Status""                VARCHAR(30) NOT NULL DEFAULT 'pending',
                ""OrderedAt""             TIMESTAMPTZ,
                ""ReceivedAt""            TIMESTAMPTZ,
                ""RecordedBy""            VARCHAR(100),
                ""CreatedAt""             TIMESTAMPTZ NOT NULL DEFAULT now(),
                ""UpdatedAt""             TIMESTAMPTZ NOT NULL DEFAULT now()
            );",
            @"CREATE INDEX IF NOT EXISTS ""IX_VendorPurchases_VendorId""   ON ""VendorPurchases"" (""VendorId"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_VendorPurchases_CompanyId""  ON ""VendorPurchases"" (""CompanyId"");"
        };
        foreach (var sql in addColumns)
        {
            try { await context.Database.ExecuteSqlRawAsync(sql); }
            catch (Exception colEx) { startupLogger.LogWarning("Column migration skipped: {Msg}", colEx.Message); }
        }

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
