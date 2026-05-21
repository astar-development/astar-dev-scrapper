using System.Text.Json.Serialization;

namespace AStar.Dev.Wallpaper.Scrapper.ApiClient;

public sealed class WallhavenSingleResponse
{
    [JsonPropertyName("data")]
    public WallhavenWallpaper Data { get; set; } = new();
}
