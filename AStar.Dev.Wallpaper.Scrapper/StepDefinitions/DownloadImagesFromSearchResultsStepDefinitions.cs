using System.Diagnostics;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.Wallpaper.Scrapper.Support;
using Microsoft.Playwright;
using Reqnroll;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scrapper.StepDefinitions;

[Binding]
public class DownloadImagesFromSearchResultsStepDefinitions(
    SearchResultsPage   searchResultsPage,
    ImagePageService    imagePageService,
    SearchConfiguration searchConfiguration,
    ScrapeDirectories   scrapeDirectories,
    ConfigurationSaver  configurationSaver,
    Logger              logger)
{
    private readonly ConfigurationSaver  configurationSaver  = configurationSaver  ?? throw new ArgumentNullException();
    private readonly ImagePageService    imagePageService    = imagePageService    ?? throw new ArgumentNullException();
    private readonly SearchResultsPage   searchResultsPage   = searchResultsPage   ?? throw new ArgumentNullException();
    private          ScrapeDirectories   scrapeDirectories   = scrapeDirectories   ?? throw new ArgumentNullException();
    private          SearchConfiguration searchConfiguration = searchConfiguration ?? throw new ArgumentNullException();

    [Then("I can download the pictures I don't currently have")]
    public async Task ThenICanDownloadThePicturesIDoNotCurrentlyHave()
    {
        try
        {
            List<Category> searchCategories = FilterSearchCategories(searchConfiguration.SearchCategories.ToList());

            await ProcessSearchCategories(searchCategories);
        }
        catch(Exception exception)
        {
            logger.Error(exception.GetBaseException().Message);

            throw;
        }
    }

    private async Task ProcessSearchCategories(List<Category> searchCategories)
    {
        foreach(Category searchCategory in searchCategories)
        {
            var combinedSearchString = $"{searchConfiguration.SearchStringPrefix}{searchCategory.Id}{searchConfiguration.SearchStringSuffix}";

            searchConfiguration = UpdateSearchDetailsIfRequired(combinedSearchString);

            IResponse? pageDetails = await searchResultsPage.LoadSearchPageAsync(combinedSearchString, searchConfiguration.StartingPageNumber);

            if(pageDetails is { Ok: false, }) throw new InvalidOperationException("Could not get the image page after retry...");

            var (pageCount, imageCount, subDirectoryName) = await searchResultsPage.PageInfoAsync();
            UpdateSearchTotalPagesIfRequired(pageCount);

            if(SearchCategoryHasBeenFullyVisited(combinedSearchString, searchCategory, imageCount))
            {
                logger.Debug("{Category} category has been fully visited...", searchCategory.Name);

                continue;
            }

            searchCategory.LastKnownImageCount     = imageCount;
            searchCategory.LastPageVisited         = 1;
            searchConfiguration.StartingPageNumber = 1;

            logger.Debug("Visiting {Category} now...", searchCategory.Name);
            scrapeDirectories = UpdateSubDirectoryIfRequired(subDirectoryName);

            _ = DirectoryHelper.CreateDirectoryIfRequired(Path.Combine(scrapeDirectories.RootDirectory, scrapeDirectories.BaseDirectory, subDirectoryName));

            await ProcessAllCategoryPages(searchCategory, combinedSearchString);
        }
    }

    private async Task ProcessAllCategoryPages(Category searchCategory, string combinedSearchString)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        logger.Debug("About to visit the specific {Category} pages now...", searchCategory.Name);

        for(var currentPageNumber = searchConfiguration.StartingPageNumber; currentPageNumber <= searchConfiguration.TotalPages; currentPageNumber++)
        {
            Thread.Sleep(2_000);
            logger.Debug("About to visit page {page} (of {totalPages}) for {Category} now...", currentPageNumber, searchConfiguration.TotalPages, searchCategory.Name);
            searchConfiguration.StartingPageNumber = currentPageNumber;
            searchCategory.LastPageVisited         = currentPageNumber;
            configurationSaver.SaveUpdatedConfiguration();
            _ = await searchResultsPage.LoadSearchPageAsync(combinedSearchString, currentPageNumber);

            IReadOnlyCollection<string> imagePageLinks = await searchResultsPage.ImagePageLinksAsync();

            await imagePageService.GetTheImagePagesAsync(imagePageLinks);
        }

        stopwatch.Stop();
        logger.Information("Completed visiting the {Category}. Total time: {CategoryVisitDuration}", searchCategory.Name, stopwatch.Elapsed);
    }

    private ScrapeDirectories UpdateSubDirectoryIfRequired(string subDirectoryName)
    {
        if(subDirectoryName.Length > 0) scrapeDirectories.SubDirectoryName = subDirectoryName;

        return scrapeDirectories;
    }

    private SearchConfiguration UpdateSearchDetailsIfRequired(string combinedSearchString)
    {
        if(searchConfiguration.SearchString == combinedSearchString) return searchConfiguration;

        searchConfiguration.StartingPageNumber = 1;
        searchConfiguration.SearchString       = combinedSearchString;

        return searchConfiguration;
    }

    private bool SearchCategoryHasBeenFullyVisited(string combinedSearchString, Category searchCategory, int imageCount)
        => searchConfiguration.SearchString == combinedSearchString && searchCategory.LastKnownImageCount == imageCount;

    private List<Category> FilterSearchCategories(List<Category> searchCategories)
    {
        for(var i = 0; i < searchCategories.Count; i++)
        {
            var combinedSearchString = $"{searchConfiguration.SearchStringPrefix}{searchCategories[i].Id}{searchConfiguration.SearchStringSuffix}";

            if(combinedSearchString != searchConfiguration.SearchString) continue;

            searchCategories = searchCategories.Skip(i).ToList();

            break;
        }

        return searchCategories;
    }

    private void UpdateSearchTotalPagesIfRequired(int pageCount)
    {
        if(searchConfiguration.TotalPages != pageCount) searchConfiguration.TotalPages = pageCount;
    }
}
