using AStar.Dev.Wallpaper.Scrapper.Models;
using Microsoft.Extensions.Configuration;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

public class ConfigurationFactory
{
    public static (ScrapeConfiguration ScrapeConfiguration, IConfigurationRoot Configuration) Configuration()
    {
        IConfigurationRoot config = new ConfigurationBuilder()
                               .SetBasePath(ApplicationMetadata.ApplicationFolder)
                               .AddJsonFile("appsettings.json", false, true)
                               .AddUserSecrets<ConfigurationFactory>(true, true)
                               .Build();

        ScrapeConfiguration scrapeConfiguration = config.GetSection(nameof(ScrapeConfiguration)).Get<ScrapeConfiguration>()!;

        if(StartingPageIsOutsideValidRange(scrapeConfiguration)) scrapeConfiguration = scrapeConfiguration with {SearchConfiguration = scrapeConfiguration.SearchConfiguration with {StartingPageNumber = 1}};

        if(SubscriptionsStartingPageIsOutsideValidRange(scrapeConfiguration)) scrapeConfiguration = scrapeConfiguration with {SearchConfiguration = scrapeConfiguration.SearchConfiguration with {SubscriptionsStartingPageNumber = 1}};

        var searchConfig = scrapeConfiguration.SearchConfiguration;
        var newSearchString = searchConfig.SearchString.Replace(searchConfig.BaseUrl, string.Empty);
        var newSubscriptions = searchConfig.Subscriptions.Replace(searchConfig.BaseUrl, string.Empty);

        var lastIndexOfEqualsInSearchString = newSearchString.LastIndexOf("=", StringComparison.OrdinalIgnoreCase) + 1;
        if(lastIndexOfEqualsInSearchString < newSearchString.Length) newSearchString = newSearchString[..lastIndexOfEqualsInSearchString];

        var lastIndexOfEqualsInSubscriptions = newSubscriptions.LastIndexOf("=", StringComparison.OrdinalIgnoreCase) + 1;
        if(lastIndexOfEqualsInSubscriptions < newSubscriptions.Length) newSubscriptions = newSubscriptions[..lastIndexOfEqualsInSubscriptions];

        scrapeConfiguration = scrapeConfiguration with { SearchConfiguration = searchConfig with { SearchString = newSearchString, Subscriptions = newSubscriptions } };

        return (scrapeConfiguration, config);
    }

    private static bool SubscriptionsStartingPageIsOutsideValidRange(ScrapeConfiguration scrapeConfiguration)
        => scrapeConfiguration.SearchConfiguration.SubscriptionsStartingPageNumber <= 0 ||
           scrapeConfiguration.SearchConfiguration.SubscriptionsStartingPageNumber > scrapeConfiguration.SearchConfiguration.SubscriptionsTotalPages;

    private static bool StartingPageIsOutsideValidRange(ScrapeConfiguration scrapeConfiguration)
        => scrapeConfiguration.SearchConfiguration.StartingPageNumber <= 0 || scrapeConfiguration.SearchConfiguration.StartingPageNumber > scrapeConfiguration.SearchConfiguration.TotalPages;
}
