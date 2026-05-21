using System.Text.Json.Serialization;

namespace AStar.Dev.Wallpaper.Scrapper.ApiClient;

public sealed class WallhavenTag
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;
}
