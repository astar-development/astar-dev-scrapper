namespace AStar.Dev.Guard.Clauses;

/// <summary>
///     The root <seealso href="GuardAgainst"></seealso> class.
/// </summary>
public static class GuardAgainst
{
    /// <summary>
    ///     This method will check whether the specified object is null or not.
    /// </summary>
    /// <typeparam name="T">
    ///     Specifies the generic object to check for null.
    /// </typeparam>
    /// <param name="value">
    ///     The object to check for null.
    /// </param>
    /// <returns>
    ///     The original object if it is not null.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when the object is, in fact, null.
    /// </exception>
    public static T Null<T>(T value)
        => value is null ? throw new ArgumentNullException(nameof(value)) : value;
}
