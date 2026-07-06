using AStar.Dev.FunctionalParadigm;
using Serilog;

namespace AStar.Dev.Wallpaper.Scrapper.Models;

/// <summary>Logging extensions for <see cref="Result{TResult,TError}" /> pipelines that fail with a <see cref="ScrapeError" />.</summary>
public static class ScrapeErrorLoggingExtensions
{
    /// <summary>Logs an error when <paramref name="result" /> is a failure, then returns <paramref name="result" /> unchanged.</summary>
    public static Result<TResult, ScrapeError> LogFailure<TResult>(this Result<TResult, ScrapeError> result, ILogger logger)
        => result.Tap(_ => { }, error => logger.Error("Scrape pipeline failure: {Message}", error.Message));
}
