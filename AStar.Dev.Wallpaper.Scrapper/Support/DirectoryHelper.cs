using AStar.Dev.Infrastructure.FilesDb.Models;
using AStar.Dev.Utilities;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

internal static class DirectoryHelper
{
    public static DirectoryName CreateDirectoryIfRequired(string fullDirectoryPath)
    {
        fullDirectoryPath = fullDirectoryPath.CleanPath();

        if(fullDirectoryPath.LastIndexOf(':') > 2) fullDirectoryPath = fullDirectoryPath[..2] + fullDirectoryPath[2..].Replace(":", "_");

        if(Directory.Exists(fullDirectoryPath)) return new(fullDirectoryPath);

        _ = Directory.CreateDirectory(fullDirectoryPath);

        return new(fullDirectoryPath);
    }
}
