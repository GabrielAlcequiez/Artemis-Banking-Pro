namespace ABP.Application.Common;

public class OperationResult
{
    protected OperationResult(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("A successful result cannot contain an error.");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("A failed result must contain an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static OperationResult Success()
    {
        return new OperationResult(true, Error.None);
    }

    public static OperationResult Failure(Error error)
    {
        return new OperationResult(false, error);
    }
}

public sealed class OperationResult<TValue> : OperationResult
{
    private readonly TValue? value;

    private OperationResult(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        this.value = value;
    }

    public TValue Value => IsSuccess
        ? value!
        : throw new InvalidOperationException("A failed result does not contain a value.");

    public static OperationResult<TValue> Success(TValue value)
    {
        return new OperationResult<TValue>(value, true, Error.None);
    }

    public new static OperationResult<TValue> Failure(Error error)
    {
        return new OperationResult<TValue>(default, false, error);
    }
}
