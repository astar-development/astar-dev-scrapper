using AStar.Dev.Infrastructure.FilesDb.Data;
using AStar.Dev.Infrastructure.FilesDb.Models;
using AStar.Dev.Wallpaper.Scrapper.Models;
using Microsoft.EntityFrameworkCore;
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
                SearchConfiguration = BuildSearchConfigurationEntity(scrapeConfiguration),
                ScrapeDirectories   = scrapeConfiguration.ScrapeDirectories.ToEntity(),
            });
            await dbContext.SaveChangesAsync();
        }
        else
        {
            await UpdateIncompleteSearchConfigurationAsync(scrapeConfiguration, logger, dbContext);
        }
    }

    public static async Task SeedFileClassificationsAsync(string csvPath, Logger logger, FilesContext dbContext)
    {
        if(dbContext.FileClassifications.Any()) return;

        logger.Information("Seeding file classifications from {CsvPath}...", csvPath);

        var lines = await File.ReadAllLinesAsync(csvPath);
        var rows = lines.Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Split(','))
            .Where(parts => parts.Length >= 4)
            .Select(parts => new
            {
                FileNameContains = parts[0],
                DatabaseMapping  = parts[1].Trim(),
                Celebrity        = parts[2].Trim().Equals("TRUE", StringComparison.OrdinalIgnoreCase),
                Searchable       = parts[3].Trim().Equals("TRUE", StringComparison.OrdinalIgnoreCase)
            })
            .ToList();

        foreach(var group in rows.GroupBy(r => r.DatabaseMapping))
        {
            var first          = group.First();
            var classification = new FileClassification
            {
                Name            = group.Key,
                Celebrity       = first.Celebrity,
                IncludeInSearch = first.Searchable,
                FileNameParts   = [.. group.Select(r => new FileNamePart { Text = r.FileNameContains, IncludeInSearch = r.Searchable })]
            };
            dbContext.FileClassifications.Add(classification);
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task UpdateIncompleteSearchConfigurationAsync(ScrapeConfiguration scrapeConfiguration, Logger logger, FilesContext dbContext)
    {
        var existing = await dbContext.SearchConfigurations.SingleAsync();
        if(!string.IsNullOrEmpty(existing.SearchStringPrefix)) return;

        logger.Information("Updating incomplete SearchConfiguration with missing fields...");
        existing.LoginUrl                       = scrapeConfiguration.SearchConfiguration.LoginUrl;
        existing.UseHeadless                    = scrapeConfiguration.SearchConfiguration.UseHeadless;
        existing.SlowMotionDelay                = scrapeConfiguration.SearchConfiguration.SlowMotionDelay;
        existing.SearchStringPrefix             = scrapeConfiguration.SearchConfiguration.SearchStringPrefix;
        existing.SearchStringSuffix             = scrapeConfiguration.SearchConfiguration.SearchStringSuffix;
        existing.Subscriptions                  = scrapeConfiguration.SearchConfiguration.Subscriptions;
        existing.ImagePauseInSeconds            = scrapeConfiguration.SearchConfiguration.ImagePauseInSeconds;
        existing.StartingPageNumber             = scrapeConfiguration.SearchConfiguration.StartingPageNumber;
        existing.TotalPages                     = scrapeConfiguration.SearchConfiguration.TotalPages;
        existing.SubscriptionsStartingPageNumber = scrapeConfiguration.SearchConfiguration.SubscriptionsStartingPageNumber;
        existing.SubscriptionsTotalPages        = scrapeConfiguration.SearchConfiguration.SubscriptionsTotalPages;
        existing.TopWallpapersTotalPages        = scrapeConfiguration.SearchConfiguration.TopWallpapersTotalPages;
        existing.TopWallpapersStartingPageNumber = scrapeConfiguration.SearchConfiguration.TopWallpapersStartingPageNumber;
        await dbContext.SaveChangesAsync();
    }

    private static Infrastructure.FilesDb.Models.SearchConfiguration BuildSearchConfigurationEntity(ScrapeConfiguration scrapeConfiguration)
        => new()
        {
            BaseUrl                         = scrapeConfiguration.SearchConfiguration.BaseUrl,
            ApiKey                          = scrapeConfiguration.SearchConfiguration.ApiKey,
            LoginUrl                        = scrapeConfiguration.SearchConfiguration.LoginUrl,
            UseHeadless                     = scrapeConfiguration.SearchConfiguration.UseHeadless,
            SlowMotionDelay                 = scrapeConfiguration.SearchConfiguration.SlowMotionDelay,
            SearchString                    = scrapeConfiguration.SearchConfiguration.SearchString,
            TopWallpapers                   = scrapeConfiguration.SearchConfiguration.TopWallpapers,
            SearchStringPrefix              = scrapeConfiguration.SearchConfiguration.SearchStringPrefix,
            SearchStringSuffix              = scrapeConfiguration.SearchConfiguration.SearchStringSuffix,
            Subscriptions                   = scrapeConfiguration.SearchConfiguration.Subscriptions,
            ImagePauseInSeconds             = scrapeConfiguration.SearchConfiguration.ImagePauseInSeconds,
            StartingPageNumber              = scrapeConfiguration.SearchConfiguration.StartingPageNumber,
            TotalPages                      = scrapeConfiguration.SearchConfiguration.TotalPages,
            SubscriptionsStartingPageNumber = scrapeConfiguration.SearchConfiguration.SubscriptionsStartingPageNumber,
            SubscriptionsTotalPages         = scrapeConfiguration.SearchConfiguration.SubscriptionsTotalPages,
            TopWallpapersTotalPages         = scrapeConfiguration.SearchConfiguration.TopWallpapersTotalPages,
            TopWallpapersStartingPageNumber = scrapeConfiguration.SearchConfiguration.TopWallpapersStartingPageNumber,
            SearchCategories                = [.. scrapeConfiguration.SearchConfiguration.SearchCategories.Select(c => new SearchCategories
            {
                Id                  = c.Id,
                Name                = c.Name,
                LastKnownImageCount = c.LastKnownImageCount,
                LastPageVisited     = c.LastPageVisited,
                TotalPages          = c.TotalPages,
            })],
        };
}
