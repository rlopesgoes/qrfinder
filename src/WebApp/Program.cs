var builder = WebApplication.CreateBuilder(args);

// Add CORS for cross-origin requests
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Enable CORS
app.UseCors();

// Configure static files
app.UseDefaultFiles(); // This will serve index.html by default
app.UseStaticFiles();


app.Run();