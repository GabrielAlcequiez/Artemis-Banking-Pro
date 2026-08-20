using ABP.Application.Common;

namespace ABP.Application.Features.Loans.DTOs;

public sealed record LoanOperationResult(
    OperationResult Operation,
    bool HasNotificationWarning)
{
    public bool IsSuccess => Operation.IsSuccess;

    public bool IsFailure => Operation.IsFailure;

    public Error Error => Operation.Error;
}

public sealed record LoanOperationResult<TValue>(
    OperationResult<TValue> Operation,
    bool HasNotificationWarning)
{
    public bool IsSuccess => Operation.IsSuccess;

    public bool IsFailure => Operation.IsFailure;

    public Error Error => Operation.Error;

    public TValue Value => Operation.Value;
}
