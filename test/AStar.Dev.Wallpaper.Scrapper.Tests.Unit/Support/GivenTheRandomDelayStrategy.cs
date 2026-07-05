using AStar.Dev.Wallpaper.Scrapper.Support;
using AStar.Dev.Wallpaper.Scrapper.Tests.Unit.TestData;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Support;

public sealed class GivenTheRandomDelayStrategy
{
    private readonly IDelayStrategy sut = new RandomDelayStrategy(new ScrapeConfigurationBuilder().Build());

    [Theory]
    [InlineData(DelayKind.CategoryUpToDate)]
    [InlineData(DelayKind.PageNavigation)]
    [InlineData(DelayKind.ImageAlreadyDownloaded)]
    [InlineData(DelayKind.BeforeImage)]
    [InlineData(DelayKind.Retry)]
    public async Task when_the_cancellation_token_is_already_cancelled_then_delay_async_throws(DelayKind delayKind)
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.DelayAsync(delayKind, cts.Token));
    }
}
