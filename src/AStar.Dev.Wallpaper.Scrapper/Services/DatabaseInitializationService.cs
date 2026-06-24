using AStar.Dev.Infrastructure.FilesDb.Data;
using AStar.Dev.Wallpaper.Scrapper.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public class DatabaseInitializationService(
    IDbContextFactory<FilesContext> contextFactory,
    IOptions<ScrapeConfiguration> scrapeConfigOptions,
    Logger logger)
{
    public async Task InitialiseAsync()
    {
        await using var context = contextFactory.CreateDbContext();

        await context.Database.MigrateAsync();

        await DataSeed.Seed(scrapeConfigOptions.Value, logger, context);

        var csvPath = Path.Combine(ApplicationMetadata.ApplicationFolder, "Mappings.csv");
        await DataSeed.SeedFileClassificationsAsync(csvPath, logger, context);
    }
}
