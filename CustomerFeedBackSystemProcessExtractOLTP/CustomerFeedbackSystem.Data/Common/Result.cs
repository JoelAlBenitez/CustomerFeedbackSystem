namespace CustomerFeedbackSystem.Data.Common;

public class Result
{
    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public IReadOnlyList<Error> Errors { get; }

    protected Result(bool isSuccess, IReadOnlyList<Error> errors)
    {
        if (isSuccess && errors.Count > 0)
        {
            throw new InvalidOperationException("A successful result cannot carry errors.");
        }

        if (!isSuccess && errors.Count == 0)
        {
            throw new InvalidOperationException("A failed result must carry at least one error.");
        }

        IsSuccess = isSuccess;
        Errors = errors;
    }

    public static Result Success() => new(true, Array.Empty<Error>());

    public static Result Failure(Error error) => new(false, [error]);

    public static Result Failure(IReadOnlyList<Error> errors) => new(false, errors);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    private Result(T value) : base(true, Array.Empty<Error>())
    {
        _value = value;
    }

    private Result(IReadOnlyList<Error> errors) : base(false, errors)
    {
        _value = default;
    }

    public static Result<T> Success(T value) => new(value);

    public static new Result<T> Failure(Error error) => new([error]);

    public static new Result<T> Failure(IReadOnlyList<Error> errors) => new(errors);
}
