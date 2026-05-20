namespace AStar.Dev.Wallpaper.Scrapper.Models;

public class Category
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int LastKnownImageCount { get; set; }

    public int LastPageVisited { get; set; }

    public int TotalPages => (int)Math.Ceiling((decimal)LastKnownImageCount / 24);
}
