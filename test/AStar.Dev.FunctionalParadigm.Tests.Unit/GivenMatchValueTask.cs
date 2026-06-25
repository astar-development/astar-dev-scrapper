using Shouldly;
using Xunit;

namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenMatchValueTask
{
    [Fact]
    public async Task when_both_handlers_are_valuetask_then_returns_expected()
    {
        var success = Result<int, string>.Success(6);
        var failure = Result<int, string>.Failure("errval");

        var actualSuccess = await success.MatchAsync(onSuccess: v => ValueTask.FromResult(v + 4), onFailure: e => ValueTask.FromResult(-1));
        var actualFailure = await failure.MatchAsync(onSuccess: v => ValueTask.FromResult(v + 4), onFailure: e => ValueTask.FromResult(e.Length));

        actualSuccess.ShouldBe(10);
        actualFailure.ShouldBe(6);
    }

    [Fact]
    public async Task when_on_success_is_valuetask_and_on_failure_sync_then_returns_expected()
    {
        var success = Result<int, string>.Success(2);
        var failure = Result<int, string>.Failure("abc");

        var actualSuccess = await success.MatchAsync(onSuccess: v => ValueTask.FromResult(v * 3), onFailure: e => -1);
        var actualFailure = await failure.MatchAsync(onSuccess: v => ValueTask.FromResult(v * 3), onFailure: e => e.Length);

        actualSuccess.ShouldBe(6);
        actualFailure.ShouldBe(3);
    }
}
