using AStar.Dev.Wallpaper.Scrapper.Models;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scrapper.Pages;

public interface ITopWallpapersPageFunctional
{
    Task<IReadOnlyCollection<string>> GetImagePageLinks(IPage page);
    Task<IResponse?> LoadTopWallpapersPageAsync(IPage page, int pageNumber);
    Task<int> PageInfoAsync(IPage page);
}

public sealed class TopWallpapersPageFunctional(SearchConfiguration searchConfiguration) : ITopWallpapersPageFunctional
{
    public async Task<IResponse?> LoadTopWallpapersPageAsync(IPage page, int pageNumber)
        => _ = await page.GotoAsync($"{searchConfiguration.TopWallpapers}{pageNumber}");

    public async Task<int> PageInfoAsync(IPage page)
    {
        var text = await page.GetByText("Page ", new PageGetByTextOptions { Exact = false, }).First.TextContentAsync();

        if (text is null) return 0;

        var firstSlashIndex = text.IndexOf('/') + 1;
        var pages = text[firstSlashIndex..].Trim();

        return Convert.ToInt32(pages);
    }

    public async Task<IReadOnlyCollection<string>> GetImagePageLinks(IPage page)
    {
        List<string> wantedLinks = [];
        IReadOnlyList<ILocator> imagePreviews = await page.GetByRole(AriaRole.Link).AllAsync();

        foreach (ILocator imagePreview in imagePreviews)
        {
            var hrefString = await imagePreview.GetAttributeAsync("href");

            if (hrefString != null && hrefString.Contains("/w/")) wantedLinks.Add(hrefString);
        }

        return wantedLinks.Take(24).ToList();
    }
}
