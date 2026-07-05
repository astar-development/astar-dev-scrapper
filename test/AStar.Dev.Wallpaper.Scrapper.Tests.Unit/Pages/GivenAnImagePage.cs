using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Repositories;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.Wallpaper.Scrapper.Tests.Unit.TestData;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Pages;

public sealed class GivenAnImagePage
{
    private const string Link = "https://example.test/w/12345";
    private const string CategoryName = "Cars";

    [Fact]
    public async Task when_getting_the_image_from_the_page_then_the_scraped_tag_repository_is_saved_to_exactly_once()
    {
        var tagLocator = Substitute.For<ILocator>();
        tagLocator.InnerTextAsync().Returns(Task.FromResult("Sports Car"));
        tagLocator.GetAttributeAsync("original-title").Returns(Task.FromResult<string?>("Vehicles > Cars & Motorcycles"));

        var tagsLocator = Substitute.For<ILocator>();
        tagsLocator.AllAsync().Returns(Task.FromResult<IReadOnlyList<ILocator>>([tagLocator,]));

        var imageLocator = Substitute.For<ILocator>();
        imageLocator.GetAttributeAsync("src").Returns(Task.FromResult<string?>("https://example.test/images/12345.jpg"));

        var page = Substitute.For<IPage>();
        page.Locator(".tagname", Arg.Any<PageLocatorOptions>()).Returns(tagsLocator);
        page.Locator("#wallpaper", Arg.Any<PageLocatorOptions>()).Returns(imageLocator);

        var playwrightService = Substitute.For<IPlaywrightService>();
        playwrightService.ConfigurePlaywrightAsync().Returns(Task.FromResult(page));

        var scrapedTagRepository = Substitute.For<IScrapedTagRepository>();
        var scrapeConfiguration = new ScrapeConfigurationBuilder().Build();

        var sut = new ImagePage(playwrightService, scrapeConfiguration, new(), new(), scrapedTagRepository);

        await sut.GetImageFromPageAsync(Link, CategoryName);

        await scrapedTagRepository.Received(1).SaveAsync(Arg.Any<IReadOnlyList<TagData>>());
    }
}
