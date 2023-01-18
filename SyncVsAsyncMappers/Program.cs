using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;

BenchmarkRunner.Run(typeof(Program).Assembly);

[MemoryDiagnoser]
public class SyncVsAsyncMappers
{
    private static readonly Dictionary<int, string> CityById = new()
    {
        { 1, "London" },
        { 2, "Paris" }
    };

    [Benchmark(Baseline = true)]
    public async Task<MappedWeatherResponse[]> GetWeatherResponseAsynMapper()
    {
        var WeatherMapperTasks = GetWeather()
            .Select(MapWeatherDataAsync);
        return await Task.WhenAll(WeatherMapperTasks);
    }
    
    [Benchmark]
    public async Task<MappedWeatherResponse[]> GetWeatherResponseSyncMapper()
    {
        var weatherResponses = GetWeather().ToArray();
        var cityName = await GetCityName(weatherResponses.First().Id);
        
        
        
        return weatherResponses
            .Select(response => MapWeatherDataSync(response, cityName))
            .ToArray();
    }

    private static IEnumerable<WeatherResponse> GetWeather()
    {
        return Enumerable.Range(1, 10)
            .Select(i => new WeatherResponse(1, DateOnly.FromDateTime(DateTime.Today).AddDays(i) , 40 + i));
    }

    private async Task<MappedWeatherResponse> MapWeatherDataAsync(WeatherResponse weatherResponse)
    {
        var cityName = await GetCityName(weatherResponse.Id);
        return new MappedWeatherResponse(cityName, weatherResponse.date,
            CelsiusToFahrenheit(weatherResponse.TemperatureInCelsius));
    }
    
    private MappedWeatherResponse MapWeatherDataSync(WeatherResponse weatherResponse, string CityName)
    {
        return new MappedWeatherResponse(CityName, weatherResponse.date,
            CelsiusToFahrenheit(weatherResponse.TemperatureInCelsius));
    }

    private async Task<string> GetCityName(int cityId)
    {
        await Task.Delay(0);
        return CityById[cityId];
    }

    private double CelsiusToFahrenheit(double celsius) {
        return celsius * 9/5 + 32;
    }
}

public record MappedWeatherResponse(string City, DateOnly date, double TemperatureInFahrenheit);
public record WeatherResponse(int Id,  DateOnly date, double TemperatureInCelsius);