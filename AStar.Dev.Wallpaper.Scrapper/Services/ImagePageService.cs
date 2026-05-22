using AStar.Dev.Infrastructure.FilesDb.Models;
using AStar.Dev.Utilities;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Repositories;
using AStar.Dev.Wallpaper.Scrapper.Support;
using Serilog.Core;
using SkiaSharp;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public sealed class ImagePageService(ImagePage imagePage, FileDetailRepository fileDetailRepository, ScrapeConfiguration scrapeConfiguration, Logger logger)
{
    public async Task GetTheImagePagesAsync(IReadOnlyCollection<string> imagePageLinks)
    {
        foreach(var pageLink in imagePageLinks)
        {
            try
            {
                var indexOfFinalSlash = pageLink.LastIndexOf('/') + 1;
                var fileName          = pageLink[indexOfFinalSlash..];

                if(await fileDetailRepository.ExistsAsync(fileName))
                {
                    logger.Information("Not downloading {fileName} as we already have it...", fileName);
                    continue;
                }

                await ProcessImagePageAsync(pageLink);
            }
            catch
            {
                await Task.Delay(TimeSpan.FromSeconds(10));
                await ProcessImagePageAsync(pageLink);
            }
        }
    }

    private async Task ProcessImagePageAsync(string pageLink)
    {
        var delay = Random.Shared.Next(scrapeConfiguration.SearchConfiguration.ImagePauseInSeconds, scrapeConfiguration.SearchConfiguration.ImagePauseInSeconds + 4);
        await Task.Delay(TimeSpan.FromSeconds(delay));

        var result = await imagePage.GetImageFromPage(pageLink);
        if(result.Skip || result.ImageUrl is null) return;

        var directoryName = DirectoryHelper.CreateDirectoryIfRequired(result.DirectoryName);
        var filePrefix    = result.FilePrefix;

        var index            = result.ImageUrl.LastIndexOf('/') + 1;
        var filename         = result.ImageUrl[index..];
        var fileNameCombined = !string.IsNullOrEmpty(filePrefix) ? filePrefix + " " + filename : filename;

        var imageNameWithPath = directoryName.CombinePath(fileNameCombined);
        var image             = await ImageRetrieverHelper.GetTheImageAsync(result.ImageUrl);
        logger.Information("About to save {filename} as {imageNameWithPath} as we do not appear to have it.", filename, imageNameWithPath);
        await ImageSaveHelper.SaveImage(image, imageNameWithPath);

        var fileInfo   = new FileInfo(imageNameWithPath);
        var fileDetail = new FileDetail
        {
            DirectoryName = new DirectoryName(directoryName),
            FileName      = new FileName(filename),
            FileSize      = fileInfo.Length,
            IsImage       = filename.IsImage()
        };

        if(fileDetail.IsImage)
        {
            var imageDetail = SKImage.FromEncodedData(imageNameWithPath);
            if(image is null)
                File.Delete(imageNameWithPath);
            else
            {
                fileDetail.Height      = imageDetail.Height;
                fileDetail.Width       = imageDetail.Width;
                fileDetail.ImageDetail = new ImageDetail { Width = imageDetail.Width, Height = imageDetail.Height };
            }
        }

        await fileDetailRepository.AddAsync(fileDetail);
    }
}
