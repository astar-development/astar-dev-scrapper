using Shouldly;
using Xunit;

namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenUnit
{
    [Fact]
    public void unit_value_is_singleton_like()
    {
        var a = Unit.Value;
        var b = new Unit();

        a.ShouldBe(b);
        Unit.Value.ShouldBe(a);
    }
}
