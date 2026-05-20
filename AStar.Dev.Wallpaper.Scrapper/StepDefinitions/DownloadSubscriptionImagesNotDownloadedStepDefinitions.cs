using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.Wallpaper.Scrapper.Support;
using Microsoft.Playwright;
using Reqnroll;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scrapper.StepDefinitions;

[Binding]
public sealed class DownloadSubscriptionImagesNotDownloadedStepDefinitions(
    SubscriptionsImagesListPage subscriptionsImagesListPage,
    ImagePageService            imagePageService,
    SearchConfiguration         searchConfiguration,
    ScrapeDirectories           scrapeDirectories,
    ConfigurationSaver          configurationSaver,
    Logger                      logger)
{
    [Then("I can download the new Subscription files")]
    public async Task ThenICanDownloadTheNewSubscriptionFiles()
    {
        try
        {
            await GetTheNewSubscriptionImagesAsync();
        }
        catch(Exception exception)
        {
            logger.Error(exception.GetBaseException().Message);

            throw;
        }
    }

    private async Task GetTheNewSubscriptionImagesAsync()
    {
        searchConfiguration.SubscriptionsStartingPageNumber = 1;
        IResponse? pageDetails = await subscriptionsImagesListPage.LoadSubscriptionResultsPageAsync(searchConfiguration.SubscriptionsStartingPageNumber);

        if(pageDetails is { Ok: false, }) _ = await subscriptionsImagesListPage.LoadSubscriptionResultsPageAsync(1);

        var (pageCount, subDirectoryName) = await subscriptionsImagesListPage.PageInfoAsync();

        if(subDirectoryName.Length > 0) scrapeDirectories.SubDirectoryName = subDirectoryName;

        _ = DirectoryHelper.CreateDirectoryIfRequired(Path.Combine(scrapeDirectories.RootDirectory, scrapeDirectories.BaseDirectory, subDirectoryName));
        UpdateSearchTotalPagesIfRequired(pageCount);

        configurationSaver.SaveUpdatedConfiguration();

        for(var currentPageNumber = searchConfiguration.SubscriptionsStartingPageNumber;
            currentPageNumber <= searchConfiguration.SubscriptionsTotalPages;
            currentPageNumber++)
        {
            var delay = Random.Shared.Next(searchConfiguration.ImagePauseInSeconds, searchConfiguration.ImagePauseInSeconds + 4);
            Thread.Sleep(TimeSpan.FromSeconds(delay));
            searchConfiguration.SubscriptionsStartingPageNumber = currentPageNumber;
            configurationSaver.SaveUpdatedConfiguration();
            logger.Information("Getting page {subscriptionPage} (of {totalPagesForSubscriptions}) now.", currentPageNumber, searchConfiguration.SubscriptionsTotalPages);
            _ = await subscriptionsImagesListPage.LoadSubscriptionResultsPageAsync(currentPageNumber);
            IReadOnlyCollection<string> imagePageLinks = await subscriptionsImagesListPage.GetImagePageLinks();

            await imagePageService.GetTheImagePagesAsync(imagePageLinks);
        }

        if(pageCount > 0)
        {
            _ = await subscriptionsImagesListPage.LoadSubscriptionResultsPageAsync(1);
            await subscriptionsImagesListPage.Clear();
        }
    }

    private void UpdateSearchTotalPagesIfRequired(int pageCount)
    {
        if(searchConfiguration.SubscriptionsTotalPages != pageCount) searchConfiguration.SubscriptionsTotalPages = pageCount;
    }
}
