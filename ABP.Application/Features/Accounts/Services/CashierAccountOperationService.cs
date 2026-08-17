using ABP.Application.Common;
using ABP.Application.Features.Accounts;
using ABP.Application.Features.Accounts.DTOs;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Domain.Entities;
using ABP.Domain.Entities.Accounts;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;

namespace ABP.Application.Features.Accounts.Services
{
    /// <summary>Looks up accounts by number and previews Deposit/Withdrawal/ThirdPartyTransfer for the Cashier confirm step.</summary>
    public sealed class CashierAccountOperationService(
        ISavingsAccountRepository accounts,
        IGenericRepository<User, string> users) : ICashierAccountOperationService
    {
        private const int AccountNumberLength = 9;

        public async Task<OperationResult<CashierDepositPreview>> PrepareDepositAsync(
            string accountNumber, decimal amount, CancellationToken cancellationToken = default)
        {
            if (amount <= 0)
            {
                return OperationResult<CashierDepositPreview>.Failure(AccountErrors.InvalidAmount);
            }

            var account = await FindActiveAccountAsync(accountNumber, cancellationToken);

            if (account.IsFailure)
            {
                return OperationResult<CashierDepositPreview>.Failure(account.Error);
            }

            var ownerName = await ResolveOwnerNameAsync(account.Value.OwnerUserId, cancellationToken);

            return OperationResult<CashierDepositPreview>.Success(
                new CashierDepositPreview(account.Value.Id, account.Value.AccountNumber, ownerName, amount));
        }

        public async Task<OperationResult<CashierWithdrawalPreview>> PrepareWithdrawalAsync(
            string accountNumber, decimal amount, CancellationToken cancellationToken = default)
        {
            if (amount <= 0)
            {
                return OperationResult<CashierWithdrawalPreview>.Failure(AccountErrors.InvalidAmount);
            }

            var account = await FindActiveAccountAsync(accountNumber, cancellationToken);

            if (account.IsFailure)
            {
                return OperationResult<CashierWithdrawalPreview>.Failure(account.Error);
            }

            if (account.Value.Balance < amount)
            {
                return OperationResult<CashierWithdrawalPreview>.Failure(AccountErrors.InsufficientFunds);
            }

            var ownerName = await ResolveOwnerNameAsync(account.Value.OwnerUserId, cancellationToken);

            return OperationResult<CashierWithdrawalPreview>.Success(
                new CashierWithdrawalPreview(
                    account.Value.Id, account.Value.AccountNumber, ownerName, account.Value.Balance, amount));
        }

        public async Task<OperationResult<CashierThirdPartyTransferPreview>> PrepareThirdPartyTransferAsync(
            string sourceAccountNumber, string destinationAccountNumber, decimal amount,
            CancellationToken cancellationToken = default)
        {
            if (amount <= 0)
            {
                return OperationResult<CashierThirdPartyTransferPreview>.Failure(AccountErrors.InvalidAmount);
            }

            var normalizedSource = Normalize(sourceAccountNumber);
            var normalizedDestination = Normalize(destinationAccountNumber);

            if (IsNineDigitNumber(normalizedSource) &&
                IsNineDigitNumber(normalizedDestination) &&
                normalizedSource == normalizedDestination)
            {
                return OperationResult<CashierThirdPartyTransferPreview>.Failure(AccountErrors.SameAccount);
            }

            var source = await FindActiveAccountAsync(normalizedSource, cancellationToken);

            if (source.IsFailure)
            {
                return OperationResult<CashierThirdPartyTransferPreview>.Failure(source.Error);
            }

            if (source.Value.Balance < amount)
            {
                return OperationResult<CashierThirdPartyTransferPreview>.Failure(AccountErrors.InsufficientFunds);
            }

            var destination = await FindActiveAccountAsync(normalizedDestination, cancellationToken);

            if (destination.IsFailure)
            {
                return OperationResult<CashierThirdPartyTransferPreview>.Failure(destination.Error);
            }

            var sourceOwnerName = await ResolveOwnerNameAsync(source.Value.OwnerUserId, cancellationToken);
            var destinationOwnerName = await ResolveOwnerNameAsync(destination.Value.OwnerUserId, cancellationToken);

            return OperationResult<CashierThirdPartyTransferPreview>.Success(
                new CashierThirdPartyTransferPreview(
                    source.Value.Id, source.Value.AccountNumber, sourceOwnerName, source.Value.Balance,
                    destination.Value.Id, destination.Value.AccountNumber, destinationOwnerName,
                    amount));
        }

        private async Task<OperationResult<SavingsAccount>> FindActiveAccountAsync(
            string accountNumber, CancellationToken cancellationToken)
        {
            var normalized = Normalize(accountNumber);

            if (!IsNineDigitNumber(normalized))
            {
                return OperationResult<SavingsAccount>.Failure(AccountErrors.NotFound);
            }

            var account = await accounts.GetByAccountNumberAsync(normalized, cancellationToken);

            if (account is null)
            {
                return OperationResult<SavingsAccount>.Failure(AccountErrors.NotFound);
            }

            return account.Status != SavingsAccountStatus.Active
                ? OperationResult<SavingsAccount>.Failure(AccountErrors.InactiveAccount)
                : OperationResult<SavingsAccount>.Success(account);
        }

        private async Task<string> ResolveOwnerNameAsync(string ownerUserId, CancellationToken cancellationToken)
        {
            var owner = await users.GetByIdAsync(ownerUserId, cancellationToken);
            return owner is null ? ownerUserId : $"{owner.Name} {owner.LastName}".Trim();
        }

        private static string Normalize(string? accountNumber) =>
            accountNumber?.Trim() ?? string.Empty;

        private static bool IsNineDigitNumber(string value) =>
            value.Length == AccountNumberLength && value.All(char.IsDigit);
    }
}
