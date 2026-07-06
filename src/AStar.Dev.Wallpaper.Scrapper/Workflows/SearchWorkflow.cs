using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Utilities;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.Wallpaper.Scrapper.Support;
using Serilog;

namespace AStar.Dev.Wallpaper.Scrapper.Workflows;

public sealed class SearchWorkflow(SearchResultsPage searchResultsPage, ScrapeConfiguration injectedScrapeConfiguration, ConfigurationSaver configurationSaver, ImagePageService imagePageService, IDirectoryHelper directoryHelper, ILogger logger, IDelayStrategy delayStrategy, TimeProvider timeProvider)
{
    private SearchProgress progress = null!;

    public async Task<Result<Unit, string>> RunAsync(ILogger scrapeLogger, CancellationToken ct = default)
    {
        try
        {
            progress = SearchProgressFactory.Create(injectedScrapeConfiguration.SearchConfiguration, injectedScrapeConfiguration.ScrapeDirectories);
            var searchCategories = SearchProgressFunctions.FilterSearchCategories(progress.SearchConfiguration, progress.SearchConfiguration.SearchCategories);
            await ProcessSearchCategoriesAsync(searchCategories, scrapeLogger, ct);

            return Unit.Value;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            scrapeLogger.Error(exception.GetBaseException().Message);
            throw;
        }
    }

    private async Task ProcessSearchCategoriesAsync(IReadOnlyList<Category> searchCategories, ILogger scrapeLogger, CancellationToken ct)
    {
        foreach (var searchCategory in searchCategories)
        {
            ct.ThrowIfCancellationRequested();
            string combinedSearchString = $"{progress.SearchConfiguration.SearchStringPrefix}{searchCategory.Id}{progress.SearchConfiguration.SearchStringSuffix}";

            progress = SearchProgressFunctions.UpdateSearchDetails(progress, combinedSearchString);

            var pageDetails = await searchResultsPage.LoadSearchPageAsync(combinedSearchString, progress.SearchConfiguration.StartingPageNumber);

            if (pageDetails is { Ok: false, }) throw new InvalidOperationException("Could not get the image page after retry...");

            var (pageCount, imageCount, subDirectoryName) = await searchResultsPage.PageInfoAsync();
            progress = SearchProgressFunctions.UpdateTotalPages(progress, pageCount);

            if (searchCategory.IsUpToDate(imageCount, pageCount))
            {
                logger.Information("{Category} is up to date (same image/page count), skipping...", searchCategory.Name);
                await delayStrategy.DelayAsync(DelayKind.CategoryUpToDate, ct).ConfigureAwait(false);
                continue;
            }

            int startingPage = searchCategory.LastPageVisited > 0 ? searchCategory.LastPageVisited : 1;
            progress = progress with { SearchConfiguration = progress.SearchConfiguration with { StartingPageNumber = startingPage, }, };

            logger.Debug("Visiting {Category} from page {StartingPage} now...", searchCategory.Name, startingPage);
            progress = SearchProgressFunctions.UpdateSubDirectory(progress, subDirectoryName);

            _ = directoryHelper.CreateDirectoryIfRequired([progress.ScrapeDirectories.RootDirectory.CombinePath(progress.ScrapeDirectories.BaseDirectory, subDirectoryName),]);

            await ProcessAllCategoryPagesAsync(searchCategory, combinedSearchString, scrapeLogger, ct);

            searchCategory.LastKnownImageCount = imageCount;
            searchCategory.TotalPages = pageCount;
            searchCategory.LastPageVisited = 0;
            await configurationSaver.SaveUpdatedConfigurationAsync();
        }
    }

    private async Task ProcessAllCategoryPagesAsync(Category searchCategory, string combinedSearchString, ILogger scrapeLogger, CancellationToken ct)
    {
        long startTimestamp = timeProvider.GetTimestamp();
        scrapeLogger.Debug("About to visit the specific {Category} pages now...", searchCategory.Name);

        for (int currentPageNumber = progress.SearchConfiguration.StartingPageNumber; currentPageNumber <= progress.SearchConfiguration.TotalPages; currentPageNumber++)
        {
            await delayStrategy.DelayAsync(DelayKind.PageNavigation, ct).ConfigureAwait(false);
            scrapeLogger.Debug("About to visit page {page} (of {totalPages}) for {Category} now...", currentPageNumber, progress.SearchConfiguration.TotalPages, searchCategory.Name);
            progress = progress with { SearchConfiguration = progress.SearchConfiguration with { StartingPageNumber = currentPageNumber, }, };
            searchCategory.LastPageVisited = currentPageNumber;
            await configurationSaver.SaveUpdatedConfigurationAsync();
            _ = await searchResultsPage.LoadSearchPageAsync(combinedSearchString, currentPageNumber);

            var imagePageLinks = await searchResultsPage.ImagePageLinksAsync();
            await imagePageService.GetTheImagePagesAsync(imagePageLinks, searchCategory.Id, searchCategory.Name, ct);
        }

        scrapeLogger.Information("Completed visiting the {Category}. Total time: {CategoryVisitDuration}", searchCategory.Name, timeProvider.GetElapsedTime(startTimestamp));
    }
}
