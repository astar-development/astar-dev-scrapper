using System.Text.RegularExpressions;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

internal static class DirectoryHelper
{
    public static string CreateDirectoryIfRequired(string fullDirectoryPath)
    {
        var invalidFileChars = Path.GetInvalidFileNameChars();
        fullDirectoryPath = Regex.Replace(fullDirectoryPath, """[^\u0000-\u007F]+""", string.Empty);

        foreach(var invalidFileChar in invalidFileChars) _ = fullDirectoryPath.Replace(invalidFileChar, ' ');

        fullDirectoryPath = fullDirectoryPath.Replace("\"", "'").Replace("|", string.Empty).Replace("@", string.Empty).Replace("煙", string.Empty);

        if(fullDirectoryPath.LastIndexOf(":", StringComparison.Ordinal) > 2) fullDirectoryPath = fullDirectoryPath[..2] + fullDirectoryPath[2..].Replace(":", "_");

        fullDirectoryPath = fullDirectoryPath.Trim();

        if(Directory.Exists(fullDirectoryPath)) return fullDirectoryPath;

        _ = Directory.CreateDirectory(fullDirectoryPath);

        return fullDirectoryPath;
    }
}
