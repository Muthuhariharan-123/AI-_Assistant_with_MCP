using System.Text.Json;

namespace mcp_server.Services;

public static class WeatherService
{
    private static readonly HttpClient HttpClient = new();

    public static async Task<string> GetWeatherAsync(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            throw new ArgumentException("Location cannot be empty.");
        }

        try
        {
            // Step 1: Geocode the location (get latitude and longitude)
            var geocodeUrl = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(location)}&count=1&language=en&format=json";
            var geocodeResponse = await HttpClient.GetStringAsync(geocodeUrl);
            var geocodeDoc = JsonDocument.Parse(geocodeResponse);

            if (!geocodeDoc.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
            {
                return $"Could not find coordinates for location: {location}";
            }

            var firstResult = results[0];
            var lat = firstResult.GetProperty("latitude").GetDouble();
            var lon = firstResult.GetProperty("longitude").GetDouble();
            var name = firstResult.GetProperty("name").GetString();
            var country = firstResult.TryGetProperty("country", out var countryProp) ? countryProp.GetString() : "";

            // Step 2: Get the current weather for those coordinates
            var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current_weather=true";
            var weatherResponse = await HttpClient.GetStringAsync(weatherUrl);
            var weatherDoc = JsonDocument.Parse(weatherResponse);

            if (!weatherDoc.RootElement.TryGetProperty("current_weather", out var currentWeather))
            {
                return $"Failed to parse weather data for {name}.";
            }

            var temp = currentWeather.GetProperty("temperature").GetDouble();
            var windspeed = currentWeather.GetProperty("windspeed").GetDouble();

            return $"The current weather in {name}, {country} is {temp}°C with a wind speed of {windspeed} km/h.";
        }
        catch (Exception ex)
        {
            return $"Error fetching weather: {ex.Message}";
        }
    }
}
