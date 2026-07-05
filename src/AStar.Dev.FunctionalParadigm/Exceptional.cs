namespace AStar.Dev.FunctionalParadigm;

/// <summary>
///     Factory methods for creating instances of <see cref="Exceptional{T}" />.
/// </summary>
public static class Exceptional
{
    /// <summary>
    ///     Creates a success <see cref="Exceptional{T}" /> wrapping the specified value.
    /// </summary>
    public static Exceptional<T> Success<T>(T value) => throw new NotImplementedException();

    /// <summary>
    ///     Creates a failure <see cref="Exceptional{T}" /> wrapping the specified exception.
    /// </summary>
    public static Exceptional<T> Failure<T>(Exception exception) => throw new NotImplementedException();
}
