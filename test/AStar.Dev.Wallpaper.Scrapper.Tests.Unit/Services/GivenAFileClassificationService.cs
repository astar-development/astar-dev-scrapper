using AStar.Dev.Infrastructure.FilesDb.Data;
using AStar.Dev.Infrastructure.FilesDb.Models;
using AStar.Dev.Wallpaper.Scrapper.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Services;

public sealed class GivenAFileClassificationService : IAsyncLifetime
{
    private SqliteConnection connection = null!;
    private DbContextOptions<FilesContext> options = null!;
    private IDbContextFactory<FilesContext> factory = null!;
    private FileClassificationService sut = null!;

    public async ValueTask InitializeAsync()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        options = new DbContextOptionsBuilder<FilesContext>()
            .UseSqlite(connection)
            .Options;

        await using var seedContext = new FilesContext(options);
        await seedContext.Database.MigrateAsync();

        seedContext.ScrapeConfiguration.Add(CreateScrapeConfigEntity());
        await seedContext.SaveChangesAsync();

        seedContext.ScrapeConfiguration.Add(CreateScrapeConfigEntity());
        await seedContext.SaveChangesAsync();

        factory = Substitute.For<IDbContextFactory<FilesContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>())
               .Returns(_ => Task.FromResult(new FilesContext(options)));

        sut = new FileClassificationService(factory, new FakeTimeProvider());
    }

    public async ValueTask DisposeAsync() => await connection.DisposeAsync();

    [Fact]
    public async Task when_classifying_with_multiple_search_configuration_rows_and_no_matching_category_then_no_classifications_are_recorded()
    {
        var fileDetail = new FileDetail
        {
            FileName      = new FileName("test.jpg"),
            DirectoryName = new DirectoryName("/tmp")
        };

        await sut.ClassifyAsync(fileDetail, "any-category", [], TestContext.Current.CancellationToken);

        await using var verifyCtx = new FilesContext(options);
        var count = await verifyCtx.DownloadedFileClassifications.CountAsync(TestContext.Current.CancellationToken);

        count.ShouldBe(0);
    }

    private static ScrapeConfigurationEntity CreateScrapeConfigEntity() => new()
    {
        ConnectionStrings   = new ConnectionStrings   { Sqlite = "Data Source=test.db" },
        UserConfiguration   = new UserConfiguration   { LoginEmailAddress = "user@example.com", Username = "user", Password = "password", SessionCookie = "cookie" },
        SearchConfiguration = new SearchConfiguration { BaseUrl = "https://example.com" },
        ScrapeDirectories   = new ScrapeDirectories   { RootDirectory = "/tmp" }
    };
}
