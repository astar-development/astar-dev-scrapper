using AStar.Dev.Wallpaper.Scrapper.ApiClient;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scrapper.Pages;

public sealed class SearchResultsPage(WallhavenApiClient apiClient, Logger logger)
{
    private WallhavenSearchResponse? lastResponse;

    public async Task<bool> LoadSearchPageAsync(string searchPath, int pageNumber)
    {
        try
        {
            lastResponse = await apiClient.SearchAsync(searchPath, pageNumber);

            return true;
        }
        catch(Exception exception)
        {
            logger.Error(exception.GetBaseException().Message);

            return false;
        }
    }

    public (int pageCount, int imageCount, string subDirectoryName) PageInfo()
    {
        if(lastResponse is null) return (0, 0, string.Empty);

        return (lastResponse.Meta.LastPage, lastResponse.Meta.Total, string.Empty);
    }

    public IReadOnlyCollection<WallhavenWallpaper> GetWallpapers()
        => lastResponse?.Data ?? [];
}
