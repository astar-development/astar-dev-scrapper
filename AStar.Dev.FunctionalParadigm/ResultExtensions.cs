namespace AStar.Dev.FunctionalParadigm;

public static class ResultExtensions
{
    public static Result<TResult, TError> Tap<TResult, TError>(this Result<TResult, TError> result, Action<TResult> onSuccess, Action<TError>? onFailure = null)
    {
        switch (result)
        {
            case Ok<TResult, TError> ok:
                onSuccess(ok.Value);
                return ok;

            case Fail<TResult, TError> fail:
                onFailure?.Invoke(fail.Error);
                return fail;

            default:
                throw new InvalidOperationException("Unexpected result type.");
        }
    }

    public static Result<TMapped, TError> Map<TResult, TMapped, TError>(this Result<TResult, TError> result, Func<TResult, TMapped> selector)
    {
        return result switch
        {
            Ok<TResult, TError> ok => new Ok<TMapped, TError>(selector(ok.Value)),
            Fail<TResult, TError> fail => new Fail<TMapped, TError>(fail.Error),
            _ => throw new InvalidOperationException("Unexpected result type.")
        };
    }

    public static Result<TMapped, TError> Bind<TResult, TMapped, TError>(this Result<TResult, TError> result, Func<TResult, Result<TMapped, TError>> binder)
    {
        return result switch
        {
            Ok<TResult, TError> ok => binder(ok.Value),
            Fail<TResult, TError> fail => new Fail<TMapped, TError>(fail.Error),
            _ => throw new InvalidOperationException("Unexpected result type.")
        };
    }

    public static TOut Match<TResult, TError, TOut>(this Result<TResult, TError> result, Func<TResult, TOut> onSuccess, Func<TError, TOut> onFailure)
    {
        return result switch
        {
            Ok<TResult, TError> ok => onSuccess(ok.Value),
            Fail<TResult, TError> fail => onFailure(fail.Error),
            _ => throw new InvalidOperationException("Unexpected result type.")
        };
    }
}
