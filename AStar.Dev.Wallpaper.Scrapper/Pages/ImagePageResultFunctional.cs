
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.FilesDb.Data;
using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scrapper.Pages;

public interface IImagePageResultFunctional
{
    Task<Result<Unit, string>> GetImagePagesAsync(Logger logger);
}

public class ImagePageResultFunctional(IDbContextFactory<FilesContext> dbContextFactory) : IImagePageResultFunctional
{
    /// <inheritdoc />
    public async Task<Result<Unit, string>> GetImagePagesAsync(Logger logger)
    {
        using var ctx = dbContextFactory.CreateDbContext();
        await ctx.AddAsync(new ScrapedTag
        {
            Id = ScrapedTagId.CreateNew(),
            Value = $"test-{ScrapedTagId.CreateNew()}"
        });
        await ctx.SaveChangesAsync();
        logger.Information("Image pages retrieved.");
        return Unit.Value;
    }
}