using AStar.Dev.Wallpaper.Scrapper.Repositories;
using AStar.Dev.Wallpaper.Scrapper.Services;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Services;

public sealed class GivenADatabaseResetService
{
    [Fact]
    public async Task when_resetting_then_reset_search_categories_is_called()
    {
        var repo = Substitute.For<IDatabaseResetRepository>();
        var sut  = new DatabaseResetService(repo);

        await sut.ResetAsync(CancellationToken.None);

        await repo.Received(1).ResetSearchCategoriesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_resetting_then_delete_all_files_is_called()
    {
        var repo = Substitute.For<IDatabaseResetRepository>();
        var sut  = new DatabaseResetService(repo);

        await sut.ResetAsync(CancellationToken.None);

        await repo.Received(1).DeleteAllFilesAsync(Arg.Any<CancellationToken>());
    }
}
