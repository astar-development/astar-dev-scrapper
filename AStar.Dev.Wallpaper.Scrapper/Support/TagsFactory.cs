using System.Text.Json;
using AStar.Dev.Wallpaper.Scrapper.DTOs;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

public static class TagsFactory
{
    public static TagsToIgnoreCompletely LoadTagsToIgnoreCompletely()
    {
        var                     tags         = File.ReadAllText(Path.Combine(ApplicationMetadata.ApplicationFolder, "tagsToIgnoreCompletely.json"));
        TagsToIgnoreCompletely? tagsToIgnore = JsonSerializer.Deserialize<TagsToIgnoreCompletely>(tags);

        return tagsToIgnore!;
    }

    public static TagsTextToIgnore LoadTagsTextToIgnore()
    {
        var               tags         = File.ReadAllText(Path.Combine(ApplicationMetadata.ApplicationFolder, "tagsTextToIgnore.json"));
        TagsTextToIgnore? tagsToIgnore = JsonSerializer.Deserialize<TagsTextToIgnore>(tags);

        return tagsToIgnore!;
    }
}
