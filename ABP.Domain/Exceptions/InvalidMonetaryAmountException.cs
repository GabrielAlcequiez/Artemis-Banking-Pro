
namespace ABP.Domain.Exceptions
{
    public sealed class InvalidMonetaryAmountException : DomainException
    {
        public InvalidMonetaryAmountException(decimal amount)
            : base("accounts.invalid_amount", $"The monetary amount '{amount}' is invalid.")
        {
            Amount = amount;
        }

        public decimal Amount { get; } = 0;
    }
}
