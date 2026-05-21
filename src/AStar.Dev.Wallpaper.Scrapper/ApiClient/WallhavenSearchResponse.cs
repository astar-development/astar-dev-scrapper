using System.Text.Json.Serialization;

namespace AStar.Dev.Wallpaper.Scrapper.ApiClient;

public sealed class WallhavenSearchResponse
{
    [JsonPropertyName("data")]
    public List<WallhavenWallpaper> Data { get; set; } = [];

    [JsonPropertyName("meta")]
    public WallhavenMeta Meta { get; set; } = new();
}
