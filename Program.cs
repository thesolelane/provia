var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

// Create a simple API endpoint for testing
app.MapGet("/", () => "Welcome to Job Tracker API!");
app.MapGet("/api/test", () => new { Message = "Job Tracker API is working!", Timestamp = DateTime.UtcNow });
app.MapGet("/api/jobs", () => new[] { 
    new { Id = 1, Name = "Kitchen Renovation", Location = "123 Main St", Status = "In Progress" },
    new { Id = 2, Name = "Bathroom Remodel", Location = "456 Oak Ave", Status = "Pending" },
    new { Id = 3, Name = "Basement Finishing", Location = "789 Pine Rd", Status = "Completed" }
});

app.Run();