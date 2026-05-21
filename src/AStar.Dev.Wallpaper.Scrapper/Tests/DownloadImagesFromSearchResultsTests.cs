using System.Diagnostics;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Support;

namespace AStar.Dev.Wallpaper.Scrapper.Tests;

public sealed class DownloadImagesFromSearchResultsTests : WallpaperScrapperTestBase
{
    [Xunit.Fact]
    public async Task ICanDownloadTheImagesIDoNotHaveAlready()
    {
        var searchResultsPage = new SearchResultsPage(ApiClient, Logger);
        var imagePageService  = CreateImagePageService();

        try
        {
            var searchCategories = FilterSearchCategories(ScrapeConfig.SearchConfiguration.SearchCategories.ToList());
            await ProcessSearchCategoriesAsync(searchResultsPage, imagePageService, searchCategories);
        }
        catch(Exception exception)
        {
            Logger.Error(exception.GetBaseException().Message);

            throw;
        }
    }

    private async Task ProcessSearchCategoriesAsync(SearchResultsPage searchResultsPage, Services.ImagePageService imagePageService, List<Category> searchCategories)
    {
        foreach(Category searchCategory in searchCategories)
        {
            var combinedSearchString = $"{ScrapeConfig.SearchConfiguration.SearchStringPrefix}{searchCategory.Id}{ScrapeConfig.SearchConfiguration.SearchStringSuffix}";

            UpdateSearchDetailsIfRequired(combinedSearchString);

            var loaded = await searchResultsPage.LoadSearchPageAsync(combinedSearchString, ScrapeConfig.SearchConfiguration.StartingPageNumber);

            if(!loaded) throw new InvalidOperationException("Could not load the search page after retry...");

            var (pageCount, imageCount, subDirectoryName) = searchResultsPage.PageInfo();
            UpdateSearchTotalPagesIfRequired(pageCount);

            if(SearchCategoryHasBeenFullyVisited(combinedSearchString, searchCategory, imageCount))
            {
                Logger.Debug("{Category} category has been fully visited...", searchCategory.Name);

                continue;
            }

            searchCategory.LastKnownImageCount                  = imageCount;
            searchCategory.LastPageVisited                      = 1;
            ScrapeConfig.SearchConfiguration.StartingPageNumber = 1;

            Logger.Debug("Visiting {Category} now...", searchCategory.Name);
            UpdateSubDirectoryIfRequired(subDirectoryName);

            _ = DirectoryHelper.CreateDirectoryIfRequired(
                Path.Combine(ScrapeConfig.ScrapeDirectories.RootDirectory,
                             ScrapeConfig.ScrapeDirectories.BaseDirectory,
                             subDirectoryName));

            await ProcessAllCategoryPagesAsync(searchResultsPage, imagePageService, searchCategory, combinedSearchString);
        }
    }

    private async Task ProcessAllCategoryPagesAsync(SearchResultsPage searchResultsPage, Services.ImagePageService imagePageService, Category searchCategory, string combinedSearchString)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        Logger.Debug("About to visit the specific {Category} pages now...", searchCategory.Name);

        for(var currentPageNumber = ScrapeConfig.SearchConfiguration.StartingPageNumber;
            currentPageNumber <= ScrapeConfig.SearchConfiguration.TotalPages;
            currentPageNumber++)
        {
            Thread.Sleep(2_000);
            Logger.Debug("About to visit page {page} (of {totalPages}) for {Category} now...", currentPageNumber, ScrapeConfig.SearchConfiguration.TotalPages, searchCategory.Name);
            ScrapeConfig.SearchConfiguration.StartingPageNumber = currentPageNumber;
            searchCategory.LastPageVisited                      = currentPageNumber;
            ConfigurationSaver.SaveUpdatedConfiguration();
            _ = await searchResultsPage.LoadSearchPageAsync(combinedSearchString, currentPageNumber);

            var wallpapers = searchResultsPage.GetWallpapers();
            await imagePageService.ProcessWallpapersAsync(wallpapers);
        }

        stopwatch.Stop();
        Logger.Information("Completed visiting the {Category}. Total time: {CategoryVisitDuration}", searchCategory.Name, stopwatch.Elapsed);
    }

    private void UpdateSubDirectoryIfRequired(string subDirectoryName)
    {
        if(subDirectoryName.Length > 0) ScrapeConfig.ScrapeDirectories.SubDirectoryName = subDirectoryName;
    }

    private void UpdateSearchDetailsIfRequired(string combinedSearchString)
    {
        if(ScrapeConfig.SearchConfiguration.SearchString == combinedSearchString) return;

        ScrapeConfig.SearchConfiguration.StartingPageNumber = 1;
        ScrapeConfig.SearchConfiguration.SearchString       = combinedSearchString;
    }

    private bool SearchCategoryHasBeenFullyVisited(string combinedSearchString, Category searchCategory, int imageCount)
        => ScrapeConfig.SearchConfiguration.SearchString == combinedSearchString && searchCategory.LastKnownImageCount == imageCount;

    private List<Category> FilterSearchCategories(List<Category> searchCategories)
    {
        for(var i = 0; i < searchCategories.Count; i++)
        {
            var combinedSearchString = $"{ScrapeConfig.SearchConfiguration.SearchStringPrefix}{searchCategories[i].Id}{ScrapeConfig.SearchConfiguration.SearchStringSuffix}";

            if(combinedSearchString != ScrapeConfig.SearchConfiguration.SearchString) continue;

            searchCategories = searchCategories.Skip(i).ToList();

            break;
        }

        return searchCategories;
    }

    private void UpdateSearchTotalPagesIfRequired(int pageCount)
    {
        if(ScrapeConfig.SearchConfiguration.TotalPages != pageCount) ScrapeConfig.SearchConfiguration.TotalPages = pageCount;
    }
}
