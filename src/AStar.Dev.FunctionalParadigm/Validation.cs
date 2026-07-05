namespace AStar.Dev.FunctionalParadigm;

/// <summary>
///     Factory methods for creating instances of <see cref="Validation{T}" />.
/// </summary>
public static class Validation
{
    /// <summary>
    ///     Creates a valid <see cref="Validation{T}" /> wrapping the specified value.
    /// </summary>
    public static Validation<T> Valid<T>(T value) => throw new NotImplementedException();

    /// <summary>
    ///     Creates an invalid <see cref="Validation{T}" /> wrapping the specified errors.
    /// </summary>
    public static Validation<T> Invalid<T>(IReadOnlyList<ValidationError> errors) => throw new NotImplementedException();

    /// <summary>
    ///     Creates an invalid <see cref="Validation{T}" /> wrapping a single error.
    /// </summary>
    public static Validation<T> Invalid<T>(ValidationError error) => throw new NotImplementedException();
}
