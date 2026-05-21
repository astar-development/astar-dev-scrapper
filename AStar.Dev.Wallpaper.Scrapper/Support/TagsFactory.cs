using System.Reflection;
using System.Text.Json;
using AStar.Dev.Utilities;
using AStar.Dev.Wallpaper.Scrapper.DTOs;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

public static class TagsFactory
{
    public static TagsToIgnoreCompletely LoadTagsToIgnoreCompletely()
    {
        var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).CombinePath("..", "..", "..");

        var                     tags         = File.ReadAllText(Path.Combine(assemblyPath, "tagsToIgnoreCompletely.json"));
        TagsToIgnoreCompletely? tagsToIgnore = JsonSerializer.Deserialize<TagsToIgnoreCompletely>(tags);

        return tagsToIgnore!;
    }

    public static TagsTextToIgnore LoadTagsTextToIgnore()
    {
        var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).CombinePath("..", "..", "..");

        var               tags         = File.ReadAllText(Path.Combine(assemblyPath, "tagsTextToIgnore.json"));
        TagsTextToIgnore? tagsToIgnore = JsonSerializer.Deserialize<TagsTextToIgnore>(tags);

        return tagsToIgnore!;
    }
}
