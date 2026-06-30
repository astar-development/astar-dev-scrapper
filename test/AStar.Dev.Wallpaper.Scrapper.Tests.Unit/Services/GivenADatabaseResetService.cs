using AStar.Dev.Wallpaper.Scrapper.Repositories;
using AStar.Dev.Wallpaper.Scrapper.Services;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Services;

public sealed class GivenADatabaseResetService
{
    private readonly IDatabaseResetRepository repo = Substitute.For<IDatabaseResetRepository>();
    private readonly DatabaseResetService sut;

    public GivenADatabaseResetService() =>
        sut = new DatabaseResetService(repo);

    [Fact]
    public async Task when_resetting_then_reset_search_categories_is_called()
    {
        await sut.ResetAsync(CancellationToken.None);

        await repo.Received(1).ResetSearchCategoriesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_resetting_then_delete_all_files_is_called()
    {
        await sut.ResetAsync(CancellationToken.None);

        await repo.Received(1).DeleteAllFilesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_resetting_then_reset_search_categories_is_called_before_delete_all_files()
    {
        var callOrder = new List<string>();
        repo.When(r => r.ResetSearchCategoriesAsync(Arg.Any<CancellationToken>()))
            .Do(_ => callOrder.Add("categories"));
        repo.When(r => r.DeleteAllFilesAsync(Arg.Any<CancellationToken>()))
            .Do(_ => callOrder.Add("files"));

        await sut.ResetAsync(CancellationToken.None);

        callOrder.ShouldBe(["categories", "files"]);
    }

    [Fact]
    public async Task when_reset_search_categories_throws_then_delete_all_files_is_not_called()
    {
        repo.ResetSearchCategoriesAsync(Arg.Any<CancellationToken>())
            .Returns<Task>(_ => Task.FromException(new InvalidOperationException()));

        await Should.ThrowAsync<InvalidOperationException>(() => sut.ResetAsync(CancellationToken.None));

        await repo.DidNotReceive().DeleteAllFilesAsync(Arg.Any<CancellationToken>());
    }
}
