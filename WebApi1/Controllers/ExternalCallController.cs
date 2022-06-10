using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System.Text.Json;

namespace WebApi1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class ExternalCallController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApplicationDbContext _dbContext;

        public ExternalCallController(IHttpClientFactory httpClientFactory, ApplicationDbContext dbContext)
        {
            _httpClientFactory = httpClientFactory;
            this._dbContext = dbContext;
        }

        [HttpGet(Name = "GetExternalWeatherForecast")]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            var books = await _dbContext.Books.ToListAsync();

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
