using AStar.Dev.Wallpaper.Scrapper.Pages;

namespace AStar.Dev.Wallpaper.Scrapper.Tests;

public sealed class DownloadTheTopWallpapersNotAlreadyDownloadedTests : WallpaperScrapperTestBase
{
    [Xunit.Fact]
    public async Task ICanDownloadAllOfTheTopWallpapersIveNotAlreadyGot()
    {
        var topWallpapersPage = new TopWallpapersPage(ApiClient, ScrapeConfig.SearchConfiguration);
        var imagePageService  = CreateImagePageService();

        try
        {
            await GetTheNewTopWallpapersAsync(topWallpapersPage, imagePageService);
        }
        catch(Exception exception)
        {
            Logger.Error(exception.GetBaseException().Message);

            throw;
        }
    }

    private async Task GetTheNewTopWallpapersAsync(TopWallpapersPage topWallpapersPage, Services.ImagePageService imagePageService)
    {
        _ = await topWallpapersPage.LoadTopWallpapersPageAsync(ScrapeConfig.SearchConfiguration.TopWallpapersStartingPageNumber);

        var pageCount = topWallpapersPage.PageInfo();
        Logger.Information("There are a total of {TopWallpapersPageCount} pages for the Top Wallpapers.", pageCount);
        ScrapeConfig.SearchConfiguration.TopWallpapersTotalPages = pageCount;
        ConfigurationSaver.SaveUpdatedConfiguration();

        for(var currentPageNumber = ScrapeConfig.SearchConfiguration.TopWallpapersStartingPageNumber;
            currentPageNumber <= ScrapeConfig.SearchConfiguration.TopWallpapersTotalPages;
            currentPageNumber++)
        {
            var delay = Random.Shared.Next(ScrapeConfig.SearchConfiguration.ImagePauseInSeconds, ScrapeConfig.SearchConfiguration.ImagePauseInSeconds + 4);
            Thread.Sleep(TimeSpan.FromSeconds(delay));
            ScrapeConfig.SearchConfiguration.TopWallpapersStartingPageNumber = currentPageNumber;
            ConfigurationSaver.SaveUpdatedConfiguration();
            _ = await topWallpapersPage.LoadTopWallpapersPageAsync(currentPageNumber);

            var wallpapers = topWallpapersPage.GetWallpapers();
            await imagePageService.ProcessWallpapersAsync(wallpapers);
        }
    }
}
