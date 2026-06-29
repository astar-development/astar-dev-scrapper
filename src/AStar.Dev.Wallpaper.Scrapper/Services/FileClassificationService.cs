using AStar.Dev.Infrastructure.FilesDb.Data;
using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public sealed class FileClassificationService(IDbContextFactory<FilesContext> contextFactory, TimeProvider timeProvider)
{
    public async Task ClassifyAsync(FileDetail fileDetail, string categoryId, IReadOnlyList<string> imageTags, CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);

        var matched = new List<FileClassification>();

        await CollectFileNameMatchesAsync(context, fileDetail, matched, token);
        await CollectCategoryMatchAsync(context, categoryId, matched, token);
        await CollectTagMatchesAsync(context, imageTags, matched, token);

        var distinct = matched.GroupBy(c => c.Name).Select(g => g.First()).ToList();

        await context.SaveChangesAsync(token);

        foreach (var classification in distinct)
            context.DownloadedFileClassifications.Add(new DownloadedFileClassification
            {
                FileDetailId = fileDetail.Id,
                FileClassificationId = classification.Id
            });

        await context.SaveChangesAsync(token);
    }

    internal async Task<List<FileClassification>> ExportClassificationsAsync(CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);
        var classifications = await context.FileClassifications
            .Include(fc => fc.FileNameParts)
            .ToListAsync(token);

        return classifications;
    }

    internal async Task<object> ImportClassificationsAsync(List<FileClassification> classifications, CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);

        foreach (var classification in classifications)
        {
            var existing = await context.FileClassifications
                .Include(fc => fc.FileNameParts)
                .FirstOrDefaultAsync(fc => fc.Name == classification.Name, token);

            if (existing is null)
            {
                classification.CreatedAt = timeProvider.GetUtcNow();
                classification.UpdatedAt = timeProvider.GetUtcNow();
                context.FileClassifications.Add(classification);
            }
            else
            {
                existing.IncludeInSearch = classification.IncludeInSearch;
                existing.UpdatedAt = timeProvider.GetUtcNow();

                var existingParts = existing.FileNameParts.ToList();
                foreach (var part in classification.FileNameParts)
                {
                    if (!existingParts.Any(ep => ep.Text.Equals(part.Text, StringComparison.OrdinalIgnoreCase)))
                    {
                        existing.FileNameParts.Add(new FileNamePart { Text = part.Text });
                    }
                }
            }
        }

        await context.SaveChangesAsync(token);
        return new { Success = true, Count = classifications.Count };
    }

    private static async Task CollectFileNameMatchesAsync(FilesContext context, FileDetail fileDetail, List<FileClassification> matched, CancellationToken token)
    {
        var searchable = await context.FileClassifications
            .Include(fc => fc.FileNameParts)
            .Where(fc => fc.IncludeInSearch)
            .ToListAsync(token);

        matched.AddRange(searchable.Where(fc =>
            fc.FileNameParts.Any(fnp => fileDetail.FullNameWithPath.Contains(fnp.Text, StringComparison.OrdinalIgnoreCase))));
    }

    private static async Task CollectCategoryMatchAsync(FilesContext context, string categoryId, List<FileClassification> matched, CancellationToken token)
    {
        if (string.IsNullOrEmpty(categoryId)) return;

        var searchConfig = await context.SearchConfigurations
            .Include(sc => sc.SearchCategories)
            .SingleAsync(token);

        var category = searchConfig.SearchCategories.FirstOrDefault(c => c.Id == categoryId && c.IncludeInSearch);
        if (category is null) return;

        matched.Add(await FindOrCreateClassificationAsync(context, category.Name));
    }

    private static async Task CollectTagMatchesAsync(FilesContext context, IReadOnlyList<string> imageTags, List<FileClassification> matched, CancellationToken token)
    {
        if (imageTags.Count == 0) return;

        var tagSet = new HashSet<string>(imageTags.Select(t => t.ToLowerInvariant()), StringComparer.OrdinalIgnoreCase);
        var allIncludedTags = await context.ScrapedTags
            .Where(t => t.IncludeInSearch)
            .ToListAsync(token);

        foreach (var tag in allIncludedTags.Where(t => tagSet.Contains(t.Value.ToLowerInvariant())))
            matched.Add(await FindOrCreateClassificationAsync(context, tag.Value));
    }

    private static async Task<FileClassification> FindOrCreateClassificationAsync(FilesContext context, string name)
    {
        var normalizedName = name.ToLowerInvariant();
        var tracked = context.ChangeTracker.Entries<FileClassification>()
            .FirstOrDefault(e => e.Entity.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase))?.Entity;
        if (tracked is not null) return tracked;

        var existing = await context.FileClassifications.FirstOrDefaultAsync(fc => fc.Name == normalizedName);
        if (existing is not null) return existing;

        var created = new FileClassification { Name = normalizedName, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
        context.FileClassifications.Add(created);

        return created;
    }
}
