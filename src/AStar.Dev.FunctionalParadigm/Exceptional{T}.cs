namespace AStar.Dev.FunctionalParadigm;

/// <summary>
///     Represents the outcome of an operation that may either succeed with a
///     <typeparamref name="T" /> value or fail with a captured <see cref="Exception" />.
///     Use the <see cref="Exceptional" /> factory class to construct instances.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public abstract record Exceptional<T>;

/// <summary>
///     Represents a successful <see cref="Exceptional{T}" /> carrying a value.
/// </summary>
public sealed record Success<T>(T Value) : Exceptional<T>;

/// <summary>
///     Represents a failed <see cref="Exceptional{T}" /> carrying the captured exception.
/// </summary>
public sealed record Failure<T>(Exception Exception) : Exceptional<T>;
