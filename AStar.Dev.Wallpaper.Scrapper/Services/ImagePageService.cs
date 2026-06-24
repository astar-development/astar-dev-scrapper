using AStar.Dev.Infrastructure.FilesDb.Models;
using AStar.Dev.Utilities;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Repositories;
using AStar.Dev.Wallpaper.Scrapper.Support;
using Serilog.Core;
using SkiaSharp;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public sealed class ImagePageService(ImagePage imagePage, IFileDetailRepository fileDetailRepository, FileClassificationService fileClassificationService, ScrapeConfiguration scrapeConfiguration, Logger logger)
{
    public async Task GetTheImagePagesAsync(IReadOnlyCollection<string> imagePageLinks, CancellationToken ct = default)
    {
        foreach(var pageLink in imagePageLinks)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var fileName = Path.GetFileName(pageLink);

                if(await fileDetailRepository.ExistsAsync(fileName))
                {
                    logger.Information("Not downloading {fileName} as we already have it...", fileName);
                    continue;
                }

                await ProcessImagePageAsync(pageLink, ct);
            }
            catch(Exception ex) when (ex is not OperationCanceledException)
            {
                logger.Warning(ex, "Failed to process {pageLink}, retrying after delay.", pageLink);
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
                await ProcessImagePageAsync(pageLink, ct);
            }
        }
    }

    private async Task ProcessImagePageAsync(string pageLink, CancellationToken ct)
    {
        var delay = Random.Shared.Next(scrapeConfiguration.SearchConfiguration.ImagePauseInSeconds, scrapeConfiguration.SearchConfiguration.ImagePauseInSeconds + 4);
        await Task.Delay(TimeSpan.FromSeconds(delay), ct);

        var result = await imagePage.GetImageFromPage(pageLink);
        if(result.Skip || result.ImageUrl is null) return;

        var directoryName = DirectoryHelper.CreateDirectoryIfRequired(result.DirectoryName);

        var filename         = Path.GetFileName(result.ImageUrl);
        var fileNameCombined = !string.IsNullOrEmpty(result.FilePrefix) ? result.FilePrefix + " " + filename : filename;

        var imageNameWithPath = directoryName.Value.CombinePath(fileNameCombined.Replace(' ', '-').ToLowerInvariant());
        var image             = await ImageRetrieverHelper.GetTheImageAsync(result.ImageUrl);
        logger.Information("About to save {filename} as {imageNameWithPath} as we do not appear to have it.", filename, imageNameWithPath);
        await ImageSaveHelper.SaveImage(image, imageNameWithPath);

        var fileInfo   = new FileInfo(imageNameWithPath);
        var fileDetail = new FileDetail
        {
            DirectoryName = directoryName,
            FileName      = new FileName(filename),
            FileSize      = fileInfo.Length,
            IsImage       = filename.IsImage()
        };

        if(fileDetail.IsImage)
        {
            var imageDetail = SKImage.FromEncodedData(imageNameWithPath);
            if(imageDetail is not null)
            {
                fileDetail.Height      = imageDetail.Height;
                fileDetail.Width       = imageDetail.Width;
                fileDetail.ImageDetail = new ImageDetail { Width = imageDetail.Width, Height = imageDetail.Height };
            }
        }

        await fileDetailRepository.AddAsync(fileDetail);
        await fileClassificationService.ClassifyAsync(fileDetail);
    }
}
