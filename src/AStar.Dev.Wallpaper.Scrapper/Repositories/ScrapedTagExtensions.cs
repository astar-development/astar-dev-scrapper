namespace AStar.Dev.Wallpaper.Scrapper.Repositories;

public static class ScrapedTagExtensions
{
    public static AStar.Dev.Infrastructure.FilesDb.Models.ScrapedTag ToDomain(this AStar.Dev.Wallpaper.Scrapper.DTOs.ScrapedTag tag)
        => new()
        {
            Id = new AStar.Dev.Infrastructure.FilesDb.Models.ScrapedTagId(tag.Id.Value),
            Value = tag.Value,
            Category = tag.Category,
            IncludeInSearch = tag.IncludeInSearch
        };
}