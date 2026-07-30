namespace ABP.Domain.Exceptions;

public sealed class InsufficientFundsException : DomainException
{
    public InsufficientFundsException(Guid accountId, decimal availableBalance, decimal requestedAmount)
        : base("accounts.insufficient_funds",$"Account '{accountId}' has insufficient funds. Available: {availableBalance}, requested: {requestedAmount}.")
    {
        AccountId = accountId;
        AvailableBalance = availableBalance;
        RequestedAmount = requestedAmount;
    }

    public Guid AccountId { get; }

    public decimal AvailableBalance { get; }

    public decimal RequestedAmount { get; }
}