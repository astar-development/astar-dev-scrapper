using AStar.Dev.Infrastructure.FilesDb.Models;

namespace AStar.Dev.Wallpaper.Scrapper.Models;

public static class ScrapeConfigurationEntityExtensions
{
    public static ScrapeConfiguration ToAppModel(this ScrapeConfigurationEntity entity)
        => new(
            new ConnectionStrings(entity.ConnectionStrings.Sqlite),
            new UserConfiguration(
                entity.UserConfiguration.LoginEmailAddress,
                entity.UserConfiguration.Username,
                entity.UserConfiguration.Password,
                entity.UserConfiguration.SessionCookie),
            new SearchConfiguration(
                entity.SearchConfiguration.BaseUrl,
                entity.SearchConfiguration.ApiKey,
                entity.SearchConfiguration.LoginUrl,
                [.. entity.SearchConfiguration.SearchCategories.Select(c => new Category
                {
                    Id                  = c.Id,
                    Name                = c.Name,
                    LastKnownImageCount = c.LastKnownImageCount,
                    LastPageVisited     = c.LastPageVisited,
                })],
                entity.SearchConfiguration.SearchString,
                entity.SearchConfiguration.TopWallpapers,
                entity.SearchConfiguration.SearchStringPrefix,
                entity.SearchConfiguration.SearchStringSuffix,
                entity.SearchConfiguration.Subscriptions,
                entity.SearchConfiguration.ImagePauseInSeconds,
                entity.SearchConfiguration.StartingPageNumber,
                entity.SearchConfiguration.TotalPages,
                entity.SearchConfiguration.UseHeadless,
                entity.SearchConfiguration.SlowMotionDelay,
                entity.SearchConfiguration.SubscriptionsStartingPageNumber,
                entity.SearchConfiguration.SubscriptionsTotalPages,
                entity.SearchConfiguration.TopWallpapersTotalPages,
                entity.SearchConfiguration.TopWallpapersStartingPageNumber),
            new ScrapeDirectories(
                entity.ScrapeDirectories.RootDirectory,
                entity.ScrapeDirectories.BaseSaveDirectory,
                entity.ScrapeDirectories.BaseDirectory,
                entity.ScrapeDirectories.BaseDirectoryFamous,
                entity.ScrapeDirectories.SubDirectoryName));
}
