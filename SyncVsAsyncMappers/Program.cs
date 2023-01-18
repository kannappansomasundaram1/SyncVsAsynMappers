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
    
    [Params(10, 25, 100)]
    public int NumberOfDays;

    [Benchmark(Baseline = true)]
    public async Task<MappedWeatherResponse[]> GetWeatherResponseAsyncMapper()
    {
        var WeatherMapperTasks = GetWeather()
            .Select(MapWeatherDataAsync);
        return await Task.WhenAll(WeatherMapperTasks);
    }
    
    [Benchmark]
    public async Task<MappedWeatherResponse[]> GetWeatherResponseAsyncMapperValueTask()
    {
        var WeatherMapperTasks = GetWeather()
            .Select(MapWeatherDataAsyncValueTask);
        return await Task.WhenAll(WeatherMapperTasks);
    }
    
    [Benchmark]
    public async Task<MappedWeatherResponse[]> GetWeatherResponseSyncMapper()
    {
        var weatherResponses = GetWeather().ToArray();
        var distinctCityIds = weatherResponses.Select(x => x.Id).Distinct();

        var cityNameById = await ResolveReferenceData(distinctCityIds);

        return weatherResponses
            .Select(response => MapWeatherDataSync(response, cityNameById))
            .ToArray();
    }

    private async Task<Dictionary<int, string>> ResolveReferenceData(IEnumerable<int> distinctCityIds)
    {
        var cityNameById = new Dictionary<int, string>();

        foreach (var cityId in distinctCityIds)
        {
            var cityName = await GetCityName(cityId);
            cityNameById.Add(cityId, cityName);
        }

        return cityNameById;
    }

    private IEnumerable<WeatherResponse> GetWeather()
    {
        return Enumerable.Range(1, NumberOfDays)
            .Select(i => new WeatherResponse(1, DateOnly.FromDateTime(DateTime.Today).AddDays(i) , 40 + i));
    }

    private async Task<MappedWeatherResponse> MapWeatherDataAsync(WeatherResponse weatherResponse)
    {
        var cityName = await GetCityName(weatherResponse.Id);
        return new MappedWeatherResponse(cityName, weatherResponse.date,
            CelsiusToFahrenheit(weatherResponse.TemperatureInCelsius));
    }
    
    private async Task<MappedWeatherResponse> MapWeatherDataAsyncValueTask(WeatherResponse weatherResponse)
    {
        var cityName = await GetCityNameValueTask(weatherResponse.Id);
        return new MappedWeatherResponse(cityName, weatherResponse.date,
            CelsiusToFahrenheit(weatherResponse.TemperatureInCelsius));
    }
    
    private MappedWeatherResponse MapWeatherDataSync(WeatherResponse weatherResponse, IReadOnlyDictionary<int, string> CityNameById)
    {
        return new MappedWeatherResponse(CityNameById[weatherResponse.Id], weatherResponse.date,
            CelsiusToFahrenheit(weatherResponse.TemperatureInCelsius));
    }

    private async Task<string> GetCityName(int cityId)
    {
        await Task.Delay(0);
        return CityById[cityId];
    }
    
    private async ValueTask<string> GetCityNameValueTask(int cityId)
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