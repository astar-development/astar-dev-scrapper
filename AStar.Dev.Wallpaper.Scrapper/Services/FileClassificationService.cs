using AStar.Dev.Infrastructure.FilesDb.Data;
using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public sealed class FileClassificationService(IDbContextFactory<FilesContext> contextFactory)
{
    public async Task ClassifyAsync(FileDetail fileDetail)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var classifications = await context.FileClassifications
            .Include(fc => fc.FileNameParts)
            .ToListAsync();

        var matchingClassifications = classifications
            .Where(fc => fc.FileNameParts.Any(fnp => fileDetail.FullNameWithPath.Contains(fnp.Text, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if(matchingClassifications.Count == 0) return;

        var trackedFile = await context.Files
            .Include(f => f.FileClassifications)
            .FirstAsync(f => f.Id == fileDetail.Id);

        foreach(var classification in matchingClassifications)
            trackedFile.FileClassifications.Add(classification);

        await context.SaveChangesAsync();
    }
}
