using System.Text.Json;
using AStar.Dev.Wallpaper.Scrapper.DTOs;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

public static class ModelFactory
{
    public static ModelsToIgnore LoadModelsIgnore()
    {
        var json = File.ReadAllText(Path.Combine(ApplicationMetadata.ApplicationFolder, "modelsToIgnore.json"));
        return new ModelsToIgnore { Models = [.. JsonSerializer.Deserialize<ModelToIgnore[]>(json)!] };
    }
}
