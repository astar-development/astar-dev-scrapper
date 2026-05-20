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
    /// <param name="object">
    ///     The object to check for null.
    /// </param>
    /// <returns>
    ///     The original object if it is not null.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when the object is, in fact, null.
    /// </exception>
    public static T Null<T>(T @object)
        => @object is null ? throw new ArgumentNullException(nameof(@object)) : @object;
}
