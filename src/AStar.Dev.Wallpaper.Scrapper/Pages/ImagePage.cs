using AStar.Dev.Utilities;
using AStar.Dev.Wallpaper.Scrapper.DTOs;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Repositories;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.Wallpaper.Scrapper.Support;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scrapper.Pages;

public sealed class ImagePage(IPlaywrightService playwrightService, ScrapeConfiguration scrapeConfiguration, TagsToIgnoreCompletely tagsToIgnoreCompletely, TagsTextToIgnore tagsTextToIgnore, IScrapedTagRepository scrapedTagRepository)
{
    private IPage page = null!;

    public async Task<ImagePageResult> GetImageFromPageAsync(string link, string categoryName)
    {
        page ??= await playwrightService.ConfigurePlaywrightAsync();
        _ = await page.GotoAsync(link);

        var tagLocators = await page.Locator(".tagname").AllAsync();
        var tagData = await Task.WhenAll(tagLocators.Select(GetTagsAsync));

        await scrapedTagRepository.SaveAsync([.. tagData.Where(t => !string.IsNullOrWhiteSpace(t.Category))]);

        string initialDirectory = scrapeConfiguration.ScrapeDirectories.BaseSaveDirectory.CombinePath(categoryName.Replace(' ', '-'));
        var context = TagRuleContextFactory.Create(initialDirectory, scrapeConfiguration.ScrapeDirectories.BaseDirectoryFamous, tagsToIgnoreCompletely, tagsTextToIgnore);

        var outcome = TagRules.Evaluate(tagData, context);

        return await MapOutcomeToResultAsync(outcome);
    }

    private async Task<ImagePageResult> MapOutcomeToResultAsync(TagOutcome outcome)
        => outcome switch
        {
            SkipImage skip => new ImagePageResult(null, [], string.Empty, true, skip.Tags),
            Accept accept => new ImagePageResult(await GetImageSourceAsync(), [.. accept.DirectorySegments], accept.FilePrefix, false, accept.Tags),
            _ => throw new InvalidOperationException("Unexpected tag outcome."),
        };

    private async Task<string?> GetImageSourceAsync()
    {
        var imageTag = page.Locator("#wallpaper");

        return await imageTag.GetAttributeAsync("src");
    }

    private static async Task<TagData> GetTagsAsync(ILocator tag)
    {
        string textTask = await tag.InnerTextAsync();
        string? attrTask = await tag.GetAttributeAsync("original-title");
        return new TagData(textTask, attrTask);
    }
}
