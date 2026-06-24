namespace AStar.Dev.Wallpaper.Scrapper.Repositories;

public interface IScrapedTagRepository
{
    Task SaveAsync(IReadOnlyList<TagData> tags);
}

public record TagData(string Tag, string? Category);