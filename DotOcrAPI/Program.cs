using DotOcrLib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Removed: builder.Services.AddEndpointsApiExplorer();
// Removed: builder.Services.AddSwaggerGen(...);

// Configure HttpClient for GeminiOcrService
builder.Services.AddHttpClient<GeminiOcrService>();

// Register GeminiOcrService with dependency injection
builder.Services.AddSingleton<GeminiOcrService>();

// Load configuration from appsettings.json
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

var app = builder.Build();

// Configure the HTTP request pipeline.
// Removed: if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();