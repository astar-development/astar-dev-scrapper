using AStar.Dev.Infrastructure.FilesDb.Data;
using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scrapper.Repositories;

public sealed class FileDetailRepository(string connectionString) : IFileDetailRepository
{
    private DbContextOptions<FilesContext> CreateOptions() =>
        new DbContextOptionsBuilder<FilesContext>().UseSqlite(connectionString).Options;

    public async Task<bool> ExistsAsync(string fileName)
    {
        await using var context = new FilesContext(CreateOptions());
        return await context.Files.FirstOrDefaultAsync(f => f.FileName.Value.Contains(fileName)) != null;
    }

    public async Task AddAsync(FileDetail fileDetail)
    {
        await using var context = new FilesContext(CreateOptions());

        var handle = FileHandle.Create(fileDetail.FileName.Value ?? fileDetail.FileHandle.Value);
        var existingCount = await context.Files.CountAsync(f => f.FileHandle.Value == handle.Value);
        if(existingCount > 0)
            handle = FileHandle.Create($"{handle}-{++existingCount}");
        fileDetail.FileHandle = handle;

        _ = await context.Files.AddAsync(fileDetail);
        _ = await context.SaveChangesAsync();
    }
}
