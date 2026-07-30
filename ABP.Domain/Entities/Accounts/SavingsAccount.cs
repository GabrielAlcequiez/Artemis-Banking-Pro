using ABP.Domain.Common;
using ABP.Domain.Enums;
using ABP.Domain.Exceptions;

namespace ABP.Domain.Entities;


public class SavingsAccount : AuditableEntity<Guid>
{
    protected SavingsAccount()
    {
       
        OwnerUserId = string.Empty;
        AccountNumber = string.Empty;
    }

    private SavingsAccount(
        Guid id,
        string ownerUserId,
        string accountNumber,
        SavingsAccountType type,
        decimal initialBalance)
        : base(id)
    {
        OwnerUserId = ownerUserId;
        AccountNumber = accountNumber;
        Type = type;
        Status = SavingsAccountStatus.Active;
        Balance = initialBalance;
    }

    public string OwnerUserId { get; protected set; }

    public string AccountNumber { get; protected set; }

    public decimal Balance { get; protected set; }

    public SavingsAccountType Type { get; protected set; }

    public SavingsAccountStatus Status { get; protected set; }

    public byte[] RowVersion { get; protected set; } = [];

    public bool IsActive => Status == SavingsAccountStatus.Active;




    public static SavingsAccount Create(
        string ownerUserId,
        string accountNumber,
        SavingsAccountType type,
        decimal initialBalance)
    {
        if (string.IsNullOrWhiteSpace(ownerUserId))
        {
            throw new ArgumentException("Owner user id is required.", nameof(ownerUserId));
        }

        if (string.IsNullOrWhiteSpace(accountNumber))
        {
            throw new ArgumentException("Account number is required.", nameof(accountNumber));
        }

        if (initialBalance < 0)
        {
            throw new InvalidMonetaryAmountException(initialBalance);
        }

        return new SavingsAccount(Guid.NewGuid(), ownerUserId, accountNumber, type, initialBalance);
    }


    public void Credit(decimal amount)
    {
        EnsurePositiveAmount(amount);
        EnsureActive();

        Balance += amount;
    }

  



    public void Debit(decimal amount)
    {
        EnsurePositiveAmount(amount);
        EnsureActive();

        if (Balance < amount)
        {
            throw new InsufficientFundsException(Id, Balance, amount);
        }

        Balance -= amount;
    }





    public void Cancel()
    {
        if (Type == SavingsAccountType.Principal)
        {
            throw new InvalidOperationException("The Principal savings account cannot be cancelled.");
        }

        EnsureActive();

        if (Balance != 0m)
        {
            throw new InvalidOperationException(
                "The account balance must be transferred out before it can be cancelled.");
        }

        Status = SavingsAccountStatus.Cancelled;
    }

    public void EnsureActive()
    {
        if (!IsActive)
        {
            throw new InactiveAccountException(Id);
        }
    }

    private static void EnsurePositiveAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new InvalidMonetaryAmountException(amount);
        }
    }
}
