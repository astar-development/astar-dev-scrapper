using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.Wallpaper.Scrapper.Support;
using Microsoft.Playwright;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scrapper.Workflows;

public sealed class TopWallpapersWorkflow(
    TopWallpapersPage   topWallpapersPage,
    ImagePageService    imagePageService,
    SearchConfiguration searchConfiguration,
    ConfigurationSaver  configurationSaver,
    Logger              logger)
{
    private SearchConfiguration _searchConfiguration = searchConfiguration;

    public async Task RunAsync()
    {
        try
        {
            await GetTheNewTopWallpapersAsync();
        }
        catch(Exception exception)
        {
            logger.Error(exception.GetBaseException().Message);
            throw;
        }
    }

    private async Task GetTheNewTopWallpapersAsync()
    {
        IResponse? pageDetails = await topWallpapersPage.LoadTopWallpapersPageAsync(_searchConfiguration.TopWallpapersStartingPageNumber);

        if(pageDetails is { Ok: false, }) _ = await topWallpapersPage.LoadTopWallpapersPageAsync(1);

        var pageCount = await topWallpapersPage.PageInfoAsync();
        logger.Information("There are a total of {TopWallpapersPageCount} pages for the Top Wallpapers.", pageCount);
        _searchConfiguration = _searchConfiguration with { TopWallpapersTotalPages = pageCount };

        await configurationSaver.SaveUpdatedConfigurationAsync();

        for(var currentPageNumber = _searchConfiguration.TopWallpapersStartingPageNumber;
            currentPageNumber <= _searchConfiguration.TopWallpapersTotalPages;
            currentPageNumber++)
        {
            var delay = Random.Shared.Next(_searchConfiguration.ImagePauseInSeconds, _searchConfiguration.ImagePauseInSeconds + 4);
            await Task.Delay(TimeSpan.FromSeconds(delay));
            _searchConfiguration = _searchConfiguration with { TopWallpapersStartingPageNumber = currentPageNumber };
            await configurationSaver.SaveUpdatedConfigurationAsync();
            _ = await topWallpapersPage.LoadTopWallpapersPageAsync(_searchConfiguration.TopWallpapersStartingPageNumber);
            IReadOnlyCollection<string> imagePageLinks = await topWallpapersPage.GetImagePageLinks();

            await imagePageService.GetTheImagePagesAsync(imagePageLinks);
        }
    }
}
