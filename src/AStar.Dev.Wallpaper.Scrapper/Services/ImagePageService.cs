using System.IO.Abstractions;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Utilities;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Repositories;
using AStar.Dev.Wallpaper.Scrapper.Support;
using Serilog.Core;
using SkiaSharp;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public sealed class ImagePageService(ImagePage imagePage, IFileDetailRepository fileDetailRepository, FileClassificationService fileClassificationService, ScrapeConfiguration scrapeConfiguration, TimeProvider timeProvider, Logger logger, IDirectoryHelper directoryHelper, ImageBroadcaster imageBroadcaster, IDelayStrategy delayStrategy, IImageRetriever imageRetriever, IImageSaver imageSaver, IFileSystem fileSystem)
{
    private const int LoggedPathTailLength = 50;

    /// <summary>Retained for constructor-signature stability. The image-pause delay it previously drove now lives in <see cref="RandomDelayStrategy" />, which is configured with its own <see cref="Models.ScrapeConfiguration" />.</summary>
    private readonly ScrapeConfiguration scrapeConfiguration = scrapeConfiguration;

    public async Task GetTheImagePagesAsync(IReadOnlyCollection<string> imagePageLinks, string categoryId, string name, CancellationToken ct = default)
    {
        var pageData = await fileClassificationService.LoadPageClassificationDataAsync(categoryId, ct).ConfigureAwait(false);

        foreach (string pageLink in imagePageLinks)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                string fileName = Path.GetFileName(pageLink);

                if (await fileDetailRepository.ExistsAsync(fileName).ConfigureAwait(false))
                {
                    logger.Information("Not downloading {fileName} as we already have it...{Timestamp:HH:mm:ss:fff} (UTC)", fileName, timeProvider.GetUtcNow());
                    await delayStrategy.DelayAsync(DelayKind.ImageAlreadyDownloaded, ct).ConfigureAwait(false);
                    continue;
                }

                await ProcessImagePageAsync(pageLink, name, pageData, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.Warning(ex, "Failed to process {pageLink}, retrying after delay.", pageLink);
                await delayStrategy.DelayAsync(DelayKind.Retry, ct).ConfigureAwait(false);
                await ProcessImagePageAsync(pageLink, name, pageData, ct).ConfigureAwait(false);
            }
        }
    }

    private async Task ProcessImagePageAsync(string pageLink, string name, PageClassificationData pageData, CancellationToken ct)
    {
        try
        {
            await delayStrategy.DelayAsync(DelayKind.BeforeImage, ct).ConfigureAwait(false);

            var result = await imagePage.GetImageFromPageAsync(pageLink, name).ConfigureAwait(false);
            if (result.Skip || result.ImageUrl is null)
            {
                logger.Information("Skipping {Name} with Tags: {Tags}", name, string.Join(", ", result.Tags));
                return;
            }

            var directoryName = directoryHelper.CreateDirectoryIfRequired(result.DirectoryName);

            string filename = ScrapedFileNameFactory.Create(result.FilePrefix, result.ImageUrl);

            string imageNameWithPath = directoryName.Value.CombinePath(filename);
            byte[] image = (await imageRetriever.GetImageAsync(result.ImageUrl, ct).ConfigureAwait(false))
                .Match(bytes => bytes, error => throw new InvalidOperationException(error.Message));
            logger.Information("About to save {filename} to ...{imageNameWithPath} as we don't appear to have it.", filename, TruncatedForLogging(imageNameWithPath));
            await imageSaver.SaveAsync(image, imageNameWithPath).ConfigureAwait(false);
            imageBroadcaster.Broadcast(imageNameWithPath);

            var fileInfo = fileSystem.FileInfo.New(imageNameWithPath);
            var fileDetail = new FileDetailEntity
            {
                DirectoryName = directoryName,
                FileName = new FileName(filename),
                FileSize = fileInfo.Length,
                IsImage = filename.IsImage()
            };

            if (fileDetail.IsImage)
            {
                var imageDetail = SKImage.FromEncodedData(imageNameWithPath);
                if (imageDetail is not null)
                {
                    fileDetail.Height = imageDetail.Height;
                    fileDetail.Width = imageDetail.Width;
                    fileDetail.ImageDetail = new ImageDetailEntity { Width = imageDetail.Width, Height = imageDetail.Height };
                }
            }

            await fileDetailRepository.AddAsync(fileDetail).ConfigureAwait(false);
            await fileClassificationService.ClassifyAsync(fileDetail, pageData, result.Tags, ct).ConfigureAwait(false);
        }
        catch(TaskCanceledException)
        {
            logger.Warning("Task was cancelled while processing {pageLink}.", pageLink);
            throw;
        }
        catch (OperationCanceledException)
        {
            logger.Warning("Operation was cancelled while processing {pageLink}.", pageLink);
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.Error(ex, "Error processing image page {pageLink}: {Message}", pageLink, ex.Message);
            throw;
        }
    }

    private static string TruncatedForLogging(string imageNameWithPath)
        => imageNameWithPath.Length > LoggedPathTailLength ? imageNameWithPath[^LoggedPathTailLength..] : imageNameWithPath;
}
