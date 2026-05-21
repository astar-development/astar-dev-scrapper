using System.Reflection;
using AStar.Dev.Utilities;
using AStar.Dev.Wallpaper.Scrapper.Models;
using Microsoft.Extensions.Configuration;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

internal class ConfigurationFactory
{
    public static ScrapeConfiguration Configuration()
    {
        var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).CombinePath("..","..","..");

        IConfiguration config = new ConfigurationBuilder()
                               .AddJsonFile(Path.Combine(assemblyPath, "appsettings.json"), false, true)
                               .AddUserSecrets<ConfigurationFactory>(true, true)
                               .Build();

        ScrapeConfiguration scrapeConfiguration = config.GetSection(nameof(ScrapeConfiguration)).Get<ScrapeConfiguration>()!;

        if(StartingPageIsOutsideValidRange(scrapeConfiguration)) scrapeConfiguration.SearchConfiguration.StartingPageNumber = 1;

        if(SubscriptionsStartingPageIsOutsideValidRange(scrapeConfiguration)) scrapeConfiguration.SearchConfiguration.SubscriptionsStartingPageNumber = 1;

        scrapeConfiguration.SearchConfiguration.SearchString = scrapeConfiguration.SearchConfiguration.SearchString.Replace(scrapeConfiguration.SearchConfiguration.BaseUrl,
                                                                                                                            string.Empty);

        scrapeConfiguration.SearchConfiguration.Subscriptions = scrapeConfiguration.SearchConfiguration.Subscriptions.Replace(scrapeConfiguration.SearchConfiguration.BaseUrl,
                                                                                                                              string.Empty);

        var lastIndexOfEqualsInSearchString = scrapeConfiguration.SearchConfiguration.SearchString.LastIndexOf("=", StringComparison.OrdinalIgnoreCase) + 1;

        if(lastIndexOfEqualsInSearchString < scrapeConfiguration.SearchConfiguration.SearchString.Length) scrapeConfiguration.SearchConfiguration.SearchString = scrapeConfiguration.SearchConfiguration.SearchString[..lastIndexOfEqualsInSearchString];

        var lastIndexOfEqualsInSubscriptions = scrapeConfiguration.SearchConfiguration.Subscriptions.LastIndexOf("=", StringComparison.OrdinalIgnoreCase) + 1;

        if(lastIndexOfEqualsInSubscriptions < scrapeConfiguration.SearchConfiguration.Subscriptions.Length) scrapeConfiguration.SearchConfiguration.Subscriptions = scrapeConfiguration.SearchConfiguration.Subscriptions[..lastIndexOfEqualsInSubscriptions];

        return scrapeConfiguration;
    }

    public static Logging Logging()
    {
        var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).CombinePath("..", "..", "..");

        IConfiguration config = new ConfigurationBuilder()
                               .AddJsonFile(Path.Combine(assemblyPath, "appsettings.json"), false, true)
                               .Build();

        return config.GetSection("Logging").Get<Logging>() ?? new Logging();
    }

    private static bool SubscriptionsStartingPageIsOutsideValidRange(ScrapeConfiguration scrapeConfiguration)
        => scrapeConfiguration.SearchConfiguration.SubscriptionsStartingPageNumber <= 0 ||
           scrapeConfiguration.SearchConfiguration.SubscriptionsStartingPageNumber > scrapeConfiguration.SearchConfiguration.SubscriptionsTotalPages;

    private static bool StartingPageIsOutsideValidRange(ScrapeConfiguration scrapeConfiguration)
        => scrapeConfiguration.SearchConfiguration.StartingPageNumber <= 0 || scrapeConfiguration.SearchConfiguration.StartingPageNumber > scrapeConfiguration.SearchConfiguration.TotalPages;
}
