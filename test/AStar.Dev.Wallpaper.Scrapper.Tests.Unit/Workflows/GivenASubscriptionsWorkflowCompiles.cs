using NSubstitute.ExceptionExtensions;
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

public sealed class GivenASubscriptionsWorkflowCompiles
{
    [Fact]
    public async Task when_run_against_the_result_based_page_contract_then_no_exception_is_thrown()
    {
        var headerLocator = Substitute.For<ILocator>();
        headerLocator.TextContentAsync().Returns(Task.FromResult<string?>("0 New Subscription Wallpapers"));
        var linksLocator = Substitute.For<ILocator>();
        linksLocator.AllAsync().Returns(Task.FromResult<IReadOnlyList<ILocator>>([]));
        var page = Substitute.For<IPage>();
        page.GetByText(Arg.Any<string>(), Arg.Any<PageGetByTextOptions>()).Returns(headerLocator);
        page.GetByRole(AriaRole.Link).Returns(linksLocator);
        var response = Substitute.For<IResponse>();
        page.GotoAsync(Arg.Any<string>()).Returns(Task.FromResult<IResponse?>(response));

        var playwrightService = Substitute.For<IPlaywrightService>();
        playwrightService.ConfigurePlaywrightAsync().Returns(Task.FromResult(page));

        var searchConfiguration = new SearchConfigurationBuilder { SubscriptionsStartingPageNumber = 1, SubscriptionsTotalPages = 0, }.Build();
        var scrapeConfiguration = new ScrapeConfigurationBuilder { SearchConfiguration = searchConfiguration, }.Build();
        var subscriptionsImagesListPage = new SubscriptionsImagesListPage(playwrightService, searchConfiguration);

        var contextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        var configurationSaver = new ConfigurationSaver(scrapeConfiguration, new LoggerConfiguration().CreateLogger(), contextFactory);
        var imagePageService = new ImagePageService(
            new ImagePage(playwrightService, scrapeConfiguration, new(), new()),
            Substitute.For<IFileDetailRepository>(),
            new FileClassificationService(contextFactory),
            scrapeConfiguration,
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

        var sut = new SubscriptionsWorkflow(subscriptionsImagesListPage, imagePageService, searchConfiguration, scrapeConfiguration.ScrapeDirectories, configurationSaver, new LoggerConfiguration().CreateLogger());

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

        var headerLocator = Substitute.For<ILocator>();
        headerLocator.TextContentAsync().Returns(Task.FromResult<string?>("1 New Subscription Wallpapers"));
        var linkLocator = Substitute.For<ILocator>();
        linkLocator.GetAttributeAsync("href").Returns(Task.FromResult<string?>("https://example.test/w/12345"));
        var linksLocator = Substitute.For<ILocator>();
        linksLocator.AllAsync().Returns(Task.FromResult<IReadOnlyList<ILocator>>([linkLocator,]));
        var page = Substitute.For<IPage>();
        page.GetByText(Arg.Any<string>(), Arg.Any<PageGetByTextOptions>()).Returns(headerLocator);
        page.GetByRole(AriaRole.Link).Returns(linksLocator);
        var response = Substitute.For<IResponse>();
        page.GotoAsync(Arg.Any<string>()).Returns(Task.FromResult<IResponse?>(response));

        var playwrightService = Substitute.For<IPlaywrightService>();
        playwrightService.ConfigurePlaywrightAsync().Returns(Task.FromResult(page));

        var searchConfiguration = new SearchConfigurationBuilder { SubscriptionsStartingPageNumber = 1, SubscriptionsTotalPages = 1, }.Build();
        var scrapeConfiguration = new ScrapeConfigurationBuilder { SearchConfiguration = searchConfiguration, }.Build();
        var subscriptionsImagesListPage = new SubscriptionsImagesListPage(playwrightService, searchConfiguration);

        var contextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        contextFactory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(new AppDbContext(options)));
        var configurationSaver = new ConfigurationSaver(scrapeConfiguration, new LoggerConfiguration().CreateLogger(), contextFactory);
        var failingPlaywrightService = Substitute.For<IPlaywrightService>();
        failingPlaywrightService.ConfigurePlaywrightAsync().ThrowsAsync(new PlaywrightException("navigation failed"));
        var fileDetailRepository = Substitute.For<IFileDetailRepository>();
        fileDetailRepository.ExistsAsync(Arg.Any<string>()).Returns(false);
        var imagePageService = new ImagePageService(
            new ImagePage(failingPlaywrightService, scrapeConfiguration, new(), new()),
            fileDetailRepository,
            new FileClassificationService(contextFactory),
            scrapeConfiguration,
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

        var sut = new SubscriptionsWorkflow(subscriptionsImagesListPage, imagePageService, searchConfiguration, scrapeConfiguration.ScrapeDirectories, configurationSaver, new LoggerConfiguration().CreateLogger());

        var exception = await Record.ExceptionAsync(() => sut.RunAsync(TestContext.Current.CancellationToken));

        exception.ShouldBeOfType<InvalidOperationException>();
    }
}
