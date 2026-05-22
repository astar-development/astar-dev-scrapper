using System.Reflection;
using AStar.Dev.Utilities;

namespace AStar.Dev.Wallpaper.Scrapper;

public static class ApplicationMetadata
{
    public const string Name = "AStar.Dev.Wallpaper.Scrapper";
    public const string Version = "1.0.0";

    public static string ApplicationFolder => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!.CombinePath("..", "..", "..");
}