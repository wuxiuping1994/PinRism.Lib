using DotOcrAPI;
using DotOcrLib; // Namespace changed to match your project name
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection; // Add this using statement for IOperationFilter

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Add the custom operation filter to handle IFormFile
    options.OperationFilter<SwaggerFileOperationFilter>();

    // Optional: Include XML comments for better API documentation in Swagger UI
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

// Configure HttpClient for GeminiOcrService
builder.Services.AddHttpClient<GeminiOcrService>();

// Register GeminiOcrService with dependency injection
builder.Services.AddSingleton<GeminiOcrService>(); // Register as Singleton for simplicity, consider Scoped/Transient based on actual use case

// Load configuration from appsettings.json
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables(); // Allow environment variables to override

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
