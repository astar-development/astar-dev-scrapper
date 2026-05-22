using System.Globalization;
using System.Text.Json;
using AStar.Dev.Utilities;
using AStar.Dev.Wallpaper.Scrapper.Models;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

public sealed class ConfigurationSaver(ScrapeConfiguration scrapeConfiguration, Logging logging, Logger logger)
{
    private const  string SecretIdFromProjectFile = "c35e09dc-dc30-416a-95a6-ec1a5ba1b4";
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
        var actualPassword     = scrapeConfiguration.UserConfiguration.Password;
        var actualSubDirectory = scrapeConfiguration.ScrapeDirectories.SubDirectoryName;
        var actualSqlServer    = scrapeConfiguration.ConnectionStrings.Sqlite;
        scrapeConfiguration.SearchConfiguration.SearchCategories = DeduplicateTheCategories();
        UpdateCategoryNames();
        var configurationWrapper = new Configuration { ScrapeConfiguration = scrapeConfiguration, Logging = logging, };
        SaveSecretsFile(configurationWrapper);

        const string redacted = "REDACTED!";
        scrapeConfiguration.UserConfiguration.Password         = redacted;
        scrapeConfiguration.ScrapeDirectories.SubDirectoryName = redacted;
        scrapeConfiguration.ConnectionStrings.Sqlite        = redacted;
        Category[] categories = scrapeConfiguration.SearchConfiguration.SearchCategories;
        scrapeConfiguration.SearchConfiguration.SearchCategories = [new Category(),];

        var content = JsonSerializer.Serialize(configurationWrapper, jsonSerializerOptions);

        SaveRedactedAppSettings(content);
        scrapeConfiguration.SearchConfiguration.SearchCategories = categories;
        scrapeConfiguration.UserConfiguration.Password           = actualPassword;
        scrapeConfiguration.ScrapeDirectories.SubDirectoryName   = actualSubDirectory;
        scrapeConfiguration.ConnectionStrings.Sqlite          = actualSqlServer;
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
            : OperatingSystem.IsLinux() ? Path.Combine(homeDirectory, ".microsoft", "usersecrets")
            : "MacOS-TBC";

    private static void SaveRedactedAppSettings(string content)
        => File.WriteAllText(Path.Combine(ApplicationMetadata.ApplicationFolder, "appsettings.json"), content);

    private void UpdateCategoryNames()
        => scrapeConfiguration.SearchConfiguration.SearchCategories
            .ForEach(searchConfigurationSearchCategory => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(searchConfigurationSearchCategory.Name));

    private Category[] DeduplicateTheCategories()
        => [.. scrapeConfiguration.SearchConfiguration.SearchCategories.DistinctBy(x => x.Id)];
}
