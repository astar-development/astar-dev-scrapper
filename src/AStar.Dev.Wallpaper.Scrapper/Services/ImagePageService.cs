using AStar.Dev.Infrastructure.FilesDb.Data;
using AStar.Dev.Wallpaper.Scrapper.ApiClient;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using Microsoft.EntityFrameworkCore;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public sealed class ImagePageService(ImagePage imagePage, Logger logger)
{
    public async Task ProcessWallpapersAsync(IReadOnlyCollection<WallhavenWallpaper> wallpapers)
    {
        foreach(var wallpaper in wallpapers)
        {
            try
            {
                using var context = new FilesContext(new DbContextOptions<FilesContext>());

                if(await context.Files.FirstOrDefaultAsync(f => f.FileName.Value.Contains(wallpaper.Id)) != null)
                {
                    logger.Information("Not downloading {id} as we already have it...", wallpaper.Id);

                    continue;
                }

                await imagePage.ProcessWallpaperAsync(wallpaper);
            }
            catch
            {
                Thread.Sleep(TimeSpan.FromSeconds(10));
                await imagePage.ProcessWallpaperAsync(wallpaper);
            }
        }
    }
}
