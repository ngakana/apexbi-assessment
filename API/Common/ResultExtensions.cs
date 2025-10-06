namespace API.Common;

public static class ResultExtensions
{
    public static Result<TOut> Bind<TIn, TOut>(this Result<TIn> result, 
        Func<TIn, Result<TOut>> bind)
    {
        if (result.IsFailure) return Result<TOut>.Failure(result.Error!);

        try
        {
            return bind(result.Value!);
        }
        catch (Exception ex)
        {
            return Result<TOut>.Failure(new Error(ErrorType.Exception, ex.Message));
        }
    }

    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(this Result<TIn> result, 
        Func<TIn, Task<Result<TOut>>> bind)
    {
        if (result.IsFailure) return Result<TOut>.Failure(result.Error!);

        try
        {
            return await bind(result.Value!);
        }
        catch (Exception ex)
        {
            return Result<TOut>.Failure(new Error(ErrorType.Exception, ex.Message));
        }
    }

    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(this Task<Result<TIn>> resultTask, 
        Func<TIn, Task<Result<TOut>>> bind)
    {
        var result = await resultTask;
        if (result.IsFailure) return Result<TOut>.Failure(result.Error!);

        try
        {
            return await bind(result.Value!);
        }
        catch (Exception ex)
        {
            return Result<TOut>.Failure(new Error(ErrorType.Exception, ex.Message));
        }
    }

    public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result,  
        Func<TIn, TOut> func)
    {
        if (result.IsFailure) return Result<TOut>.Failure(result.Error!);
        try
        {
            return Result<TOut>.Success(func(result.Value!));
        }
        catch
        {
            return Result<TOut>.Failure(new Error(ErrorType.Exception, $"Failed to map data from {nameof(TIn)} to {nameof(TOut)}."));
        }
    }

    public static Result<T> Tap<T>(this Result<T> result, Action<T> action)
    {
        if (result.IsSuccess && result.Value is not null)
        {
            action(result.Value);
        }
        return result;
    }

    public static async Task<Result<T>> TapAsync<T>(this Result<T> result,
        Func<T, Task> action)
    {
        if (result.IsSuccess) await action(result.Value!);

        return result;
    }

    public static async Task<Result<T>> TapAsync<T>(this Task<Result<T>> resultTask,
        Func<T, Task> action)
    {
        var result = await resultTask;

        if (result.IsSuccess) await action(result.Value!);

        return result;
    }

    public static Result<U> OnSuccess<T, U>(this Result<T> result, Func<T, Result<U>> func)
    {
        if (result.IsFailure) return Result<U>.Failure(result.Error!);
        return func(result.Value!);
    }

    public static Result<T> OnFailure<T>(this Result<T> result, Action<Error> action)
    {
        if (result.IsFailure) action(result.Error!);
        return result;
    }

    public static TOut Match<TIn, TOut>(this Result<TIn> result,
        Func<TIn, TOut> onSuccess,
        Func<Error, TOut> onFailure)
    {
        return result.IsSuccess
            ? onSuccess(result.Value!)
            : onFailure(result.Error!);
    }

    public static async Task<TOut> MatchAsync<TIn, TOut>(this Task<Result<TIn>> resultTask,
        Func<TIn, Task<TOut>> onSuccess,
        Func<Error, Task<TOut>> onFailure)
    {
        var result = await resultTask;
        return result.IsSuccess
            ? await onSuccess(result.Value!)
            : await onFailure(result.Error!);
    }
}
