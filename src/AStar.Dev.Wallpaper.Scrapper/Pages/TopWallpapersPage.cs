using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Services;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scrapper.Pages;

public sealed class TopWallpapersPage(IPlaywrightService playwrightService, SearchConfiguration searchConfiguration) : ITopWallpapersPage
{
    private IPage page = null!;

    private ILocator PageCount => page.GetByText("Page ", new PageGetByTextOptions { Exact = false, });

    private ILocator ImagePreviews => page.GetByRole(AriaRole.Link);

    public async Task<IResponse?> LoadTopWallpapersPageAsync(int pageNumber)
    {
        page ??= await playwrightService.ConfigurePlaywrightAsync();
        return _ = await page.GotoAsync($"{searchConfiguration.TopWallpapers}{pageNumber}");
    }

    public async Task<int> PageInfoAsync()
    {
        page ??= await playwrightService.ConfigurePlaywrightAsync();
        string? text = await PageCount.First.TextContentAsync();

        if (text is null) return 0;

        return TopWallpapersHeaderParser.Parse(text).Match(
            pageCount => pageCount,
            error => throw new InvalidOperationException(error.Message));
    }

    public async Task<IReadOnlyCollection<string>> GetImagePageLinksAsync()
    {
        page ??= await playwrightService.ConfigurePlaywrightAsync();
        var imagePreviews = await ImagePreviews.AllAsync().ConfigureAwait(false);
        List<string?> hrefs = [];
        foreach (var imagePreview in imagePreviews) hrefs.Add(await imagePreview.GetAttributeAsync("href").ConfigureAwait(false));

        return ImageLinkSelector.SelectWanted(hrefs);
    }
}
