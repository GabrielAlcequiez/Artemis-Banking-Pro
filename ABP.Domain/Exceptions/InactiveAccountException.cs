namespace ABP.Domain.Exceptions;

public sealed class InactiveAccountException : DomainException
{
    public InactiveAccountException(Guid accountId)
        : base( "accounts.inactive_account", $"Account '{accountId}' is cancelled and cannot participate in new operations.")
    {
        AccountId = accountId;
    }

    public Guid AccountId { get; }
}
