using AStar.Dev.Utilities;
using AStar.Dev.Wallpaper.Scrapper.DTOs;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Repositories;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scrapper.Pages;

public sealed class ImagePage(IPage page, ScrapeConfiguration scrapeConfiguration, TagsToIgnoreCompletely tagsToIgnoreCompletely, TagsTextToIgnore tagsTextToIgnore, IScrapedTagRepository scrapedTagRepository)
{
    public async Task<ImagePageResult> GetImageFromPage(string link)
    {
        _ = await page.GotoAsync(link);

        IReadOnlyList<ILocator> tags          = await page.Locator(".tagname").AllAsync();
        var                     directoryName = scrapeConfiguration.ScrapeDirectories.BaseSaveDirectory.CombinePath(scrapeConfiguration.ScrapeDirectories.SubDirectoryName);
        var (directoryNameUpdated, filePrefix, skip) = await ProcessTheImageTags(tags, directoryName);

        if(skip) return new ImagePageResult(null, directoryNameUpdated, filePrefix, Skip: true);

        ILocator imageTag   = page.Locator("#wallpaper");
        var      sourcePath = await imageTag.GetAttributeAsync("src");
        return new ImagePageResult(sourcePath, directoryNameUpdated, filePrefix, Skip: false);
    }

    private async Task<(string directoryName, string filePrefix, bool skip)> ProcessTheImageTags(IEnumerable<ILocator> tags, string directoryName)
    {
        var skip                     = false;
        var filePrefix               = string.Empty;
        var alreadyContainsModelName = false;

        var tagData = await Task.WhenAll(tags.Select(GetTags));

        await scrapedTagRepository.SaveAsync([.. tagData.Select(t => t).Where(t => !string.IsNullOrWhiteSpace(t.Category))]);

        foreach(var (tagText, tagToUse) in tagData)
        {
            if(tagToUse == null) continue;

            var trimmedTagToUse = tagToUse.Trim();
            skip = IsOneOfTheImageTagsToExcludeCompletely(trimmedTagToUse) || IsOneOfTheImageTagsToExcludeCompletely(tagText);

            if(skip) break;

            var (filePrefixUpdated, alreadyContainsModelNameUpdated, directoryNameUpdated) = UpdateFilePrefixForModels(trimmedTagToUse, tagText, filePrefix, alreadyContainsModelName, directoryName);

            alreadyContainsModelName = alreadyContainsModelNameUpdated;
            directoryName            = directoryNameUpdated;

            filePrefix = UpdateFilePrefixForVehicles(trimmedTagToUse, filePrefixUpdated);

            if(UpdateToTagIsNotRequired(trimmedTagToUse, tagText, filePrefix)) continue;

            filePrefix    = string.Join("-", filePrefix, tagText);
            directoryName = scrapeConfiguration.ScrapeDirectories.BaseDirectoryFamous + tagText;
        }

        filePrefix = UpdateFilePrefixIfRequired(filePrefix);

        return (directoryName, filePrefix, skip);
    }

    private static string UpdateFilePrefixIfRequired(string filePrefix)
    {
        if(filePrefix.StartsWith('-')) filePrefix = filePrefix[1..];

        return filePrefix;
    }

    private bool UpdateToTagIsNotRequired(string tagToUse, string tagText, string filePrefix)
        => TagIsNotCelebEtc(tagToUse) || FilePrefixDoesNotNeedUpdating(tagText, filePrefix);

    private static async Task<TagData> GetTags(ILocator tag)
    {
        var textTask = await tag.InnerTextAsync();
        var attrTask = await tag.GetAttributeAsync("original-title");
        return new TagData(textTask, attrTask);
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

    private (string filePrefix, bool alreadyContainsModelName, string directoryName) UpdateFilePrefixForModels(string tagToUse,
                                                                                                               string tagText,
                                                                                                               string filePrefix,
                                                                                                               bool   alreadyContainsModelName,
                                                                                                               string directoryName)
    {
        var filePrefixUpdated = filePrefix;

        if(TagContains(tagToUse, "people > model") || TagContains(tagToUse, "people > porn"))
        {
            if(IsWantedText(tagText) && !filePrefix.Contains(tagText))
            {
                filePrefixUpdated = string.Join("-", filePrefix, tagText);

                if(!alreadyContainsModelName)
                {
                    directoryName            = directoryName.CombinePath("..", "named", tagText, scrapeConfiguration.ScrapeDirectories.SubDirectoryName);
                    alreadyContainsModelName = true;
                }
            }
        }

        return (filePrefixUpdated, alreadyContainsModelName, directoryName);
    }

    private bool IsOneOfTheImageTagsToExcludeCompletely(string tagText)
        => tagsToIgnoreCompletely.Tags.Contains(tagText);

    private bool IsWantedText(string tagText)
        => !tagsTextToIgnore.Tags.Contains(tagText) && !tagText.StartsWith("model", StringComparison.CurrentCultureIgnoreCase);
}
