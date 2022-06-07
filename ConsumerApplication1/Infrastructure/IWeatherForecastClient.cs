using Refit;

namespace ConsumerApplication1.Infrastructure
{
    public interface IWeatherForecastClient
    {
        [Get("/api1/WeatherForecast")]
        Task<IEnumerable<WeatherForecast>> GetForecast1Async();

        [Get("/weather2")]
        Task<IEnumerable<WeatherForecast>> GetForecast2Async();
        [Get("/api1/ExternalCall")]
        Task<IEnumerable<WeatherForecast>> GetForecastExternalAsync();
    }
}
