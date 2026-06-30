using AStar.Dev.Wallpaper.Scrapper.Repositories;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public sealed class DatabaseResetService(IDatabaseResetRepository repository) : IDatabaseResetService
{
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await repository.ResetSearchCategoriesAsync(cancellationToken).ConfigureAwait(false);
        await repository.DeleteAllFilesAsync(cancellationToken).ConfigureAwait(false);
    }
}
