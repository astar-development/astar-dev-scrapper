using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.Wallpaper.Scrapper.Tests.Unit.TestData;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Pages;

public sealed class GivenATopWallpapersPage
{
    [Fact]
    public async Task when_loading_the_top_wallpapers_page_for_the_first_time_then_no_exception_is_thrown()
    {
        var response = Substitute.For<IResponse>();

        var page = Substitute.For<IPage>();
        page.GotoAsync(Arg.Any<string>(), Arg.Any<PageGotoOptions>()).Returns(Task.FromResult<IResponse?>(response));

        var playwrightService = Substitute.For<IPlaywrightService>();
        playwrightService.ConfigurePlaywrightAsync().Returns(Task.FromResult(page));

        var searchConfiguration = new SearchConfigurationBuilder().Build();
        var sut = new TopWallpapersPage(playwrightService, searchConfiguration);

        var exception = await Record.ExceptionAsync(() => sut.LoadTopWallpapersPageAsync(1));

        exception.ShouldBeNull();
    }

    [Fact]
    public async Task when_getting_the_page_info_for_the_first_time_then_no_exception_is_thrown()
    {
        var locator = Substitute.For<ILocator>();
        locator.First.Returns(locator);
        locator.TextContentAsync().Returns(Task.FromResult<string?>("Page 1 / 5"));

        var page = Substitute.For<IPage>();
        page.GetByText(Arg.Any<string>(), Arg.Any<PageGetByTextOptions>()).Returns(locator);

        var playwrightService = Substitute.For<IPlaywrightService>();
        playwrightService.ConfigurePlaywrightAsync().Returns(Task.FromResult(page));

        var searchConfiguration = new SearchConfigurationBuilder().Build();
        var sut = new TopWallpapersPage(playwrightService, searchConfiguration);

        var exception = await Record.ExceptionAsync(() => sut.PageInfoAsync());

        exception.ShouldBeNull();
    }
}
