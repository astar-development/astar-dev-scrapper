using AStar.Dev.Infrastructure.FilesDb.Data;
using AStar.Dev.Infrastructure.FilesDb.Models;
using AStar.Dev.Wallpaper.Scrapper.Models;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public static class DataSeed
{
    private static readonly string[] TagsToIgnoreCompletelyValues =
    [
        "Vladislava Shelygina", "Bianca Beauchamp", "Uy Uy", "CGI", "Functions",
        "hairy armpits", "Beau D", "Lucie Wilde", "Brooke Adams", "erotic art",
        "concept art", "2D", "foot fetishism", "curvy", "Big Areola", "big areolae",
        "cartoon", "artwork", "Jana Defi", "Piper Perri", "Dakota Pink", "saggy boobs",
        "Sarah Jay", "Sara Jay", "fan art"
    ];

    public static async Task Seed(ScrapeConfiguration scrapeConfiguration, Logger logger, FilesContext dbContext)
    {
        if(!dbContext.TagsToIgnore.Any(t => t.IgnoreImage))
        {
            logger.Information("Seeding tags to ignore completely...");
            dbContext.TagsToIgnore.AddRange(
                TagsToIgnoreCompletelyValues.Distinct().Select(tag => new TagToIgnore { Value = tag, IgnoreImage = true }));
            await dbContext.SaveChangesAsync();
        }

        if(!dbContext.ScrapeConfiguration.Any())
        {
            logger.Information("Seeding ScrapeConfiguration...");
            dbContext.ScrapeConfiguration.Add(new ScrapeConfigurationEntity
            {
                ConnectionStrings = new Infrastructure.FilesDb.Models.ConnectionStrings
                {
                    Sqlite = scrapeConfiguration.ConnectionStrings.Sqlite
                },
                UserConfiguration = new Infrastructure.FilesDb.Models.UserConfiguration
                {
                    Username          = scrapeConfiguration.UserConfiguration.Username,
                    Password          = scrapeConfiguration.UserConfiguration.Password,
                    LoginEmailAddress = scrapeConfiguration.UserConfiguration.LoginEmailAddress,
                    SessionCookie     = scrapeConfiguration.UserConfiguration.SessionCookie,
                },
                SearchConfiguration = new Infrastructure.FilesDb.Models.SearchConfiguration
                {
                    BaseUrl          = scrapeConfiguration.SearchConfiguration.BaseUrl,
                    ApiKey           = scrapeConfiguration.SearchConfiguration.ApiKey,
                    LoginUrl         = scrapeConfiguration.SearchConfiguration.LoginUrl,
                    UseHeadless      = scrapeConfiguration.SearchConfiguration.UseHeadless,
                    SlowMotionDelay  = scrapeConfiguration.SearchConfiguration.SlowMotionDelay,
                    SearchString     = scrapeConfiguration.SearchConfiguration.SearchString,
                    TopWallpapers    = scrapeConfiguration.SearchConfiguration.TopWallpapers,
                    SearchStringPrefix             = scrapeConfiguration.SearchConfiguration.SearchStringPrefix,
                    SearchStringSuffix             = scrapeConfiguration.SearchConfiguration.SearchStringSuffix,
                    Subscriptions                  = scrapeConfiguration.SearchConfiguration.Subscriptions,
                    ImagePauseInSeconds            = scrapeConfiguration.SearchConfiguration.ImagePauseInSeconds,
                    StartingPageNumber             = scrapeConfiguration.SearchConfiguration.StartingPageNumber,
                    TotalPages                     = scrapeConfiguration.SearchConfiguration.TotalPages,
                    SubscriptionsStartingPageNumber = scrapeConfiguration.SearchConfiguration.SubscriptionsStartingPageNumber,
                    SubscriptionsTotalPages        = scrapeConfiguration.SearchConfiguration.SubscriptionsTotalPages,
                    TopWallpapersTotalPages        = scrapeConfiguration.SearchConfiguration.TopWallpapersTotalPages,
                    TopWallpapersStartingPageNumber = scrapeConfiguration.SearchConfiguration.TopWallpapersStartingPageNumber,
                    SearchCategories               = [.. scrapeConfiguration.SearchConfiguration.SearchCategories.Select(c => new SearchCategories
                    {
                        Id                  = c.Id,
                        Name                = c.Name,
                        LastKnownImageCount = c.LastKnownImageCount,
                        LastPageVisited     = c.LastPageVisited,
                        TotalPages          = c.TotalPages,
                    })],
                },
                ScrapeDirectories = scrapeConfiguration.ScrapeDirectories.ToEntity(),
            });
            await dbContext.SaveChangesAsync();
        }
    }
}
