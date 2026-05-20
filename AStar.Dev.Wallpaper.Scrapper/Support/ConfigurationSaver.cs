using System.Globalization;
using System.Reflection;
using System.Text.Json;
using AStar.Dev.Wallpaper.Scrapper.Models;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

public sealed class ConfigurationSaver(ScrapeConfiguration scrapeConfiguration, Logging logging, Logger logger)
{
    private readonly JsonSerializerOptions jsonSerializerOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, };
    private readonly Logging               logging               = logging             ?? throw new ArgumentNullException();
    private readonly ScrapeConfiguration   scrapeConfiguration   = scrapeConfiguration ?? throw new ArgumentNullException();

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
        var actualSqlServer    = scrapeConfiguration.ConnectionStrings.SqlServer;
        scrapeConfiguration.SearchConfiguration.SearchCategories = DeduplicateTheCategories();
        UpdateCategoryNames();
        var configurationWrapper = new Configuration { ScrapeConfiguration = scrapeConfiguration, Logging = logging, };
        SaveSecretsFile(configurationWrapper);

        const string redacted = "REDACTED!";
        scrapeConfiguration.UserConfiguration.Password         = redacted;
        scrapeConfiguration.ScrapeDirectories.SubDirectoryName = redacted;
        scrapeConfiguration.ConnectionStrings.SqlServer        = redacted;
        Category[] categories = scrapeConfiguration.SearchConfiguration.SearchCategories;
        scrapeConfiguration.SearchConfiguration.SearchCategories = [new Category(),];

        var content = JsonSerializer.Serialize(configurationWrapper, jsonSerializerOptions);

        SaveRedactedAppSettings(content);
        scrapeConfiguration.SearchConfiguration.SearchCategories = categories;
        scrapeConfiguration.UserConfiguration.Password           = actualPassword;
        scrapeConfiguration.ScrapeDirectories.SubDirectoryName   = actualSubDirectory;
        scrapeConfiguration.ConnectionStrings.SqlServer          = actualSqlServer;
    }

    private void SaveSecretsFile(Configuration configurationWrapper)
    {
        var homeDirectory           = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var secretsPath             = Path.Combine(homeDirectory, """AppData\Roaming\Microsoft\UserSecrets\c35e09dc-dc30-416a-95a6-ec1a5ba1b43f\secrets.json""");
        var contentWithRealPassword = JsonSerializer.Serialize(configurationWrapper, jsonSerializerOptions);

        File.WriteAllText(secretsPath, contentWithRealPassword);
    }

    private static void SaveRedactedAppSettings(string content)
    {
        const string navigateUp   = """..\..\..\..\""";
        var          assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + navigateUp;

        File.WriteAllText(Path.Combine(assemblyPath, "appSettings.json"), content);
    }

    private void UpdateCategoryNames()
    {
        TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;

        foreach(Category searchConfigurationSearchCategory in scrapeConfiguration.SearchConfiguration.SearchCategories) searchConfigurationSearchCategory.Name = textInfo.ToTitleCase(searchConfigurationSearchCategory.Name);
    }

    private Category[] DeduplicateTheCategories()
        => scrapeConfiguration.SearchConfiguration.SearchCategories.DistinctBy(x => x.Id).ToArray();
}
