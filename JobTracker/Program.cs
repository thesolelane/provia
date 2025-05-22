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

// Add database context
builder.Services.AddDbContext<JobTrackerContext>(options =>
{
    // Directly use the connection string from the environment variable
    options.UseNpgsql(connectionString);
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

// Create a sample jobs endpoint for testing
app.MapGet("/api/jobs", () => 
{
    var jobs = new[]
    {
        new { Id = 1, Name = "Kitchen Renovation", Location = "123 Main St", Status = "In Progress" },
        new { Id = 2, Name = "Bathroom Remodel", Location = "456 Oak Ave", Status = "Pending" },
        new { Id = 3, Name = "Basement Finishing", Location = "789 Pine Rd", Status = "Completed" }
    };
    
    return jobs;
});

app.Run();
