using AStar.Dev.Wallpaper.Scrapper.Workflows;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Workflows;

public sealed class GivenAScrapeSiteWorkflowDecision
{
    [Fact]
    public async Task when_the_setup_step_fails_then_the_workflow_is_never_invoked()
    {
        int workflowInvocationCount = 0;

        Task<global::AStar.Dev.FunctionalParadigm.Result<global::AStar.Dev.FunctionalParadigm.Unit, string>> Workflow(CancellationToken ct)
        {
            workflowInvocationCount++;

            return Task.FromResult<global::AStar.Dev.FunctionalParadigm.Result<global::AStar.Dev.FunctionalParadigm.Unit, string>>(global::AStar.Dev.FunctionalParadigm.Unit.Value);
        }

        await ScrapeSiteWorkflowDecision.DecideAsync(new InvalidOperationException("setup failed"), Workflow);

        workflowInvocationCount.ShouldBe(0);
    }

    [Fact]
    public async Task when_the_setup_step_succeeds_then_the_workflow_is_invoked()
    {
        using var cts = new CancellationTokenSource();
        int workflowInvocationCount = 0;

        Task<global::AStar.Dev.FunctionalParadigm.Result<global::AStar.Dev.FunctionalParadigm.Unit, string>> Workflow(CancellationToken ct)
        {
            workflowInvocationCount++;

            return Task.FromResult<global::AStar.Dev.FunctionalParadigm.Result<global::AStar.Dev.FunctionalParadigm.Unit, string>>(global::AStar.Dev.FunctionalParadigm.Unit.Value);
        }

        await ScrapeSiteWorkflowDecision.DecideAsync(cts.Token, Workflow);

        workflowInvocationCount.ShouldBe(1);
    }
}
