using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Support;

namespace AStar.Dev.Wallpaper.Scrapper.Tests;

public sealed class DownloadSubscriptionImagesNotDownloadedTests : WallpaperScrapperTestBase
{
    [Xunit.Fact]
    public async Task ICanDownloadAllOfTheNewSubscriptionFiles()
    {
        var subscriptionsPage = new SubscriptionsImagesListPage(ApiClient, ScrapeConfig.SearchConfiguration, ScrapeConfig.UserConfiguration, Logger);
        var imagePageService  = CreateImagePageService();

        try
        {
            await GetTheNewSubscriptionImagesAsync(subscriptionsPage, imagePageService);
        }
        catch(Exception exception)
        {
            Logger.Error(exception.GetBaseException().Message);

            throw;
        }
    }

    private async Task GetTheNewSubscriptionImagesAsync(SubscriptionsImagesListPage subscriptionsPage, Services.ImagePageService imagePageService)
    {
        ScrapeConfig.SearchConfiguration.SubscriptionsStartingPageNumber = 1;

        var (pageCount, subDirectoryName) = await subscriptionsPage.LoadAndGetPageInfoAsync(ScrapeConfig.SearchConfiguration.SubscriptionsStartingPageNumber);

        if(subDirectoryName.Length > 0) ScrapeConfig.ScrapeDirectories.SubDirectoryName = subDirectoryName;

        _ = DirectoryHelper.CreateDirectoryIfRequired(
            Path.Combine(ScrapeConfig.ScrapeDirectories.RootDirectory,
                         ScrapeConfig.ScrapeDirectories.BaseDirectory,
                         subDirectoryName));

        UpdateSearchTotalPagesIfRequired(pageCount);
        ConfigurationSaver.SaveUpdatedConfiguration();

        for(var currentPageNumber = ScrapeConfig.SearchConfiguration.SubscriptionsStartingPageNumber;
            currentPageNumber <= ScrapeConfig.SearchConfiguration.SubscriptionsTotalPages;
            currentPageNumber++)
        {
            var delay = Random.Shared.Next(ScrapeConfig.SearchConfiguration.ImagePauseInSeconds, ScrapeConfig.SearchConfiguration.ImagePauseInSeconds + 4);
            Thread.Sleep(TimeSpan.FromSeconds(delay));
            ScrapeConfig.SearchConfiguration.SubscriptionsStartingPageNumber = currentPageNumber;
            ConfigurationSaver.SaveUpdatedConfiguration();
            Logger.Information("Getting page {subscriptionPage} (of {totalPagesForSubscriptions}) now.", currentPageNumber, ScrapeConfig.SearchConfiguration.SubscriptionsTotalPages);

            var wallpapers = await subscriptionsPage.GetWallpapersAsync(currentPageNumber);
            await imagePageService.ProcessWallpapersAsync(wallpapers);
        }

        if(pageCount > 0) subscriptionsPage.Clear();
    }

    private void UpdateSearchTotalPagesIfRequired(int pageCount)
    {
        if(ScrapeConfig.SearchConfiguration.SubscriptionsTotalPages != pageCount)
            ScrapeConfig.SearchConfiguration.SubscriptionsTotalPages = pageCount;
    }
}
