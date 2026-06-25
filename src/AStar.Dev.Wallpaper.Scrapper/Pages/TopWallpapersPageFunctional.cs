using AStar.Dev.Wallpaper.Scrapper.Models;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scrapper.Pages;

public interface ITopWallpapersPageFunctional
{
    Task<IReadOnlyCollection<string>> GetImagePageLinks();
    Task<IResponse?> LoadTopWallpapersPageAsync(int pageNumber);
    Task<int> PageInfoAsync();
}

public sealed class TopWallpapersPageFunctional(SearchConfiguration searchConfiguration, IPage page) : ITopWallpapersPageFunctional
{
    public async Task<IResponse?> LoadTopWallpapersPageAsync(int pageNumber)
        => _ = await page.GotoAsync($"{searchConfiguration.TopWallpapers}{pageNumber}");

    public async Task<int> PageInfoAsync()
    {
        var text = await page.GetByText("Page ", new PageGetByTextOptions { Exact = false, }).First.TextContentAsync();

        if (text is null) return 0;

        var firstSlashIndex = text.IndexOf('/') + 1;
        var pages = text[firstSlashIndex..].Trim();

        return Convert.ToInt32(pages);
    }

    public async Task<IReadOnlyCollection<string>> GetImagePageLinks()
    {
        List<string> wantedLinks = [];
        IReadOnlyList<ILocator> imagePreviews = await page.GetByRole(AriaRole.Link).AllAsync();

        foreach (ILocator imagePreview in imagePreviews)
        {
            var hrefString = await imagePreview.GetAttributeAsync("href");

            if (hrefString != null && hrefString.Contains("/w/")) wantedLinks.Add(hrefString);
        }

        return [.. wantedLinks.Take(24)];
    }
}
