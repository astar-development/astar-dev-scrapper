using AStar.Dev.Infrastructure.FilesDb.Data;
using AStar.Dev.Infrastructure.FilesDb.Models;
using AStar.Dev.Wallpaper.Scrapper.Models;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public static class DataSeed
{
    public static async Task Seed(ScrapeConfiguration scrapeConfiguration, Logger logger, DTOs.TagsToIgnoreCompletely tagsToIgnoreCompletely, DTOs.TagsTextToIgnore tagsTextToIgnore, DTOs.ModelsToIgnore modelsToIgnore, FilesContext dbContext)
    {
        if (CheckDatabaseForMissingData(tagsToIgnoreCompletely, tagsTextToIgnore, dbContext, scrapeConfiguration, modelsToIgnore))
        {
            logger.Information("Updating database...");
            dbContext.TagsToIgnore.AddRange(tagsTextToIgnore.Tags.Select(tag => new TagToIgnore { Value = tag }));
            dbContext.ModelsToIgnore.AddRange(modelsToIgnore.Models.Select(model => new ModelToIgnore { Value = model.Value }));
            dbContext.ScrapeConfiguration.Add(new ScrapeConfigurationEntity
            {
                ConnectionStrings = new Infrastructure.FilesDb.Models.ConnectionStrings()
                {
                    Sqlite = scrapeConfiguration.ConnectionStrings.ToString()!
                },
                UserConfiguration = new Infrastructure.FilesDb.Models.UserConfiguration
                {
                    Username = scrapeConfiguration.UserConfiguration.Username,
                    Password = scrapeConfiguration.UserConfiguration.Password,
                    LoginEmailAddress = scrapeConfiguration.UserConfiguration.LoginEmailAddress,
                    SessionCookie = scrapeConfiguration.UserConfiguration.SessionCookie,
                },
                SearchConfiguration = new Infrastructure.FilesDb.Models.SearchConfiguration
                {
                    BaseUrl = scrapeConfiguration.SearchConfiguration.BaseUrl,
                    ApiKey = scrapeConfiguration.SearchConfiguration.ApiKey,
                    SearchCategories = [.. scrapeConfiguration.SearchConfiguration.SearchCategories.Select(category => new SearchCategories
                {
                    Id    = category.Id,
                    Name = category.Name,
                    LastKnownImageCount = category.LastKnownImageCount,
                    LastPageVisited = category.LastPageVisited,
                    TotalPages = category.TotalPages,
                })],
                    SearchString = scrapeConfiguration.SearchConfiguration.SearchString,
                    TopWallpapers = scrapeConfiguration.SearchConfiguration.TopWallpapers,
                },
                ScrapeDirectories = scrapeConfiguration.ScrapeDirectories.ToEntity(),
            });
            await dbContext.SaveChangesAsync();
        }
    }
    static bool CheckDatabaseForMissingData(DTOs.TagsToIgnoreCompletely tagsToIgnoreCompletely, DTOs.TagsTextToIgnore tagsTextToIgnore, FilesContext dbContext, ScrapeConfiguration scrapeConfiguration, DTOs.ModelsToIgnore modelsToIgnore)
        => (modelsToIgnore.Models.Count > 0 || tagsTextToIgnore.Tags.Count > 0 || tagsToIgnoreCompletely.Tags.Count > 0 || scrapeConfiguration.SearchConfiguration.SearchCategories.Length>0)
        && (!dbContext.TagsToIgnore.Any() || !dbContext.ModelsToIgnore.Any() || !dbContext.SearchCategories.Any());
}
