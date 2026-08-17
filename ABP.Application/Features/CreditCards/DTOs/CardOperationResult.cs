using ABP.Application.Common;

namespace ABP.Application.Features.CreditCards.DTOs;

public sealed record CardOperationResult(
    OperationResult Operation,
    bool HasNotificationWarning)
{
    public bool IsSuccess => Operation.IsSuccess;

    public bool IsFailure => Operation.IsFailure;

    public Error Error => Operation.Error;
}

public sealed record CardOperationResult<TValue>(
    OperationResult<TValue> Operation,
    bool HasNotificationWarning)
{
    public bool IsSuccess => Operation.IsSuccess;

    public bool IsFailure => Operation.IsFailure;

    public Error Error => Operation.Error;

    public TValue Value => Operation.Value;
}
