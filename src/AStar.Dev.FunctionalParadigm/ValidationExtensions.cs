namespace AStar.Dev.FunctionalParadigm;

/// <summary>
///     Functional helpers and utilities for working with <see cref="Validation{T}" />, including the
///     applicative <see cref="Apply{T,TResult}" />/<see cref="Combine{T}" /> operations that accumulate
///     errors instead of stopping at the first failure.
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    ///     Applies a validated function to a validated value. When both sides are invalid, the errors from
    ///     both are accumulated (function errors first, then value errors) into a single <see cref="Invalid{T}" />.
    /// </summary>
    public static Validation<TResult> Apply<T, TResult>(this Validation<Func<T, TResult>> validationFunc, Validation<T> validationValue) => throw new NotImplementedException();

    /// <summary>
    ///     Combines a sequence of validations into a single <see cref="Validation{T}" /> of the ordered values.
    ///     When one or more validations are invalid, all of their errors are accumulated, in encounter order,
    ///     into a single <see cref="Invalid{T}" />.
    /// </summary>
    public static Validation<IReadOnlyList<T>> Combine<T>(this IEnumerable<Validation<T>> validations) => throw new NotImplementedException();

    /// <summary>
    ///     Lifts a <see cref="Validation{T}" /> into a <see cref="Result{TResult,TError}" />, mapping the
    ///     accumulated errors to a domain error via <paramref name="mapErrors" />.
    /// </summary>
    public static Result<T, TError> ToResult<T, TError>(this Validation<T> validation, Func<IReadOnlyList<ValidationError>, TError> mapErrors) => throw new NotImplementedException();
}
