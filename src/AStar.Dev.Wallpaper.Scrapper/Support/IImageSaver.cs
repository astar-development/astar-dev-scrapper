namespace AStar.Dev.Wallpaper.Scrapper.Support;

/// <summary>Saves a scraped image's bytes to disk.</summary>
public interface IImageSaver
{
    /// <summary>Saves <paramref name="image" /> to <paramref name="path" />, cleaning the path first.</summary>
    Task SaveAsync(byte[] image, string path);
}
