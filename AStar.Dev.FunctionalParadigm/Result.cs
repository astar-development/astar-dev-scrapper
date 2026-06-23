namespace AStar.Dev.FunctionalParadigm;

public abstract record Result<TSuccess, TFailure>
{
    public static Result<TSuccess, TFailure> Success(TSuccess value) => new Ok(value);
    public static Result<TSuccess, TFailure> Failure(TFailure value) => new Failed(value);

    public sealed record Ok(TSuccess Value) : Result<TSuccess, TFailure>;
    public sealed record Failed(TFailure Value) : Result<TSuccess, TFailure>;
}
