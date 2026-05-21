using System.Text.Json;

namespace AStar.Dev.Wallpaper.Scrapper.ApiClient;

public sealed class WallhavenApiClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient httpClient;

    public WallhavenApiClient(string apiKey)
    {
        httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://wallhaven.cc/api/v1/"),
            Timeout     = TimeSpan.FromMinutes(2),
        };
        httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
    }

    // searchPath format: "/search?q=id:872&...&page=" (same as appsettings searchString/topWallpapers)
    public async Task<WallhavenSearchResponse> SearchAsync(string searchPath, int page)
    {
        var queryParams = searchPath.Replace("/search?", "");
        var response    = await httpClient.GetAsync($"search?{queryParams}{page}");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<WallhavenSearchResponse>(json, JsonOptions)!;
    }

    public async Task<WallhavenWallpaper> GetWallpaperAsync(string id)
    {
        var response = await httpClient.GetAsync($"w/{id}");
        response.EnsureSuccessStatusCode();
        var json   = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<WallhavenSingleResponse>(json, JsonOptions)!;

        return result.Data;
    }

    public void Dispose() => httpClient.Dispose();
}
