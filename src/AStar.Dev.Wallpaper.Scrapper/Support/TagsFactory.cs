using System.Reflection;
using System.Text.Json;
using AStar.Dev.Wallpaper.Scrapper.DTOs;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

internal static class TagsFactory
{
    public static TagsToIgnoreCompletely LoadTagsToIgnoreCompletely()
    {
        var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + """..\..\..\..\""";

        var                     tags         = File.ReadAllText(Path.Combine(assemblyPath, "tagsToIgnoreCompletely.json"));
        TagsToIgnoreCompletely? tagsToIgnore = JsonSerializer.Deserialize<TagsToIgnoreCompletely>(tags);

        return tagsToIgnore!;
    }

    public static TagsTextToIgnore LoadTagsTextToIgnore()
    {
        var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + """..\..\..\..\""";

        var               tags         = File.ReadAllText(Path.Combine(assemblyPath, "tagsTextToIgnore.json"));
        TagsTextToIgnore? tagsToIgnore = JsonSerializer.Deserialize<TagsTextToIgnore>(tags);

        return tagsToIgnore!;
    }
}
