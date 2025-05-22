using Microsoft.EntityFrameworkCore;
using JobTracker.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure PostgreSQL connection
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
Console.WriteLine($"Database URL: {connectionString}");

// Add database context with connection string parsing
string processedConnectionString;

if (connectionString.StartsWith("postgresql://"))
{
    // Parse connection string from URL format to standard format
    var uri = new Uri(connectionString);
    var userInfo = uri.UserInfo.Split(':');
    
    processedConnectionString = new Npgsql.NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Database = uri.AbsolutePath.TrimStart('/'),
        Username = userInfo[0],
        Password = userInfo[1],
        SslMode = Npgsql.SslMode.Require
    }.ToString();
}
else
{
    processedConnectionString = connectionString;
}

Console.WriteLine($"Processed connection string created");

builder.Services.AddDbContext<JobTrackerContext>(options =>
{
    options.UseNpgsql(processedConnectionString);
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

app.UseCors("AllowReactApp");

app.UseAuthorization();

app.MapControllers();

// Add simple test endpoint
app.MapGet("/api/test", () => new { Message = "Job Tracker API is working!", Timestamp = DateTime.UtcNow });

app.Run();
