using System.Globalization;
using AStar.Dev.Infrastructure.FilesDb.Data;
using AStar.Dev.Infrastructure.FilesDb.Models;
using AStar.Dev.Utilities;
using AStar.Dev.Wallpaper.Scrapper.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scrapper.Repositories;

public sealed class ScrapedTagRepository(IDbContextFactory<FilesContext> contextFactory) : IScrapedTagRepository
{
    public async Task SaveAsync(IReadOnlyList<TagData> tags)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var titleCasedTags = new List<AStar.Dev.Wallpaper.Scrapper.DTOs.ScrapedTag>();

        var textInfo = new CultureInfo("en-GB",false).TextInfo;
        tags.ForEach(t => titleCasedTags.Add(new AStar.Dev.Wallpaper.Scrapper.DTOs.ScrapedTag{Category = textInfo.ToTitleCase(t.Category ?? ""), Value = textInfo.ToTitleCase(t.Tag)}));

        foreach(var tag in titleCasedTags)
        {
            if(!await context.ScrapedTags.AnyAsync(t => t.Value == tag.Value && tag.Category == t.Category))
                _ = await context.ScrapedTags.AddAsync(tag.ToDomain());
        }

        _ = await context.SaveChangesAsync();
    }
}
