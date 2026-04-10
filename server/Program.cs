using System.Threading.RateLimiting;
using Scalar.AspNetCore;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var allowedOrigins = new List<string> { "http://localhost:5239", "http://localhost:5173", "http://localhost:5174" };
var frontendUrl = builder.Configuration["FRONTEND_URL"];
if (!string.IsNullOrWhiteSpace(frontendUrl))
    allowedOrigins.Add(frontendUrl);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins.ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("X-Cache-Status");
    });
});


builder.Services.AddRateLimiter(options =>
{
    // Chained: global server cap first, then per-IP cap
    options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
        // Hard cap on total requests regardless of who's asking
        PartitionedRateLimiter.Create<HttpContext, string>(_ =>
            RateLimitPartition.GetFixedWindowLimiter("global", _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1)
            })),
        // Per-IP cap for all endpoints
        PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1)
                }))
    );

    // Stricter per-IP policy for multi-location (expensive upstream calls)
    options.AddPolicy("multi", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});


builder.Services.AddMemoryCache();


builder.Services.AddHttpClient();
builder.Services.AddControllers();


var app = builder.Build();

// Configure the HTTP request pipeline.

app.MapOpenApi();
app.MapScalarApiReference();

app.UseCors();
app.UseRateLimiter();
// app.UseHttpsRedirection();  // Azure handles HTTPS at the ingress level
app.MapControllers();

// Server health check
app.MapGet("/", () => "WeatherAPI is running!");

app.Run();

