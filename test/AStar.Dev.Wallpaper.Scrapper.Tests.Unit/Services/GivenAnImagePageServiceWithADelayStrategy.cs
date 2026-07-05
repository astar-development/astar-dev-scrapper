using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Repositories;
using AStar.Dev.Wallpaper.Scrapper.Support;
using AStar.Dev.Wallpaper.Scrapper.Tests.Unit.TestData;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Serilog;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Services;

public sealed class GivenAnImagePageServiceWithADelayStrategy : IAsyncLifetime
{
    private const string Link = "https://example.test/w/12345";
    private const string CategoryName = "Cars";
    private const string CategoryId = "cat-1";
    private const string ImageUrl = "https://example.test/images/12345.data";

    private SqliteConnection connection = null!;
    private DbContextOptions<AppDbContext> options = null!;

    public async ValueTask InitializeAsync()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var seedContext = new AppDbContext(options);
        await seedContext.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync() => await connection.DisposeAsync();

    private static ImagePage BuildImagePageReturningImage(string imageUrl)
    {
        var tagLocator = Substitute.For<ILocator>();
        tagLocator.InnerTextAsync().Returns(Task.FromResult("Nature"));
        tagLocator.GetAttributeAsync("original-title").Returns(Task.FromResult<string?>("Landscape"));

        var tagsLocator = Substitute.For<ILocator>();
        tagsLocator.AllAsync().Returns(Task.FromResult<IReadOnlyList<ILocator>>([tagLocator,]));

        var imageLocator = Substitute.For<ILocator>();
        imageLocator.GetAttributeAsync("src").Returns(Task.FromResult<string?>(imageUrl));

        var page = Substitute.For<IPage>();
        page.Locator(".tagname", Arg.Any<PageLocatorOptions>()).Returns(tagsLocator);
        page.Locator("#wallpaper", Arg.Any<PageLocatorOptions>()).Returns(imageLocator);

        var playwrightService = Substitute.For<IPlaywrightService>();
        playwrightService.ConfigurePlaywrightAsync().Returns(Task.FromResult(page));

        var scrapeConfiguration = new ScrapeConfigurationBuilder().Build();

        return new ImagePage(playwrightService, scrapeConfiguration, new(), new(), Substitute.For<IScrapedTagRepository>());
    }

    private ImagePageService BuildService(ImagePage imagePage, IFileDetailRepository fileDetailRepository, IDelayStrategy delayStrategy, IImageRetriever imageRetriever, IImageSaver imageSaver, MockFileSystem fileSystem, IDirectoryHelper directoryHelper)
    {
        var contextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        contextFactory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(new AppDbContext(options)));
        var fileClassificationService = new FileClassificationService(contextFactory);
        var scrapeConfiguration = new ScrapeConfigurationBuilder().Build();

