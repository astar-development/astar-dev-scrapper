using AStar.Dev.Infrastructure.FilesDb.Data;
using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public sealed class FileClassificationService(IDbContextFactory<FilesContext> contextFactory)
{
    public async Task ClassifyAsync(FileDetail fileDetail, string categoryId, IReadOnlyList<string> imageTags)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var matched = new List<FileClassification>();

        await CollectFileNameMatchesAsync(context, fileDetail, matched);
        await CollectCategoryMatchAsync(context, categoryId, matched);
        await CollectTagMatchesAsync(context, imageTags, matched);

        var distinct = matched.GroupBy(c => c.Name).Select(g => g.First()).ToList();

        await context.SaveChangesAsync();

        foreach(var classification in distinct)
            context.DownloadedFileClassifications.Add(new DownloadedFileClassification
            {
                FileDetailId         = fileDetail.Id,
                FileClassificationId = classification.Id
            });

        await context.SaveChangesAsync();
    }

    private static async Task CollectFileNameMatchesAsync(FilesContext context, FileDetail fileDetail, List<FileClassification> matched)
    {
        var searchable = await context.FileClassifications
            .Include(fc => fc.FileNameParts)
            .Where(fc => fc.IncludeInSearch)
            .ToListAsync();

        matched.AddRange(searchable.Where(fc =>
            fc.FileNameParts.Any(fnp => fileDetail.FullNameWithPath.Contains(fnp.Text, StringComparison.OrdinalIgnoreCase))));
    }

    private static async Task CollectCategoryMatchAsync(FilesContext context, string categoryId, List<FileClassification> matched)
    {
        if(string.IsNullOrEmpty(categoryId)) return;

        var searchConfig = await context.SearchConfigurations
            .Include(sc => sc.SearchCategories)
            .SingleAsync();

        var category = searchConfig.SearchCategories.FirstOrDefault(c => c.Id == categoryId && c.IncludeInSearch);
        if(category is null) return;

        matched.Add(await FindOrCreateClassificationAsync(context, category.Name));
    }

    private static async Task CollectTagMatchesAsync(FilesContext context, IReadOnlyList<string> imageTags, List<FileClassification> matched)
    {
        if(imageTags.Count == 0) return;

        var lowerImageTags = imageTags.Select(t => t.ToLowerInvariant()).ToList();
        var tags = await context.ScrapedTags
            .Where(t => t.IncludeInSearch && lowerImageTags.Contains(t.Value.ToLower()))
            .ToListAsync();

        foreach(var tag in tags)
            matched.Add(await FindOrCreateClassificationAsync(context, tag.Value));
    }

    private static async Task<FileClassification> FindOrCreateClassificationAsync(FilesContext context, string name)
    {
        var tracked = context.ChangeTracker.Entries<FileClassification>()
            .FirstOrDefault(e => e.Entity.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Entity;
        if(tracked is not null) return tracked;

        var existing = await context.FileClassifications.FirstOrDefaultAsync(fc => fc.Name.ToLower() == name.ToLower());
        if(existing is not null) return existing;

        var created = new FileClassification { Name = name };
        context.FileClassifications.Add(created);

        return created;
    }
}
