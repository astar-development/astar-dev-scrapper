namespace AStar.Dev.FunctionalParadigm;

/// <summary>
///     Functional helpers and utilities for working with <see cref="Exceptional{T}" />.
/// </summary>
public static class ExceptionalExtensions
{
    /// <summary>
    ///     Pattern matches on the <see cref="Exceptional{T}" />, invoking the handler for the case present.
    /// </summary>
    public static TOut Match<T, TOut>(this Exceptional<T> exceptional, Func<T, TOut> onSuccess, Func<Exception, TOut> onFailure)
        => exceptional switch
            {
                Success<T> success => onSuccess(success.Value),
                Failure<T> failure => onFailure(failure.Exception),
                _ => throw new InvalidOperationException("Unexpected exceptional type.")
            };

    /// <summary>
    ///     Asynchronously pattern matches on the <see cref="Exceptional{T}" />, invoking the async success handler.
    /// </summary>
    public static async Task<TOut> MatchAsync<T, TOut>(this Exceptional<T> exceptional, Func<T, Task<TOut>> onSuccess, Func<Exception, TOut> onFailure)
        => exceptional switch
            {
                Success<T> success => await onSuccess(success.Value).ConfigureAwait(false),
                Failure<T> failure => onFailure(failure.Exception),
                _ => throw new InvalidOperationException("Unexpected exceptional type.")
            };

    /// <summary>
    ///     Transforms the value inside a <see cref="Exceptional{T}" /> if it is a <see cref="Success{T}" />.
    /// </summary>
    public static Exceptional<TResult> Map<T, TResult>(this Exceptional<T> exceptional, Func<T, TResult> selector)
        => exceptional switch
            {
                Success<T> success => new Success<TResult>(selector(success.Value)),
                Failure<T> failure => new Failure<TResult>(failure.Exception),
                _ => throw new InvalidOperationException("Unexpected exceptional type.")
            };

    /// <summary>
    ///     Asynchronously transforms the value inside a <see cref="Exceptional{T}" /> if it is a <see cref="Success{T}" />.
    /// </summary>
    public static async Task<Exceptional<TResult>> MapAsync<T, TResult>(this Exceptional<T> exceptional, Func<T, Task<TResult>> selector)
        => exceptional switch
            {
                Success<T> success => new Success<TResult>(await selector(success.Value).ConfigureAwait(false)),
                Failure<T> failure => new Failure<TResult>(failure.Exception),
                _ => throw new InvalidOperationException("Unexpected exceptional type.")
            };

    /// <summary>
    ///     Asynchronously transforms the value inside a <see cref="Exceptional{T}" /> if it is a <see cref="Success{T}" />.
    /// </summary>
    public static async ValueTask<Exceptional<TResult>> MapAsync<T, TResult>(this Exceptional<T> exceptional, Func<T, ValueTask<TResult>> selector)
        => exceptional switch
            {
                Success<T> success => new Success<TResult>(await selector(success.Value).ConfigureAwait(false)),
                Failure<T> failure => new Failure<TResult>(failure.Exception),
                _ => throw new InvalidOperationException("Unexpected exceptional type.")
            };

    /// <summary>
    ///     Chains another <see cref="Exceptional{T}" />-producing function, short-circuiting on <see cref="Failure{T}" />.
    /// </summary>
    public static Exceptional<TResult> Bind<T, TResult>(this Exceptional<T> exceptional, Func<T, Exceptional<TResult>> binder)
        => exceptional switch
            {
                Success<T> success => binder(success.Value),
                Failure<T> failure => new Failure<TResult>(failure.Exception),
                _ => throw new InvalidOperationException("Unexpected exceptional type.")
            };

    /// <summary>
    ///     Asynchronously chains another <see cref="Exceptional{T}" />-producing function, short-circuiting on <see cref="Failure{T}" />.
    /// </summary>
    public static async Task<Exceptional<TResult>> BindAsync<T, TResult>(this Exceptional<T> exceptional, Func<T, Task<Exceptional<TResult>>> binder)
        => exceptional switch
            {
                Success<T> success => await binder(success.Value).ConfigureAwait(false),
                Failure<T> failure => new Failure<TResult>(failure.Exception),
                _ => throw new InvalidOperationException("Unexpected exceptional type.")
            };

    /// <summary>
    ///     Executes a side-effect action for the case present, and returns the original <see cref="Exceptional{T}" />.
    /// </summary>
    public static Exceptional<T> Tap<T>(this Exceptional<T> exceptional, Action<T> onSuccess, Action<Exception>? onFailure = null)
    {
        switch (exceptional)
        {
            case Success<T> success:
                onSuccess(success.Value);
                return success;

            case Failure<T> failure:
                onFailure?.Invoke(failure.Exception);
                return failure;

            default:
                throw new InvalidOperationException("Unexpected exceptional type.");
        }
    }

    /// <summary>
    ///     Asynchronously executes a side-effect action for the case present, and returns the original <see cref="Exceptional{T}" />.
    /// </summary>
    public static async Task<Exceptional<T>> TapAsync<T>(this Task<Exceptional<T>> exceptionalTask, Action<T> onSuccess, Action<Exception>? onFailure = null)
    {
        var exceptional = await exceptionalTask.ConfigureAwait(false);
        return exceptional.Tap(onSuccess, onFailure);
    }

    /// <summary>
    ///     Lifts an <see cref="Exceptional{T}" /> into a <see cref="Result{TResult,TError}" />, mapping a captured
    ///     exception to a domain error via <paramref name="mapError" />.
    /// </summary>
    public static Result<T, TError> ToResult<T, TError>(this Exceptional<T> exceptional, Func<Exception, TError> mapError)
        => exceptional switch
            {
                Success<T> success => new Ok<T, TError>(success.Value),
                Failure<T> failure => new Fail<T, TError>(mapError(failure.Exception)),
                _ => throw new InvalidOperationException("Unexpected exceptional type.")
            };
}
