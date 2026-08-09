using ABP.Application.Common;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ABP.Application.Features.Accounts.Services
{
    
    public sealed class AccountBalanceService : IAccountBalanceService
    {
        private readonly ISavingsAccountRepository _accounts;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AccountBalanceService> _logger;

        public AccountBalanceService(
            ISavingsAccountRepository accounts,
            IUnitOfWork unitOfWork,
            ILogger<AccountBalanceService> logger)
        {
            _accounts = accounts;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<OperationResult> CreditAsync(
            Guid accountId, decimal amount, CancellationToken cancellationToken = default)
        {
            if (amount <= 0)
            {
                _logger.LogWarning(
                    "Intento de crédito con monto inválido {Amount} para la cuenta {AccountId}.", amount, accountId);
                return OperationResult.Failure(
                    new Error("accounts.invalid_amount", "The amount must be greater than zero."));
            }

            var account = await _accounts.GetByIdAsync(accountId, cancellationToken);
            if (account is null)
            {
                _logger.LogWarning("Crédito rechazado: la cuenta {AccountId} no existe.", accountId);
                return OperationResult.Failure(
                    new Error("accounts.not_found", $"Account '{accountId}' was not found."));
            }

            if (account.Status != SavingsAccountStatus.Active)
            {
                _logger.LogWarning("Crédito rechazado: la cuenta {AccountId} está cancelada.", accountId);
                return OperationResult.Failure(
                    new Error("accounts.inactive_account", $"Account '{accountId}' is cancelled."));
            }

            account.Balance += amount;

            await _accounts.UpdateAsync(account.Id, account, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Crédito de {Amount} aplicado a la cuenta {AccountId}. Nuevo saldo: {Balance}.",
                amount, accountId, account.Balance);

            return OperationResult.Success();
        }

        public async Task<OperationResult> DebitAsync(
            Guid accountId, decimal amount, CancellationToken cancellationToken = default)
        {
            if (amount <= 0)
            {
                _logger.LogWarning(
                    "Intento de débito con monto inválido {Amount} para la cuenta {AccountId}.", amount, accountId);
                return OperationResult.Failure(
                    new Error("accounts.invalid_amount", "The amount must be greater than zero."));
            }

            var account = await _accounts.GetByIdAsync(accountId, cancellationToken);
            if (account is null)
            {
                _logger.LogWarning("Débito rechazado: la cuenta {AccountId} no existe.", accountId);
                return OperationResult.Failure(
                    new Error("accounts.not_found", $"Account '{accountId}' was not found."));
            }

            if (account.Status != SavingsAccountStatus.Active)
            {
                _logger.LogWarning("Débito rechazado: la cuenta {AccountId} está cancelada.", accountId);
                return OperationResult.Failure(
                    new Error("accounts.inactive_account", $"Account '{accountId}' is cancelled."));
            }

            if (account.Balance < amount)
            {
                _logger.LogWarning(
                    "Débito rechazado por fondos insuficientes en la cuenta {AccountId}. Saldo: {Balance}, solicitado: {Amount}.",
                    accountId, account.Balance, amount);
                return OperationResult.Failure(
                    new Error("accounts.insufficient_funds",
                        $"Account '{accountId}' has insufficient funds. Available: {account.Balance}, requested: {amount}."));
            }

            account.Balance -= amount;

            await _accounts.UpdateAsync(account.Id, account, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Débito de {Amount} aplicado a la cuenta {AccountId}. Nuevo saldo: {Balance}.",
                amount, accountId, account.Balance);

            return OperationResult.Success();
        }
    }
}
