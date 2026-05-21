using AStar.Dev.Wallpaper.Scrapper.ApiClient;
using AStar.Dev.Wallpaper.Scrapper.Models;

namespace AStar.Dev.Wallpaper.Scrapper.Pages;

public sealed class TopWallpapersPage(WallhavenApiClient apiClient, SearchConfiguration searchConfiguration)
{
    private WallhavenSearchResponse? lastResponse;

    public async Task<bool> LoadTopWallpapersPageAsync(int pageNumber)
    {
        lastResponse = await apiClient.SearchAsync(searchConfiguration.TopWallpapers, pageNumber);

        return true;
    }

    public int PageInfo() => lastResponse?.Meta.LastPage ?? 0;

    public IReadOnlyCollection<WallhavenWallpaper> GetWallpapers()
        => lastResponse?.Data ?? [];
}
