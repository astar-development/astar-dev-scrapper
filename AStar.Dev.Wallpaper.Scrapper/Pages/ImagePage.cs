using AStar.Dev.Infrastructure.FilesDb.Data;
using AStar.Dev.Infrastructure.FilesDb.Models;
using AStar.Dev.Technical.Debt.Reporting;
using AStar.Dev.Wallpaper.Scrapper.DTOs;
using AStar.Dev.Wallpaper.Scrapper.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Serilog.Core;
using SkiaSharp;
using ScrapeDirectories = AStar.Dev.Wallpaper.Scrapper.Models.ScrapeDirectories;
using SearchConfiguration = AStar.Dev.Wallpaper.Scrapper.Models.SearchConfiguration;

namespace AStar.Dev.Wallpaper.Scrapper.Pages;

public sealed class ImagePage(
    IPage                  page,
    SearchConfiguration    searchConfiguration,
    ScrapeDirectories      scrapeDirectories,
    TagsToIgnoreCompletely tagsToIgnoreCompletely,
    TagsTextToIgnore       tagsTextToIgnore,
    Logger                 logger)
{
    public async Task GetImageFromPage(string link)
    {
        _ = await page.GotoAsync(link);

        IReadOnlyList<ILocator> tags          = await page.Locator(".tagname").AllAsync();
        var             directoryName = Path.Combine(scrapeDirectories.RootDirectory, scrapeDirectories.BaseSaveDirectory, scrapeDirectories.SubDirectoryName);
        var (directoryNameUpdated, filePrefix, skip) = await ProcessTheImageTags(tags, directoryName);

        await GetTheImage(skip, directoryNameUpdated, filePrefix);
    }

    private async Task<(string directoryName, string filePrefix, bool skip)> ProcessTheImageTags(IEnumerable<ILocator> tags, string directoryName)
    {
        var skip                     = false;
        var skip2                    = false;
        var filePrefix               = string.Empty;
        var alreadyContainsModelName = false;
        var tagsToAddToDatabase      = new List<string>();

        foreach(ILocator tag in tags)
        {
            var (tagText, tagToUse) = await GetTags(tag);

            if(tagToUse == null) continue;

            tagToUse = tagToUse.Trim();
            skip     = IsOneOfTheImageTagsToExcludeCompletely(tagToUse);
            skip2    = IsOneOfTheImageTagsToExcludeCompletely(tagText);

            if(skip || skip2) break;

            var (filePrefixUpdated, alreadyContainsModelNameUpdated, directoryNameUpdated) = UpdateFilePrefixForModels(tagToUse, tagText, filePrefix, alreadyContainsModelName, directoryName);

            alreadyContainsModelName = alreadyContainsModelNameUpdated;
            directoryName            = directoryNameUpdated;

            filePrefix = UpdateFilePrefixForVehicles(tagToUse, filePrefixUpdated);
            tagsToAddToDatabase.Add(tagText);

            if(UpdateToTagIsNotRequired(tagToUse, tagText, filePrefix)) continue;

            filePrefix    = string.Join("-", filePrefix, tagText);
            directoryName = scrapeDirectories.BaseDirectoryFamous + tagText;
        }

        filePrefix = UpdateFilePrefixIfRequired(filePrefix);

        return (directoryName, filePrefix, skip || skip2);
    }

    private static string UpdateFilePrefixIfRequired(string filePrefix)
    {
        if(filePrefix.StartsWith('-')) filePrefix = filePrefix[1..];

        return filePrefix;
    }

    private bool UpdateToTagIsNotRequired(string tagToUse, string tagText, string filePrefix)
        => TagIsNotCelebEtc(tagToUse) || FilePrefixDoesNotNeedUpdating(tagText, filePrefix);

    private static async Task<(string tagText, string? tagToUse)> GetTags(ILocator tag)
    {
        var tagText  = await tag.InnerTextAsync();
        var tagToUse = await tag.GetAttributeAsync("original-title");

        return (tagText, tagToUse);
    }

    private bool FilePrefixDoesNotNeedUpdating(string tagText, string filePrefix)
        => IsWantedText(tagText) || !filePrefix.Contains(tagText);

    private static bool TagIsNotCelebEtc(string tagToUse)
        => !TagContains(tagToUse,    "celeb")
           && !TagContains(tagToUse, "singer")
           && !TagContains(tagToUse, "actress");

    private static bool TagContains(string tagToUse, string contains)
        => tagToUse.Contains(contains, StringComparison.CurrentCultureIgnoreCase);

    private string UpdateFilePrefixForVehicles(string tagToUse, string filePrefix)
    {
        if(!TagContains(tagToUse, "Vehicles > Cars & Motorcycles")) return filePrefix;

        if(IsWantedFilePrefix(tagToUse, filePrefix)) filePrefix = string.Join("-", filePrefix, tagToUse);

        return filePrefix;
    }

    private bool IsWantedFilePrefix(string tagToUse, string filePrefix)
        => IsWantedText(tagToUse)                                             && !filePrefix.Contains(tagToUse) &&
           !tagToUse.Equals("car", StringComparison.CurrentCultureIgnoreCase) &&
           !TagContains(tagToUse, "cars");

    /// <summary>
    ///     This method suffers the same flaw as the earlier version: the previous "moreThan" and this "alreadyContains" don't actually work the way intended
    /// </summary>
    /// <param name="tagToUse">
    /// </param>
    /// <param name="tagText">
    /// </param>
    /// <param name="filePrefix">
    /// </param>
    /// <param name="alreadyContainsModelName">
    /// </param>
    /// <param name="directoryName">
    /// </param>
    /// <returns>
    /// </returns>
    private (string filePrefix, bool alreadyContainsModelName, string directoryName) UpdateFilePrefixForModels(string tagToUse,
                                                                                                               string tagText,
                                                                                                               string filePrefix,
                                                                                                               bool   alreadyContainsModelName,
                                                                                                               string directoryName)
    {
        var filePrefixUpdated = string.Empty;

        if(TagContains(tagToUse, "people > model") || TagContains(tagToUse, "people > porn"))
        {
            if(IsWantedText(tagText) && !filePrefix.Contains(tagText))
            {
                filePrefixUpdated = string.Join("-", filePrefix, tagText);

                if(!alreadyContainsModelName)
                {
                    directoryName            += $@"\..\named\{tagText}\{scrapeDirectories.SubDirectoryName}";
                    alreadyContainsModelName =  true;
                }
            }
        }

        var directoryNameUpdated = directoryName.Replace("/", "aka");

        return (filePrefixUpdated, alreadyContainsModelName, directoryNameUpdated);
    }

    [Refactor(2, 5, "Too long!")]
    private async Task GetTheImage(bool skip, string directoryName, string filePrefix)
    {
        if(!skip)
        {
            var delay = Random.Shared.Next(searchConfiguration.ImagePauseInSeconds, searchConfiguration.ImagePauseInSeconds + 4);
            Thread.Sleep(TimeSpan.FromSeconds(delay));
            directoryName = DirectoryHelper.CreateDirectoryIfRequired(directoryName);

            if(filePrefix.StartsWith("-")) filePrefix = filePrefix[1..];

            if(filePrefix.Contains('/')) filePrefix = filePrefix.Replace("/", "aka");

            ILocator imageTag   = page.Locator("#wallpaper");
            var sourcePath = await imageTag.GetAttributeAsync("src");

            if(sourcePath != null)
            {
                var index            = sourcePath.LastIndexOf("/", StringComparison.Ordinal) + 1;
                var filename         = sourcePath[index..];
                var fileNameCombined = string.Empty;

                if(!string.IsNullOrEmpty(filePrefix))
                    fileNameCombined += filePrefix + " " + filename;
                else
                    fileNameCombined = filename;

                var imageNameWithPath = directoryName + "\\" + fileNameCombined;
                var image             = await ImageRetrieverHelper.GetTheImageAsync(sourcePath);
                logger.Information("About to save {filename} as {imageNameWithPath} as we do not appear to have it.", filename, imageNameWithPath);
                await ImageSaveHelper.SaveImage(image, imageNameWithPath);
                var fileInfo = new FileInfo(imageNameWithPath);

                var fileDetail = new FileDetail { DirectoryName = new DirectoryName(directoryName), FileName = new FileName(filename), FileSize = fileInfo.Length, };

                if(fileDetail.IsImage)
                {
                    var imageDetail = SKImage.FromEncodedData(imageNameWithPath);

                    if(image is null)
                        File.Delete(imageNameWithPath);
                    else
                    {
                        fileDetail.Height = imageDetail.Height;
                        fileDetail.Width  = imageDetail.Width;
                    }
                }

                await using var context = new FilesContext(new DbContextOptions<FilesContext>());
                _ = await context.Files.AddAsync(fileDetail);
                _ = await context.SaveChangesAsync();
            }
        }
    }

    private bool IsOneOfTheImageTagsToExcludeCompletely(string tagText)
        => tagsToIgnoreCompletely.Tags.Contains(tagText);

    private bool IsWantedText(string tagText)
        => !tagsTextToIgnore.Tags.Contains(tagText) && !tagText.StartsWith("model", StringComparison.CurrentCultureIgnoreCase);
}
