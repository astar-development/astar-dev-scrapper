using System.Text.Json.Serialization;

namespace AStar.Dev.Wallpaper.Scrapper.ApiClient;

public sealed class WallhavenWallpaper
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("tags")]
    public List<WallhavenTag> Tags { get; set; } = [];
}
