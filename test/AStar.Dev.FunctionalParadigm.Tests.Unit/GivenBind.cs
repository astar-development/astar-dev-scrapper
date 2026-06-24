using Shouldly;
using Xunit;

namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenBind
{
    [Fact]
    public void when_binder_returns_success_then_returns_bound_result()
    {
        var result = Result<int, string>.Success(2);

        var actual = result.Bind(value => Result<int, string>.Success(value * 5));

        actual.ShouldBeOfType<Ok<int, string>>();
        actual.ShouldBe(new Ok<int, string>(10));
    }

    [Fact]
    public void when_result_is_failure_then_returns_failure()
    {
        var result = Result<int, string>.Failure("err");

        var actual = result.Bind(value => Result<int, string>.Success(value * 5));

        actual.ShouldBeOfType<Fail<int, string>>();
        actual.ShouldBe(new Fail<int, string>("err"));
    }
}
