using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherController(IConfiguration config, IHttpClientFactory httpClientFactory) : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly string _baseURL = config["WeatherApi:BaseUrl"]
            ?? throw new InvalidOperationException("WeatherApi:BaseUrl not configured");
        private readonly string _apiKey = config["WeatherApi:ApiKey"]
            ?? throw new InvalidOperationException("WeatherApi:ApiKey not configured");

        private readonly string _unitGroup = config["WeatherApi:UnitGroup"]
            ?? throw new InvalidOperationException("WeatherApi:UnitGroup not configured");

        /// <summary>
        /// Retrieves weather forecast data for a specified city from an external weather API.
        /// </summary>
        /// <param name="city">The name of the city for which to retrieve weather forecast data. Passed as a query parameter.</param>
        /// <returns>
        /// An IActionResult containing:
        /// - HTTP 200 with JSON forecast data if the external API call succeeds
        /// - The original HTTP status code from the external API (with error message) if the request fails
        /// </returns>
        [HttpGet]
        public async Task<IActionResult> GetLocationForecast(
            [FromQuery] string city
        )
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{_baseURL}{city}?unitGroup={_unitGroup}&key={_apiKey}&contentType=json");

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, "Weather API request failed!");

            var content = await response.Content.ReadAsStringAsync();
            return Content(content, "application/json");
        }
    }
}
