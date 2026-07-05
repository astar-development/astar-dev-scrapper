namespace AStar.Dev.FunctionalParadigm;

/// <summary>
///     Functional helpers and utilities for working with <see cref="Exceptional{T}" />.
/// </summary>
public static class ExceptionalExtensions
{
    /// <summary>
    ///     Pattern matches on the <see cref="Exceptional{T}" />, invoking the handler for the case present.
    /// </summary>
    public static TOut Match<T, TOut>(this Exceptional<T> exceptional, Func<T, TOut> onSuccess, Func<Exception, TOut> onFailure) => throw new NotImplementedException();

    /// <summary>
    ///     Asynchronously pattern matches on the <see cref="Exceptional{T}" />, invoking the async success handler.
    /// </summary>
    public static Task<TOut> MatchAsync<T, TOut>(this Exceptional<T> exceptional, Func<T, Task<TOut>> onSuccess, Func<Exception, TOut> onFailure) => throw new NotImplementedException();

    /// <summary>
    ///     Transforms the value inside a <see cref="Exceptional{T}" /> if it is a <see cref="Success{T}" />.
    /// </summary>
    public static Exceptional<TResult> Map<T, TResult>(this Exceptional<T> exceptional, Func<T, TResult> selector) => throw new NotImplementedException();

    /// <summary>
    ///     Asynchronously transforms the value inside a <see cref="Exceptional{T}" /> if it is a <see cref="Success{T}" />.
    /// </summary>
    public static Task<Exceptional<TResult>> MapAsync<T, TResult>(this Exceptional<T> exceptional, Func<T, Task<TResult>> selector) => throw new NotImplementedException();

    /// <summary>
    ///     Asynchronously transforms the value inside a <see cref="Exceptional{T}" /> if it is a <see cref="Success{T}" />.
    /// </summary>
    public static ValueTask<Exceptional<TResult>> MapAsync<T, TResult>(this Exceptional<T> exceptional, Func<T, ValueTask<TResult>> selector) => throw new NotImplementedException();

    /// <summary>
    ///     Chains another <see cref="Exceptional{T}" />-producing function, short-circuiting on <see cref="Failure{T}" />.
    /// </summary>
    public static Exceptional<TResult> Bind<T, TResult>(this Exceptional<T> exceptional, Func<T, Exceptional<TResult>> binder) => throw new NotImplementedException();

    /// <summary>
    ///     Asynchronously chains another <see cref="Exceptional{T}" />-producing function, short-circuiting on <see cref="Failure{T}" />.
    /// </summary>
    public static Task<Exceptional<TResult>> BindAsync<T, TResult>(this Exceptional<T> exceptional, Func<T, Task<Exceptional<TResult>>> binder) => throw new NotImplementedException();

    /// <summary>
    ///     Executes a side-effect action for the case present, and returns the original <see cref="Exceptional{T}" />.
    /// </summary>
    public static Exceptional<T> Tap<T>(this Exceptional<T> exceptional, Action<T> onSuccess, Action<Exception>? onFailure = null) => throw new NotImplementedException();

    /// <summary>
    ///     Asynchronously executes a side-effect action for the case present, and returns the original <see cref="Exceptional{T}" />.
    /// </summary>
    public static Task<Exceptional<T>> TapAsync<T>(this Task<Exceptional<T>> exceptionalTask, Action<T> onSuccess, Action<Exception>? onFailure = null) => throw new NotImplementedException();

    /// <summary>
    ///     Lifts an <see cref="Exceptional{T}" /> into a <see cref="Result{TResult,TError}" />, mapping a captured
    ///     exception to a domain error via <paramref name="mapError" />.
    /// </summary>
    public static Result<T, TError> ToResult<T, TError>(this Exceptional<T> exceptional, Func<Exception, TError> mapError) => throw new NotImplementedException();
}
