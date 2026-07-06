using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Services;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scrapper.Pages;

public sealed class SubscriptionsImagesListPage(IPlaywrightService playwrightService, SearchConfiguration searchConfiguration)
{
    private IPage page = null!;

    private ILocator ImagePreviews => page.GetByRole(AriaRole.Link);

    private ILocator NewSubscriptionWallpapersHeader => page.GetByText("New Subscription Wallpapers", new PageGetByTextOptions { Exact = false, });

    public async Task<IResponse?> LoadSubscriptionResultsPageAsync(int pageNumber)
    {
        page ??= await playwrightService.ConfigurePlaywrightAsync();
        return await page.GotoAsync($"{searchConfiguration.Subscriptions}{pageNumber}");
    }

    public async Task<(int pageCount, string subDirectoryName)> PageInfoAsync()
    {
        page ??= await playwrightService.ConfigurePlaywrightAsync();
        string? text = await NewSubscriptionWallpapersHeader.TextContentAsync();

        if (text is null) return (0, string.Empty);

        return SubscriptionHeaderParser.Parse(text).Match(
            pageInfo => (pageInfo.PageCount, pageInfo.SubDirectoryName),
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

    public async Task ClearAsync()
        => await page.Locator("div")
                     .Filter(new LocatorFilterOptions { HasText = " Clear All Subscriptions", })
                     .GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = " Clear All Subscriptions", })
                     .ClickAsync();
}
