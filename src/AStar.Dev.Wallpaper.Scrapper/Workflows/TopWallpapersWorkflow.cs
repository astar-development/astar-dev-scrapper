using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.Wallpaper.Scrapper.Support;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scrapper.Workflows;

public sealed class TopWallpapersWorkflow(
    ITopWallpapersPage topWallpapersPage,
    ImagePageService imagePageService,
    SearchConfiguration searchConfiguration,
    ConfigurationSaver configurationSaver,
    Logger logger)
{
    private SearchConfiguration _searchConfiguration = searchConfiguration;

    public async Task RunAsync(CancellationToken ct = default)
    {
        try
        {
            await GetTheNewTopWallpapersAsync(ct).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.Error(exception.GetBaseException().Message);
            throw;
        }
    }

    private async Task GetTheNewTopWallpapersAsync(CancellationToken ct)
    {
        var pageDetails = await topWallpapersPage.LoadTopWallpapersPageAsync(_searchConfiguration.TopWallpapersStartingPageNumber).ConfigureAwait(false);
        var loadedSuccessfully = pageDetails.Match(_ => true, _ => false);

        if (!loadedSuccessfully) _ = await topWallpapersPage.LoadTopWallpapersPageAsync(1).ConfigureAwait(false);

        int pageCount = (await topWallpapersPage.PageInfoAsync().ConfigureAwait(false))
            .Match(count => count, error => throw new InvalidOperationException(error.Message));
        logger.Information("There are a total of {TopWallpapersPageCount} pages for the Top Wallpapers.", pageCount);
        _searchConfiguration = _searchConfiguration with { TopWallpapersTotalPages = pageCount };

        await configurationSaver.SaveUpdatedConfigurationAsync().ConfigureAwait(false);

        for (int currentPageNumber = _searchConfiguration.TopWallpapersStartingPageNumber;
            currentPageNumber <= _searchConfiguration.TopWallpapersTotalPages;
            currentPageNumber++)
        {
            ct.ThrowIfCancellationRequested();
            int delay = Random.Shared.Next(_searchConfiguration.ImagePauseInSeconds, _searchConfiguration.ImagePauseInSeconds + 4);
            await Task.Delay(TimeSpan.FromSeconds(delay), ct).ConfigureAwait(false);
            _searchConfiguration = _searchConfiguration with { TopWallpapersStartingPageNumber = currentPageNumber };
            await configurationSaver.SaveUpdatedConfigurationAsync().ConfigureAwait(false);
            _ = await topWallpapersPage.LoadTopWallpapersPageAsync(_searchConfiguration.TopWallpapersStartingPageNumber).ConfigureAwait(false);

            var imagePageLinks = (await topWallpapersPage.GetImagePageLinksAsync().ConfigureAwait(false))
                .Match(links => links, error => throw new InvalidOperationException(error.Message));

            _ = (await imagePageService.GetTheImagePagesAsync(imagePageLinks, "", "", ct: ct).ConfigureAwait(false))
                .Match(unit => unit, error => throw new InvalidOperationException(error.Message));
        }
    }
}
