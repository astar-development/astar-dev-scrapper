using AStar.Dev.Infrastructure.FilesDb.Data;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using Microsoft.EntityFrameworkCore;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public sealed class ImagePageService(ImagePage imagePage, ScrapeConfiguration scrapeConfiguration, Logger logger)
{
    public async Task GetTheImagePagesAsync(IReadOnlyCollection<string> imagePageLinks)
    {
        foreach(var pageLink in imagePageLinks)
        {
            try
            {
                var indexOfFinalSlash = pageLink.LastIndexOf('/') + 1;
                var fileName          = pageLink[indexOfFinalSlash..];                

                var options = new DbContextOptionsBuilder<FilesContext>()
                    .UseSqlite(scrapeConfiguration.ConnectionStrings.Sqlite)
                    .Options;
                await using var context = new FilesContext(options);

                if(await context.Files.FirstOrDefaultAsync(fileInfoJb => fileInfoJb.FileName.Value.Contains(fileName)) != null)
                {
                    logger.Information("Not downloading {fileName} as we already have it...", fileName);

                    continue;
                }

                await imagePage.GetImageFromPage(pageLink);
            }
            catch
            {
                Thread.Sleep(TimeSpan.FromSeconds(10));
                await imagePage.GetImageFromPage(pageLink);
            }
        }
    }
}
