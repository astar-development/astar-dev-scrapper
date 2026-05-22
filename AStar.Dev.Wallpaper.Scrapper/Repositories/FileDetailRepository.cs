using AStar.Dev.Infrastructure.FilesDb.Data;
using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scrapper.Repositories;

public sealed class FileDetailRepository(string connectionString)
{
    public async Task<bool> ExistsAsync(string fileName)
    {
        var options = new DbContextOptionsBuilder<FilesContext>()
            .UseSqlite(connectionString)
            .Options;
        await using var context = new FilesContext(options);
        return await context.Files.FirstOrDefaultAsync(f => f.FileName.Value.Contains(fileName)) != null;
    }

    public async Task AddAsync(FileDetail fileDetail)
    {
        var options = new DbContextOptionsBuilder<FilesContext>()
            .UseSqlite(connectionString)
            .Options;
        await using var context = new FilesContext(options);

        var handle = FileHandle.Create(fileDetail.FileName.Value ?? fileDetail.FileHandle.Value);
        var existingCount = await context.Files.AsAsyncEnumerable().CountAsync(f => f.FileHandle.Value == handle.Value);
        if(existingCount > 0)
            handle = FileHandle.Create($"{handle}-{++existingCount}");
        fileDetail.FileHandle = handle;

        _ = await context.Files.AddAsync(fileDetail);
        _ = await context.SaveChangesAsync();
    }
}
