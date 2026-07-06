using NSubstitute.ExceptionExtensions;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Repositories;
using AStar.Dev.Wallpaper.Scrapper.Support;
using AStar.Dev.Wallpaper.Scrapper.Tests.Unit.TestData;
using AStar.Dev.Wallpaper.Scrapper.Workflows;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
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

    [Fact]
    public async Task when_the_image_page_service_reports_a_failure_then_run_async_throws_an_invalid_operation_exception()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using (var seedContext = new AppDbContext(options)) await seedContext.Database.MigrateAsync(TestContext.Current.CancellationToken);

        var topWallpapersPage = Substitute.For<ITopWallpapersPage>();
        topWallpapersPage.LoadTopWallpapersPageAsync(Arg.Any<int>()).Returns(Task.FromResult(Result.Success<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>(global::AStar.Dev.FunctionalParadigm.Unit.Value)));
        topWallpapersPage.PageInfoAsync().Returns(Task.FromResult(Result.Success<int, ScrapeError>(1)));
        topWallpapersPage.GetImagePageLinksAsync().Returns(Task.FromResult(Result.Success<IReadOnlyCollection<string>, ScrapeError>(["https://example.test/w/12345",])));

        var searchConfiguration = new SearchConfigurationBuilder { TopWallpapersStartingPageNumber = 1, TopWallpapersTotalPages = 1, }.Build();
        var contextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        contextFactory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(new AppDbContext(options)));
        var configurationSaver = new ConfigurationSaver(new ScrapeConfigurationBuilder { SearchConfiguration = searchConfiguration, }.Build(), new LoggerConfiguration().CreateLogger(), contextFactory);
        var failingPlaywrightService = Substitute.For<IPlaywrightService>();
        failingPlaywrightService.ConfigurePlaywrightAsync().ThrowsAsync(new PlaywrightException("navigation failed"));
        var fileDetailRepository = Substitute.For<IFileDetailRepository>();
        fileDetailRepository.ExistsAsync(Arg.Any<string>()).Returns(false);
        var imagePageService = new ImagePageService(
            new ImagePage(failingPlaywrightService, new ScrapeConfigurationBuilder().Build(), new(), new()),
            fileDetailRepository,
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

        exception.ShouldBeOfType<InvalidOperationException>();
    }
}
