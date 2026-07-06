using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.Wallpaper.Scrapper.Support;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scrapper.Workflows;

public sealed class SubscriptionsWorkflow(
    SubscriptionsImagesListPage subscriptionsImagesListPage,
    ImagePageService imagePageService,
    SearchConfiguration searchConfiguration,
    ScrapeDirectories scrapeDirectories,
    ConfigurationSaver configurationSaver,
    Logger logger)
{
    private SearchConfiguration _searchConfiguration = searchConfiguration;
    private ScrapeDirectories _scrapeDirectories = scrapeDirectories;

    public async Task RunAsync(CancellationToken ct = default)
    {
        try
        {
            await GetTheNewSubscriptionImagesAsync(ct).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.Error(exception.GetBaseException().Message);
            throw;
        }
    }

    private async Task GetTheNewSubscriptionImagesAsync(CancellationToken ct)
    {
        _searchConfiguration = _searchConfiguration with { SubscriptionsStartingPageNumber = 1 };
        var pageDetails = await subscriptionsImagesListPage.LoadSubscriptionResultsPageAsync(_searchConfiguration.SubscriptionsStartingPageNumber).ConfigureAwait(false);
        var loadedSuccessfully = pageDetails.Match(_ => true, _ => false);

        if (!loadedSuccessfully) _ = await subscriptionsImagesListPage.LoadSubscriptionResultsPageAsync(1).ConfigureAwait(false);

        var pageInfo = (await subscriptionsImagesListPage.PageInfoAsync().ConfigureAwait(false))
            .Match(info => info, error => throw new InvalidOperationException(error.Message));

        if (pageInfo.SubDirectoryName.Length > 0) _scrapeDirectories = _scrapeDirectories with { SubDirectoryName = pageInfo.SubDirectoryName };

        UpdateSearchTotalPagesIfRequired(pageInfo.PageCount);

        await configurationSaver.SaveUpdatedConfigurationAsync().ConfigureAwait(false);

        for (int currentPageNumber = _searchConfiguration.SubscriptionsStartingPageNumber;
            currentPageNumber <= _searchConfiguration.SubscriptionsTotalPages;
            currentPageNumber++)
        {
            ct.ThrowIfCancellationRequested();
            int delay = Random.Shared.Next(_searchConfiguration.ImagePauseInSeconds, _searchConfiguration.ImagePauseInSeconds + 4);
            await Task.Delay(TimeSpan.FromSeconds(delay), ct).ConfigureAwait(false);
            _searchConfiguration = _searchConfiguration with { SubscriptionsStartingPageNumber = currentPageNumber };
            await configurationSaver.SaveUpdatedConfigurationAsync().ConfigureAwait(false);
            logger.Information("Getting page {subscriptionPage} (of {totalPagesForSubscriptions}) now.", currentPageNumber, _searchConfiguration.SubscriptionsTotalPages);
            _ = await subscriptionsImagesListPage.LoadSubscriptionResultsPageAsync(currentPageNumber).ConfigureAwait(false);

            var imagePageLinks = (await subscriptionsImagesListPage.GetImagePageLinksAsync().ConfigureAwait(false))
                .Match(links => links, error => throw new InvalidOperationException(error.Message));

            _ = (await imagePageService.GetTheImagePagesAsync(imagePageLinks, "", pageInfo.SubDirectoryName, ct: ct).ConfigureAwait(false))
                .Match(unit => unit, error => throw new InvalidOperationException(error.Message));
        }

        if (pageInfo.PageCount > 0)
        {
            _ = await subscriptionsImagesListPage.LoadSubscriptionResultsPageAsync(1).ConfigureAwait(false);
            _ = await subscriptionsImagesListPage.ClearAsync().ConfigureAwait(false);
        }
    }

    private void UpdateSearchTotalPagesIfRequired(int pageCount)
    {
        if (_searchConfiguration.SubscriptionsTotalPages != pageCount) _searchConfiguration = _searchConfiguration with { SubscriptionsTotalPages = pageCount };
    }
}
