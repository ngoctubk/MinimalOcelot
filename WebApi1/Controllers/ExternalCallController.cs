using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System.Text.Json;

namespace WebApi1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class ExternalCallController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ExternalCallController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet(Name = "GetExternalWeatherForecast")]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            var httpClient = _httpClientFactory.CreateClient("WebAPI2");

            var httpResponseMessage = await httpClient.GetAsync("WeatherForecast");

            var weatherForecasts = new List<WeatherForecast>();

            if (httpResponseMessage.IsSuccessStatusCode)
            {
                using var content= await httpResponseMessage.Content.ReadAsStreamAsync();

                weatherForecasts = (await JsonSerializer.DeserializeAsync<IEnumerable<WeatherForecast>>(content))?.ToList();
            }

            return weatherForecasts;
        }
    }
}
