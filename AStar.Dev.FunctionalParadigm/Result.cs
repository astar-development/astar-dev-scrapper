namespace AStar.Dev.FunctionalParadigm;

public abstract record Result<TResult, TError>
{
    public static Result<TResult, TError> Success(TResult value) => new Ok<TResult, TError>(value);

    public static Result<TResult, TError> Failure(TError error) => new Fail<TResult, TError>(error);

    public static implicit operator Result<TResult, TError>(TResult value) => Success(value);

    public static implicit operator Result<TResult, TError>(TError error) => Failure(error);
}

public record Ok<TResult, TError>(TResult Value) : Result<TResult, TError>;

public record Fail<TResult, TError>(TError Error) : Result<TResult, TError>;
