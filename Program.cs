using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddCors();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();
builder.Services.AddRazorPages();

// Register weather service with API key
// NOTE: In production, store this in a secure configuration
builder.Services.AddSingleton<WeatherService>(provider => 
    new WeatherService(
        provider.GetRequiredService<IHttpClientFactory>(),
        "18e4b666f4c6d57dff36e5f07ea2071d"
    )
);

var app = builder.Build();

// Configure HTTP pipeline
app.UseHttpsRedirection();
app.UseStaticFiles(); // Serve static files
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

// Set up home page
app.MapGet("/", () => Results.Redirect("/index.html"));

// Serve the frontend
app.MapGet("/index.html", async context => {
    context.Response.ContentType = "text/html";
    await context.Response.WriteAsync(@"
    <!DOCTYPE html>
    <html lang='en'>
    <head>
        <meta charset='UTF-8'>
        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
        <title>Weather App</title>
        <style>
            body {
                font-family: Arial, sans-serif;
                max-width: 800px;
                margin: 0 auto;
                padding: 20px;
                line-height: 1.6;
            }
            h1 {
                color: #336699;
                text-align: center;
            }
            .search-container {
                text-align: center;
                margin: 20px 0;
            }
            input, button {
                padding: 8px 15px;
                font-size: 16px;
            }
            button {
                background-color: #336699;
                color: white;
                border: none;
                cursor: pointer;
            }
            .weather-container, .forecast-container {
                margin-top: 30px;
                border: 1px solid #ddd;
                padding: 20px;
                border-radius: 5px;
            }
            .weather-data, .forecast-data {
                display: none;
            }
            .error {
                color: red;
                text-align: center;
            }
            .forecast-day {
                margin-top: 15px;
                padding: 10px;
                background-color: #f5f5f5;
                border-radius: 5px;
            }
            .loading {
                text-align: center;
                display: none;
            }
        </style>
    </head>
    <body>
        <h1>Weather Information</h1>
        
        <div class='search-container'>
            <input type='text' id='cityInput' placeholder='Enter city name'>
            <button onclick='getWeather()'>Get Weather</button>
        </div>
        
        <div class='loading' id='loading'>Loading data...</div>
        
        <div class='weather-container'>
            <h2>Current Weather</h2>
            <div id='weatherError' class='error'></div>
            <div id='weatherData' class='weather-data'>
                <h3 id='cityName'></h3>
                <p><strong>Temperature:</strong> <span id='temperature'></span>°C</p>
                <p><strong>Feels like:</strong> <span id='feelsLike'></span>°C</p>
                <p><strong>Weather:</strong> <span id='weatherDesc'></span></p>
                <p><strong>Humidity:</strong> <span id='humidity'></span>%</p>
                <p><strong>Wind Speed:</strong> <span id='windSpeed'></span> m/s</p>
            </div>
        </div>
        
        <div class='forecast-container'>
            <h2>5-Day Forecast</h2>
            <div id='forecastError' class='error'></div>
            <div id='forecastData' class='forecast-data'></div>
        </div>
        
        <script>
            function getWeather() {
                const city = document.getElementById('cityInput').value;
                
                if (!city) {
                    alert('Please enter a city name');
                    return;
                }
                
                document.getElementById('loading').style.display = 'block';
                document.getElementById('weatherError').textContent = '';
                document.getElementById('forecastError').textContent = '';
                document.getElementById('weatherData').style.display = 'none';
                document.getElementById('forecastData').style.display = 'none';
                
                // Get current weather
                fetch(`/weather/current/${encodeURIComponent(city)}`)
                    .then(response => response.json())
                    .then(data => {
                        if (data.error) {
                            document.getElementById('weatherError').textContent = data.error;
                        } else {
                            document.getElementById('cityName').textContent = data.name + ', ' + data.sys.country;
                            document.getElementById('temperature').textContent = data.main.temp;
                            document.getElementById('feelsLike').textContent = data.main.feels_like;
                            document.getElementById('weatherDesc').textContent = data.weather[0].description;
                            document.getElementById('humidity').textContent = data.main.humidity;
                            document.getElementById('windSpeed').textContent = data.wind.speed;
                            document.getElementById('weatherData').style.display = 'block';
                        }
                    })
                    .catch(err => {
                        document.getElementById('weatherError').textContent = 'Failed to get weather data';
                    });
                
                // Get forecast
                fetch(`/weather/forecast/${encodeURIComponent(city)}`)
                    .then(response => response.json())
                    .then(data => {
                        if (data.error) {
                            document.getElementById('forecastError').textContent = data.error;
                        } else {
                            const forecastContainer = document.getElementById('forecastData');
                            forecastContainer.innerHTML = '';
                            
                            // Group by day - get forecast for each day at noon
                            const dailyForecasts = {};
                            
                            data.list.forEach(item => {
                                const date = item.dt_txt.split(' ')[0];
                                const time = item.dt_txt.split(' ')[1];
                                
                                // Use the noon forecast or closest available
                                if (!dailyForecasts[date] || time === '12:00:00' || 
                                    (time > '12:00:00' && !dailyForecasts[date].isNoon)) {
                                    dailyForecasts[date] = {
                                        ...item,
                                        isNoon: time === '12:00:00'
                                    };
                                }
                            });
                            
                            // Display daily forecasts
                            Object.keys(dailyForecasts).forEach(date => {
                                const forecast = dailyForecasts[date];
                                const dayDiv = document.createElement('div');
                                dayDiv.className = 'forecast-day';
                                
                                const formattedDate = new Date(date).toLocaleDateString('en-US', { 
                                    weekday: 'long', 
                                    year: 'numeric', 
                                    month: 'long', 
                                    day: 'numeric' 
                                });
                                
                                dayDiv.innerHTML = `
                                    <h3>${formattedDate}</h3>
                                    <p><strong>Temperature:</strong> ${forecast.main.temp}°C</p>
                                    <p><strong>Weather:</strong> ${forecast.weather[0].description}</p>
                                    <p><strong>Humidity:</strong> ${forecast.main.humidity}%</p>
                                    <p><strong>Wind Speed:</strong> ${forecast.wind.speed} m/s</p>
                                `;
                                
                                forecastContainer.appendChild(dayDiv);
                            });
                            
                            forecastContainer.style.display = 'block';
                        }
                    })
                    .catch(err => {
                        document.getElementById('forecastError').textContent = 'Failed to get forecast data';
                    })
                    .finally(() => {
                        document.getElementById('loading').style.display = 'none';
                    });
            }
        </script>
    </body>
    </html>
    ");
});

// Weather API endpoints with better responses
app.MapGet("/weather/current/{city}", async (string city, WeatherService weatherService) => {
    var result = await weatherService.GetCurrentWeatherAsync(city);
    
    if (result is WeatherError error)
        return Results.BadRequest(new { error = error.Message });
    
    return Results.Ok(result);
});

app.MapGet("/weather/forecast/{city}", async (string city, WeatherService weatherService) => {
    var result = await weatherService.GetForecastAsync(city);
    
    if (result is WeatherError error)
        return Results.BadRequest(new { error = error.Message });
    
    return Results.Ok(result);
});

app.Run();

// Weather models
public class WeatherError
{
    public string Message { get; set; }
    
    public WeatherError(string message)
    {
        Message = message;
    }
}

// Enhanced Weather Service
public class WeatherService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiKey;
    private readonly string _baseUrl = "https://api.openweathermap.org/data/2.5";
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public WeatherService(IHttpClientFactory httpClientFactory, string apiKey)
    {
        _httpClientFactory = httpClientFactory;
        _apiKey = apiKey;
    }

    public async Task<object> GetCurrentWeatherAsync(string city)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            return new WeatherError("City name cannot be empty");
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(
                $"{_baseUrl}/weather?q={Uri.EscapeDataString(city)}&appid={_apiKey}&units=metric");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new WeatherError($"City '{city}' not found");
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<object>(_jsonOptions) 
                ?? new WeatherError("Failed to retrieve weather data");
        }
        catch (HttpRequestException ex)
        {
            return new WeatherError($"Weather service error: {GetFriendlyErrorMessage(ex)}");
        }
        catch (Exception ex)
        {
            return new WeatherError($"Unexpected error: {ex.Message}");
        }
    }

    public async Task<object> GetForecastAsync(string city)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            return new WeatherError("City name cannot be empty");
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(
                $"{_baseUrl}/forecast?q={Uri.EscapeDataString(city)}&appid={_apiKey}&units=metric");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new WeatherError($"City '{city}' not found");
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<object>(_jsonOptions) 
                ?? new WeatherError("Failed to retrieve forecast data");
        }
        catch (HttpRequestException ex)
        {
            return new WeatherError($"Weather service error: {GetFriendlyErrorMessage(ex)}");
        }
        catch (Exception ex)
        {
            return new WeatherError($"Unexpected error: {ex.Message}");
        }
    }

    private string GetFriendlyErrorMessage(HttpRequestException ex)
    {
        return ex.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => "Invalid API key",
            System.Net.HttpStatusCode.NotFound => "City not found",
            System.Net.HttpStatusCode.TooManyRequests => "Rate limit exceeded. Please try again later",
            _ => $"Service unavailable ({ex.Message})"
        };
    }
}