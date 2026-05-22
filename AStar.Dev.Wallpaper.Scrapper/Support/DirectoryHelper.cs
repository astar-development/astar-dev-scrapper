using AStar.Dev.Utilities;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

internal static class DirectoryHelper
{
    public static string CreateDirectoryIfRequired(string fullDirectoryPath)
    {
        fullDirectoryPath = fullDirectoryPath.CleanPath();

        if(fullDirectoryPath.LastIndexOf(':') > 2) fullDirectoryPath = fullDirectoryPath[..2] + fullDirectoryPath[2..].Replace(":", "_");

        if(Directory.Exists(fullDirectoryPath)) return fullDirectoryPath;

        _ = Directory.CreateDirectory(fullDirectoryPath);

        return fullDirectoryPath;
    }
}
