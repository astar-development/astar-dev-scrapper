using AStar.Dev.FunctionalParadigm;

namespace AStar.Dev.Wallpaper.Scrapper.Workflows;

/// <summary>
///     Models the decision made by <c>MainWindow.OnScrapeSiteFunctionalClicked</c>: the scrape workflow must only run
///     when the preceding setup step (resetting the cancellation token source / disabling controls) succeeded.
///     A failed setup step must short-circuit the chain, and the workflow must never be invoked.
/// </summary>
public static class ScrapeSiteWorkflowDecision
{
    /// <summary>
    ///     Decides whether <paramref name="workflow" /> should run, based on the outcome of the setup step.
    /// </summary>
    /// <param name="setupResult">The outcome of the setup step that must precede the scrape workflow.</param>
    /// <param name="workflow">The scrape workflow to invoke only when <paramref name="setupResult" /> is a success.</param>
    /// <returns>
    ///     The result of <paramref name="workflow" /> when <paramref name="setupResult" /> is a success; otherwise a
    ///     failure carrying the setup error, without invoking <paramref name="workflow" />.
    /// </returns>
    public static Task<Result<Unit, string>> DecideAsync(Result<CancellationToken, Exception> setupResult, Func<CancellationToken, Task<Result<Unit, string>>> workflow)
        => setupResult.MatchAsync(workflow, exception => Result.Failure<Unit, string>(exception.Message)).AsTask();
}
