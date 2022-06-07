using ConsumerApplication1.Infrastructure;

using Microsoft.AspNetCore.Mvc;

namespace ConsumerApplication1.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IWeatherForecastClient _weatherForecastClient;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IWeatherForecastClient weatherForecastClient)
        {
            _logger = logger;
            _weatherForecastClient = weatherForecastClient;
        }

        [HttpGet("Get1", Name = "GetWeatherForecast1")]
        public async Task<IEnumerable<WeatherForecast>> Get1()
        {
            var weatherForecast = await _weatherForecastClient.GetForecast1Async();

            return weatherForecast;
        }

        [HttpGet("Get2", Name = "GetWeatherForecast2")]
        public async Task<IEnumerable<WeatherForecast>> Get2()
        {
            var weatherForecast = await _weatherForecastClient.GetForecast2Async();

            return weatherForecast;
        }

        [HttpGet("GetExternalCall", Name = "GetWeatherForecast3")]
        public async Task<IEnumerable<WeatherForecast>> GetExternalCall()
        {
            var weatherForecast = await _weatherForecastClient.GetForecastExternalAsync();

            return weatherForecast;
        }
    }
}