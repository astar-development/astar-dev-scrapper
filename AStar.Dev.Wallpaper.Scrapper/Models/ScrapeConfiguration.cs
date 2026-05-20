namespace AStar.Dev.Wallpaper.Scrapper.Models;

public sealed class ScrapeConfiguration
{
    public ConnectionStrings ConnectionStrings { get; set; } = new();

    public UserConfiguration UserConfiguration { get; set; } = new();

    public SearchConfiguration SearchConfiguration { get; set; } = new();

    public ScrapeDirectories ScrapeDirectories { get; set; } = new();
}
