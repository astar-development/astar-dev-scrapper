using System.Text.Json.Serialization;

namespace AStar.Dev.Wallpaper.Scrapper.ApiClient;

public sealed class WallhavenMeta
{
    [JsonPropertyName("current_page")]
    public int CurrentPage { get; set; }

    [JsonPropertyName("last_page")]
    public int LastPage { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }
}
