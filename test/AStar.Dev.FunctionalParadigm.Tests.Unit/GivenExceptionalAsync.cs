using Shouldly;
using Xunit;

namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenExceptionalAsync
{
    [Fact]
    public async Task when_map_async_task_selector_and_success_then_transforms_value()
    {
        var exceptional = Exceptional.Success(5);

        var actual = await exceptional.MapAsync(value => Task.FromResult(value * 2));

        actual.ShouldBeOfType<Success<int>>();
        actual.ShouldBe(new Success<int>(10));
    }

    [Fact]
    public async Task when_map_async_value_task_selector_and_success_then_transforms_value()
    {
        var exceptional = Exceptional.Success(5);

        var actual = await exceptional.MapAsync(value => ValueTask.FromResult(value * 2));

        actual.ShouldBeOfType<Success<int>>();
        actual.ShouldBe(new Success<int>(10));
    }

    [Fact]
    public async Task when_map_async_and_failure_then_returns_same_failure()
    {
        var exception = new InvalidOperationException("bad");
        var exceptional = Exceptional.Failure<int>(exception);

        var actual = await exceptional.MapAsync(value => Task.FromResult(value * 2));

        actual.ShouldBeOfType<Failure<int>>();
        actual.ShouldBe(new Failure<int>(exception));
    }

    [Fact]
    public async Task when_bind_async_and_success_then_returns_bound_result()
    {
        var exceptional = Exceptional.Success(3);

        var actual = await exceptional.BindAsync(value => Task.FromResult(Exceptional.Success(value * 4)));

        actual.ShouldBeOfType<Success<int>>();
        actual.ShouldBe(new Success<int>(12));
    }

    [Fact]
    public async Task when_bind_async_and_failure_then_binder_not_invoked()
    {
        var exception = new InvalidOperationException("fail");
        var exceptional = Exceptional.Failure<int>(exception);
        var invoked = false;

        var actual = await exceptional.BindAsync(value =>
        {
            invoked = true;
            return Task.FromResult(Exceptional.Success(value));
        });

        actual.ShouldBeOfType<Failure<int>>();
        invoked.ShouldBeFalse();
    }

    [Fact]
    public async Task when_tap_async_on_task_of_success_then_executes_handler_and_returns_same()
    {
        var exceptionalTask = Task.FromResult(Exceptional.Success(9));
        var sideEffect = false;

        var actual = await exceptionalTask.TapAsync(value => sideEffect = value == 9);

        actual.ShouldBeOfType<Success<int>>();
        actual.ShouldBe(new Success<int>(9));
        sideEffect.ShouldBeTrue();
    }

    [Fact]
    public async Task when_tap_async_on_task_of_failure_then_executes_failure_handler()
    {
        var exception = new InvalidOperationException("boom");
        var exceptionalTask = Task.FromResult(Exceptional.Failure<int>(exception));
        var sideEffect = false;

        var actual = await exceptionalTask.TapAsync(_ => { }, ex => sideEffect = ex == exception);

        actual.ShouldBeOfType<Failure<int>>();
        sideEffect.ShouldBeTrue();
    }

    [Fact]
    public async Task when_match_async_on_success_then_invokes_async_success_handler()
    {
        var exceptional = Exceptional.Success(3);

        var actual = await exceptional.MatchAsync(onSuccess: value => Task.FromResult(value + 1), onFailure: _ => -1);

        actual.ShouldBe(4);
    }

    [Fact]
    public async Task when_match_async_on_failure_then_invokes_sync_failure_handler()
    {
        var exception = new InvalidOperationException("err");
        var exceptional = Exceptional.Failure<int>(exception);

        var actual = await exceptional.MatchAsync(onSuccess: value => Task.FromResult(value + 1), onFailure: ex => ex.Message.Length);

        actual.ShouldBe(3);
    }
}
