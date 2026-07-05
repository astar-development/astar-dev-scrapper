using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scrapper.Pages;

public interface ITopWallpapersPage
{
    Task<IResponse?> LoadTopWallpapersPageAsync(int pageNumber);

    Task<int> PageInfoAsync();

    Task<IReadOnlyCollection<string>> GetImagePageLinksAsync();
}
