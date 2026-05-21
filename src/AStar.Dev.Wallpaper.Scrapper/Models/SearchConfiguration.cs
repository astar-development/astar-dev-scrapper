namespace AStar.Dev.Wallpaper.Scrapper.Models;

public sealed class SearchConfiguration
{
    public string BaseUrl { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public Category[] SearchCategories { get; set; } = [];

    public string SearchString { get; set; } = string.Empty;

    public string TopWallpapers { get; set; } = string.Empty;

    public string SearchStringPrefix { get; set; } = string.Empty;

    public string SearchStringSuffix { get; set; } = string.Empty;

    public string Subscriptions { get; set; } = string.Empty;

    public int ImagePauseInSeconds { get; set; }

    public int StartingPageNumber { get; set; } = 1;

    public int TotalPages { get; set; }

    public int SubscriptionsStartingPageNumber { get; set; }

    public int SubscriptionsTotalPages { get; set; }

    public int TopWallpapersTotalPages { get; set; }

    public int TopWallpapersStartingPageNumber { get; set; }
}
