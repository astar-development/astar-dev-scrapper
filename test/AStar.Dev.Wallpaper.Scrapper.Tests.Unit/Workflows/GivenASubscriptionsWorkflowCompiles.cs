using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Repositories;
using AStar.Dev.Wallpaper.Scrapper.Support;
using AStar.Dev.Wallpaper.Scrapper.Tests.Unit.TestData;
using AStar.Dev.Wallpaper.Scrapper.Workflows;
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
}
