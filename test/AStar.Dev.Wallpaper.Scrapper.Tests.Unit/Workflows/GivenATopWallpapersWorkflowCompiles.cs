using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Repositories;
using AStar.Dev.Wallpaper.Scrapper.Support;
using AStar.Dev.Wallpaper.Scrapper.Tests.Unit.TestData;
using AStar.Dev.Wallpaper.Scrapper.Workflows;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Workflows;

public sealed class GivenATopWallpapersWorkflowCompiles
{
    [Fact]
    public async Task when_run_against_the_result_based_page_contract_then_no_exception_is_thrown()
    {
        var topWallpapersPage = Substitute.For<ITopWallpapersPage>();
        topWallpapersPage.LoadTopWallpapersPageAsync(Arg.Any<int>()).Returns(Task.FromResult(Result.Success<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>(global::AStar.Dev.FunctionalParadigm.Unit.Value)));
        topWallpapersPage.PageInfoAsync().Returns(Task.FromResult(Result.Success<int, ScrapeError>(0)));
        topWallpapersPage.GetImagePageLinksAsync().Returns(Task.FromResult(Result.Success<IReadOnlyCollection<string>, ScrapeError>([])));

        var searchConfiguration = new SearchConfigurationBuilder { TopWallpapersStartingPageNumber = 1, TopWallpapersTotalPages = 0, }.Build();
        var contextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        var configurationSaver = new ConfigurationSaver(new ScrapeConfigurationBuilder { SearchConfiguration = searchConfiguration, }.Build(), new LoggerConfiguration().CreateLogger(), contextFactory);
        var imagePageService = new ImagePageService(
            new ImagePage(Substitute.For<IPlaywrightService>(), new ScrapeConfigurationBuilder().Build(), new(), new()),
            Substitute.For<IFileDetailRepository>(),
            new FileClassificationService(contextFactory),
            new ScrapeConfigurationBuilder().Build(),
            System.TimeProvider.System,
            new LoggerConfiguration().CreateLogger(),
            Substitute.For<IDirectoryHelper>(),
            new(),
            new NoOpDelayStrategy(),
            Substitute.For<IImageRetriever>(),
            Substitute.For<IImageSaver>(),
            new MockFileSystem(),
            Substitute.For<IScrapedTagRepository>(),
            Substitute.For<IImageDimensionReader>());

        var sut = new TopWallpapersWorkflow(topWallpapersPage, imagePageService, searchConfiguration, configurationSaver, new LoggerConfiguration().CreateLogger());

        var exception = await Record.ExceptionAsync(() => sut.RunAsync(TestContext.Current.CancellationToken));

        exception.ShouldBeNull();
    }
}
