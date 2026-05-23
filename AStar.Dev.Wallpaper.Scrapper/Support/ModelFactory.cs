using System.Text.Json;
using AStar.Dev.Wallpaper.Scrapper.DTOs;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

public static class ModelFactory
{
    public static ModelsToIgnore LoadModelsIgnore()
    {
        var               tags         = File.ReadAllText(Path.Combine(ApplicationMetadata.ApplicationFolder, "modelsToIgnore.json"));
        ModelsToIgnore? modelsToIgnore = JsonSerializer.Deserialize<ModelsToIgnore>(tags);

        return modelsToIgnore!;
    }
}