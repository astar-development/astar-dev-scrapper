using System.Globalization;
using System.Text.Json;
using AStar.Dev.Utilities;
using AStar.Dev.Wallpaper.Scrapper.Models;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

public sealed class ConfigurationSaver(ScrapeConfiguration scrapeConfiguration, Logging logging, Logger logger)
{
    private const  string SecretIdFromProjectFile = "c35e09dc-dc30-416a-95a6-ec1a5ba1b43f";
    private readonly JsonSerializerOptions jsonSerializerOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, };

    public void SaveUpdatedConfiguration()
    {
        try
        {
            UpdateAndSaveTheConfiguration();
        }
        catch(Exception exception)
        {
            logger.Error(exception.GetBaseException().Message);

            throw;
        }
    }

    private void UpdateAndSaveTheConfiguration()
    {
        scrapeConfiguration = scrapeConfiguration with {SearchConfiguration= scrapeConfiguration.SearchConfiguration with {SearchCategories = DeduplicateTheCategories()}};
        UpdateCategoryNames();
        var configurationWrapper = new Configuration { ScrapeConfiguration = scrapeConfiguration, Logging = logging, };
        SaveSecretsFile(configurationWrapper);

        const string redacted = "REDACTED!";

        var copy = scrapeConfiguration with { UserConfiguration = scrapeConfiguration.UserConfiguration with { Password = redacted}, ConnectionStrings = scrapeConfiguration.ConnectionStrings with { Sqlite = redacted }, ScrapeDirectories = scrapeConfiguration.ScrapeDirectories with { SubDirectoryName = redacted}, SearchConfiguration = scrapeConfiguration.SearchConfiguration with { SearchCategories = []} };
        var content = JsonSerializer.Serialize(new Configuration { ScrapeConfiguration = copy });
        SaveRedactedAppSettings(content);

    }

    private void SaveSecretsFile(Configuration configurationWrapper)
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string? secretsPath = GetSecretsPath(homeDirectory);
        var contentWithRealPassword = JsonSerializer.Serialize(configurationWrapper, jsonSerializerOptions);
        File.WriteAllText(secretsPath, contentWithRealPassword);
    }

    private static string GetSecretsPath(string homeDirectory)
        => OperatingSystem.IsWindows()
            ? Path.Combine(homeDirectory, "AppData", "Roaming", "Microsoft", "UserSecrets", SecretIdFromProjectFile, "secrets.json")
            : OperatingSystem.IsLinux() ? Path.Combine(homeDirectory, ".microsoft", "usersecrets", SecretIdFromProjectFile, "secrets.json")
            : "MacOS-TBC";

    private static void SaveRedactedAppSettings(string content)
        => File.WriteAllText(Path.Combine(ApplicationMetadata.ApplicationFolder, "appsettings.json"), content);

    private void UpdateCategoryNames()
        => scrapeConfiguration.SearchConfiguration.SearchCategories
            .ForEach(searchConfigurationSearchCategory => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(searchConfigurationSearchCategory.Name));

    private Category[] DeduplicateTheCategories()
        => [.. scrapeConfiguration.SearchConfiguration.SearchCategories.DistinctBy(x => x.Id)];
}
