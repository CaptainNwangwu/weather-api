using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherController(
        IConfiguration config,
        IMemoryCache cache,
        ILogger<WeatherController> logger,
        IHttpClientFactory httpClientFactory) : ControllerBase
    {
        private readonly IMemoryCache _cache = cache;

        private readonly ILogger _logger = logger;

        private readonly MemoryCacheEntryOptions _cacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        };
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly string _baseURL = config["WeatherApi:BaseUrl"]
            ?? throw new InvalidOperationException("WeatherApi:BaseUrl not configured");

        private readonly string _multiURL = config["WeatherApi:MultiSearchUrl"]
            ?? throw new InvalidOperationException("WeatherApi:MultiSearchUrl not configured");
        private readonly string _apiKey = config["WeatherApi:ApiKey"]
            ?? throw new InvalidOperationException("WeatherApi:ApiKey not configured");

        private readonly string _unitGroup = config["WeatherApi:UnitGroup"]
            ?? throw new InvalidOperationException("WeatherApi:UnitGroup not configured");

        private IActionResult HandleExternalApiError(HttpResponseMessage response)
        {
            var errorMessage = response.StatusCode switch
            {
                System.Net.HttpStatusCode.BadRequest => "Invalid request — check location format and parameters",
                System.Net.HttpStatusCode.Unauthorized => "Weather API authentication failed — check API key",
                System.Net.HttpStatusCode.NotFound => "Location not found — check city name or coordinates",
                System.Net.HttpStatusCode.TooManyRequests => "Weather API rate limit exceeded — try again later",
                _ => "Weather API request failed"
            };

            return StatusCode((int)response.StatusCode, new { error = errorMessage });
        }

        /// <summary>
        /// Retrieves weather forecast data for a location from the Visual Crossing API.
        /// Results are cached in Redis for 30 minutes to reduce external API calls.
        /// </summary>
        /// <param name="location">Address, city name, ZIP code, or lat,long coordinates.</param>
        /// <param name="date1">Optional start date (yyyy-MM-dd). Without date2, returns a single day.</param>
        /// <param name="date2">Optional end date (yyyy-MM-dd). Requires date1. Returns an inclusive date range.</param>
        /// <param name="include">Optional comma-separated data sections: days, hours, minutes, alerts, current, obs, fcst, stats.</param>
        /// <param name="elements">Optional comma-separated weather elements: tempmax, tempmin, temp, humidity, precip, etc.</param>
        /// <returns>
        /// 200 with JSON weather data on success.
        /// Forwards the external API's status code on failure.
        /// </returns>
        [HttpGet]
        public async Task<IActionResult> GetLocationForecast(
            [FromQuery] string location,
            [FromQuery] string? date1 = null,
            [FromQuery] string? date2 = null,
            [FromQuery] string? include = null,
            [FromQuery] string? elements = null)
        {
            // Build path conditionally
            var path = $"{_baseURL}/{location}";

            if (date1 is not null)
                path += $"/{date1}";

            if (date2 is not null && date1 is not null)
                path += $"/{date2}";

            // Build query params — key and unitGroup always present. Content Type will be JSON.
            var queryParams = new List<string>
                {
                    $"key={_apiKey}",
                    $"unitGroup={_unitGroup}",
                    $"contentType=json"
                };


            if (include is not null)
                queryParams.Add($"include={include}");

            if (elements is not null)
                queryParams.Add($"elements={elements}");

            var requestUrl = $"{path}?{string.Join("&", queryParams)}";

            // For security reasons, we cache a variation without the APIKey (@queryParams[0])
            var cachedQueryParams = queryParams[1..];
            var cachedUrl = $"{path}?{string.Join("&", cachedQueryParams)}";

            // Check the cache first
            var cached = _cache.Get<string>(cachedUrl);
            if (cached is not null)
            {
                _logger.LogInformation("Cache HIT: {Key}", cachedUrl);
                return Content(cached, "application/json");
            }
            // On Cache miss - Check External API.
            _logger.LogInformation("Cache MISS: {Key}", cachedUrl);

            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(requestUrl);

            if (!response.IsSuccessStatusCode)
                HandleExternalApiError(response);

            var content = await response.Content.ReadAsStringAsync();

            // Store in cache for next time 
            _cache.Set(cachedUrl, content, _cacheOptions);
            return Content(content, "application/json");
        }

        /// <summary>
        /// Retrieves weather forecast data for multiple locations from the Visual Crossing API. 
        /// Results are cached in redis for 30 mins to reduce external API calls.
        /// </summary>
        /// <param name="locations">Address, city name, ZIP Code, or lat,long coordinates. Each location, however,
        /// must be separated by a pipe `(|)` symbol. e.g. `Phoenix,USA|London,UK|Hamburg,Germany`.</param>
        /// <param name="datestart">Optional start date (yyyy-MM-dd). Without date 2, returns a single day.</param>
        /// <param name="dateend">Optional end date (yyyy-MM-dd). Requires date 1. Returns an inclusive date range.</param>
        /// <param name="include">Optional comma-separated data sections: days, hours, minutes, alerts, current, obs, fcst, stats</param>
        /// <param name="elements">Optional comma-separated weather elements: tempmax, tempmin, temp, humidity, precip, etc.</param>
        /// <returns>
        /// 200 with JSON weather data on success.
        /// Fowards the external API's status code on failure.
        /// </returns>
        [HttpGet("multi")]
        public async Task<IActionResult> GetMultiLocationForecast(
            [FromQuery] string locations,
            [FromQuery] string? datestart = null,
            [FromQuery] string? dateend = null,
            [FromQuery] string? include = null,
            [FromQuery] string? elements = null)
        {
            // Uri encode all locations
            var encodedLocations = Uri.EscapeDataString(locations);

            // Build query params — All members are treated as query parameters.
            var queryParams = new List<string>
            {
                $"key={_apiKey}",
                $"unitGroup={_unitGroup}",
                $"locations={encodedLocations}",
                $"contentType=json"
            };

            if (datestart is not null)
                queryParams.Add($"datestart={datestart}");

            if (dateend is not null)
                queryParams.Add($"dateend={dateend}");

            if (include is not null)
                queryParams.Add($"include={include}");

            if (elements is not null)
                queryParams.Add($"elements={elements}");

            var requestUrl = $"{_multiURL}?{string.Join("&", queryParams)}";

            // --- Caching --- //
            // For security reasons, exclude storage of the ApiKey
            var cachedPath = $"{_multiURL}/{encodedLocations}";
            var cachedUrl = $"{cachedPath}?{string.Join("&", queryParams)}";

            // Check cache before sending request to external API.
            var cached = _cache.Get<string>(cachedUrl);
            if (cached is not null)
            {
                _logger.LogInformation("Cache HIT: {Key}", cachedUrl);
                return Content(cached, "application/json");
            }
            // On Cache Miss - Check external API
            _logger.LogInformation("Cache MISS: {Key}", cachedUrl);
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(requestUrl);

            if (!response.IsSuccessStatusCode)
                HandleExternalApiError(response);

            var content = await response.Content.ReadAsStringAsync();

            // Store in cache for next time
            _cache.Set(cachedUrl, content, _cacheOptions);
            return Content(content, "application/json");
        }

    }
}
