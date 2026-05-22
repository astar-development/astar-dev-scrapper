
namespace AStar.Dev.Wallpaper.Scrapper.Models;

public sealed class ScrapeDirectories
{
    public string RootDirectory { get; set; } = string.Empty;

    public string BaseSaveDirectory { get; set; } = string.Empty;

    public string BaseDirectory { get; set; } = string.Empty;

    public string BaseDirectoryFamous { get; set; } = string.Empty;

    public string SubDirectoryName { get; set; } = string.Empty;

    internal Infrastructure.FilesDb.Models.ScrapeDirectories ToEntity()
    {
        return new Infrastructure.FilesDb.Models.ScrapeDirectories
        {
            RootDirectory = RootDirectory,
            BaseSaveDirectory = BaseSaveDirectory,
            BaseDirectory = BaseDirectory,
            BaseDirectoryFamous = BaseDirectoryFamous,
            SubDirectoryName = SubDirectoryName
        };
    }
}
