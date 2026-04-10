using server.Controllers;
using System.Text;
using System.Threading.RateLimiting;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
            .AllowAnyMethod();
    });
});


builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});


builder.Services.AddMemoryCache();


builder.Services.AddHttpClient();
builder.Services.AddControllers();


var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI();


app.UseCors();
app.UseRateLimiter();
// app.UseHttpsRedirection();  // Azure handles HTTPS at the ingress level
app.MapControllers();

// Server health check
app.MapGet("/", () => "WeatherAPI is running!");

app.Run();