        return new ImagePageService(imagePage, fileDetailRepository, fileClassificationService, scrapeConfiguration, System.TimeProvider.System, new LoggerConfiguration().CreateLogger(), directoryHelper, new(), delayStrategy, imageRetriever, imageSaver, fileSystem);
    }

    [Fact]
    public async Task when_the_file_already_exists_then_the_delay_strategy_receives_the_image_already_downloaded_delay()
    {
        var fileDetailRepository = Substitute.For<IFileDetailRepository>();
        fileDetailRepository.ExistsAsync(Arg.Any<string>()).Returns(true);
        var delayStrategy = Substitute.For<IDelayStrategy>();
        delayStrategy.DelayAsync(Arg.Any<DelayKind>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var imagePage = BuildImagePageReturningImage(ImageUrl);

        var sut = BuildService(imagePage, fileDetailRepository, delayStrategy, Substitute.For<IImageRetriever>(), Substitute.For<IImageSaver>(), new MockFileSystem(), Substitute.For<IDirectoryHelper>());

        await sut.GetTheImagePagesAsync([Link,], CategoryId, CategoryName, TestContext.Current.CancellationToken);

        await delayStrategy.Received(1).DelayAsync(DelayKind.ImageAlreadyDownloaded, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_file_does_not_already_exist_then_the_delay_strategy_receives_the_before_image_delay()
    {
        var fileDetailRepository = Substitute.For<IFileDetailRepository>();
        fileDetailRepository.ExistsAsync(Arg.Any<string>()).Returns(false);
        var delayStrategy = Substitute.For<IDelayStrategy>();
        delayStrategy.DelayAsync(Arg.Any<DelayKind>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var imagePage = BuildImagePageReturningImage(ImageUrl);
        var directoryHelper = Substitute.For<IDirectoryHelper>();
        directoryHelper.CreateDirectoryIfRequired(Arg.Any<List<string>>()).Returns(new DirectoryName("/save/dir"));
        var imageRetriever = Substitute.For<IImageRetriever>();
        imageRetriever.GetImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success<byte[], ScrapeError>([1, 2, 3,])));
        var imageSaver = Substitute.For<IImageSaver>();
        var fileSystem = new MockFileSystem();
        fileSystem.Directory.CreateDirectory("/save/dir");
        fileSystem.File.WriteAllBytes("/save/dir/12345.data", [1, 2, 3,]);

        var sut = BuildService(imagePage, fileDetailRepository, delayStrategy, imageRetriever, imageSaver, fileSystem, directoryHelper);

        await sut.GetTheImagePagesAsync([Link,], CategoryId, CategoryName, TestContext.Current.CancellationToken);

        await delayStrategy.Received(1).DelayAsync(DelayKind.BeforeImage, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_image_retriever_always_fails_then_get_the_image_pages_async_retries_exactly_once_before_throwing()
    {
        var fileDetailRepository = Substitute.For<IFileDetailRepository>();
        fileDetailRepository.ExistsAsync(Arg.Any<string>()).Returns(false);
        var delayStrategy = Substitute.For<IDelayStrategy>();
        delayStrategy.DelayAsync(Arg.Any<DelayKind>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var imagePage = BuildImagePageReturningImage(ImageUrl);
        var directoryHelper = Substitute.For<IDirectoryHelper>();
        directoryHelper.CreateDirectoryIfRequired(Arg.Any<List<string>>()).Returns(new DirectoryName("/save/dir"));
        var imageRetriever = Substitute.For<IImageRetriever>();
        imageRetriever.GetImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult(Result.Failure<byte[], ScrapeError>(ScrapeErrorFactory.CreateImageDownloadFailed(ImageUrl, "download failed"))));
        var imageSaver = Substitute.For<IImageSaver>();
        var fileSystem = new MockFileSystem();

        var sut = BuildService(imagePage, fileDetailRepository, delayStrategy, imageRetriever, imageSaver, fileSystem, directoryHelper);

        await Should.ThrowAsync<Exception>(() => sut.GetTheImagePagesAsync([Link,], CategoryId, CategoryName, TestContext.Current.CancellationToken));

        await imageRetriever.Received(2).GetImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_image_retriever_always_fails_then_the_delay_strategy_receives_exactly_one_retry_delay()
    {
        var fileDetailRepository = Substitute.For<IFileDetailRepository>();
        fileDetailRepository.ExistsAsync(Arg.Any<string>()).Returns(false);
        var delayStrategy = Substitute.For<IDelayStrategy>();
        delayStrategy.DelayAsync(Arg.Any<DelayKind>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var imagePage = BuildImagePageReturningImage(ImageUrl);
        var directoryHelper = Substitute.For<IDirectoryHelper>();
        directoryHelper.CreateDirectoryIfRequired(Arg.Any<List<string>>()).Returns(new DirectoryName("/save/dir"));
        var imageRetriever = Substitute.For<IImageRetriever>();
        imageRetriever.GetImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult(Result.Failure<byte[], ScrapeError>(ScrapeErrorFactory.CreateImageDownloadFailed(ImageUrl, "download failed"))));
        var imageSaver = Substitute.For<IImageSaver>();
        var fileSystem = new MockFileSystem();

        var sut = BuildService(imagePage, fileDetailRepository, delayStrategy, imageRetriever, imageSaver, fileSystem, directoryHelper);

        await Should.ThrowAsync<Exception>(() => sut.GetTheImagePagesAsync([Link,], CategoryId, CategoryName, TestContext.Current.CancellationToken));

        await delayStrategy.Received(1).DelayAsync(DelayKind.Retry, Arg.Any<CancellationToken>());
    }
}
