using System.Text.RegularExpressions;
using AStar.Dev.Wallpaper.Scrapper.ApiClient;
using AStar.Dev.Wallpaper.Scrapper.Models;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scrapper.Pages;

public sealed partial class SubscriptionsImagesListPage(
    WallhavenApiClient  apiClient,
    SearchConfiguration searchConfiguration,
    UserConfiguration   userConfiguration,
    Logger              logger)
{
    // Fetches HTML subscription page (requires SessionCookie in UserConfiguration).
    // Returns empty if SessionCookie is not configured.
    public async Task<(int pageCount, string subDirectoryName)> LoadAndGetPageInfoAsync(int pageNumber)
    {
        var html = await FetchSubscriptionHtmlAsync(pageNumber);

        if(html is null) return (0, string.Empty);

        var match = ImageCountPattern().Match(html);

        if(!match.Success) return (0, string.Empty);

        var imageCount = decimal.Parse(match.Groups[1].Value.Replace(",", string.Empty));

        return (Convert.ToInt32(Math.Ceiling(imageCount / 24)), "New-Subscription-Wallpapers");
    }

    public async Task<IReadOnlyCollection<WallhavenWallpaper>> GetWallpapersAsync(int pageNumber)
    {
        var html = await FetchSubscriptionHtmlAsync(pageNumber);

        if(html is null) return [];

        var ids       = WallpaperIdPattern().Matches(html).Select(m => m.Groups[1].Value).Distinct().ToList();
        var wallpapers = new List<WallhavenWallpaper>(ids.Count);

        foreach(var id in ids)
        {
            try
            {
                wallpapers.Add(await apiClient.GetWallpaperAsync(id));
            }
            catch(Exception exception)
            {
                logger.Warning("Could not get wallpaper {id}: {message}", id, exception.GetBaseException().Message);
            }
        }

        return wallpapers;
    }

    public void Clear()
        => logger.Warning("Subscription clear requires a browser session. Clear manually at https://wallhaven.cc/subscription");

    private async Task<string?> FetchSubscriptionHtmlAsync(int pageNumber)
    {
        if(string.IsNullOrEmpty(userConfiguration.SessionCookie))
        {
            logger.Warning("SessionCookie not configured — subscription download skipped. See UserConfiguration.SessionCookie.");

            return null;
        }

        using var client  = new HttpClient { BaseAddress = new Uri(searchConfiguration.BaseUrl), Timeout = TimeSpan.FromMinutes(2) };
        using var request = new HttpRequestMessage(HttpMethod.Get, $"subscription?page={pageNumber}");
        request.Headers.Add("Cookie",          userConfiguration.SessionCookie);
        request.Headers.Add("User-Agent",      "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
        request.Headers.Add("Accept",          "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
        request.Headers.Add("Accept-Language", "en-US,en;q=0.5");

        try
        {
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
        catch(Exception exception)
        {
            logger.Error("Failed to fetch subscription page {page}: {message}", pageNumber, exception.GetBaseException().Message);

            return null;
        }
    }

    [GeneratedRegex(@"([\d,]+)\s+New Subscription Wallpapers")]
    private static partial Regex ImageCountPattern();

    [GeneratedRegex(@"href=""/w/([a-zA-Z0-9]+)""")]
    private static partial Regex WallpaperIdPattern();
}
