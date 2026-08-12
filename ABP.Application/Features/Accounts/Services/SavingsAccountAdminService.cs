using ABP.Application.Common;
using ABP.Application.Features.Accounts;
using ABP.Application.Features.Accounts.DTOs;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Domain.Entities.Accounts;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ABP.Application.Features.Accounts.Services
{
    /// <summary>Opens Secondary savings accounts and handles their cancellation.</summary>
    public sealed class SavingsAccountAdminService : ISavingsAccountAdminService
    {
        private readonly ISavingsAccountRepository _accounts;
        private readonly IFinancialIdentifierGenerator _identifierGenerator;
        private readonly IAccountLedger _ledger;
        private readonly IMoneyTransferService _moneyTransfer;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SavingsAccountAdminService> _logger;

        public SavingsAccountAdminService(
            ISavingsAccountRepository accounts,
            IFinancialIdentifierGenerator identifierGenerator,
            IAccountLedger ledger,
            IMoneyTransferService moneyTransfer,
            IUnitOfWork unitOfWork,
            ILogger<SavingsAccountAdminService> logger)
        {
            _accounts = accounts;
            _identifierGenerator = identifierGenerator;
            _ledger = ledger;
            _moneyTransfer = moneyTransfer;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<OperationResult<Guid>> CreateSecondaryAccountAsync(
            CreateSecondaryAccountRequest request, CancellationToken cancellationToken = default)
        {
            if (request.InitialBalance < 0)
            {
                return OperationResult<Guid>.Failure(AccountErrors.InvalidAmount);
            }

            var accountNumber = await _identifierGenerator.GenerateNineDigitIdentifierAsync(
                FinancialIdentifierType.SavingsAccount, cancellationToken);

            var account = new SavingsAccount(Guid.NewGuid())
            {
                OwnerUserId = request.OwnerUserId,
                AccountNumber = accountNumber,
                Type = SavingsAccountType.Secondary,
                Status = SavingsAccountStatus.Active,
                Balance = request.InitialBalance
            };
            // CreatedAtUtc/CreatedByUserId are filled automatically by AuditableEntityInterceptor on save.

            await _accounts.AddAsync(account, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (request.InitialBalance > 0)
            {
                await _ledger.RecordApprovedAsync(
                    Guid.NewGuid(), account.Id, request.InitialBalance, TransactionDirection.Credit,
                    FinancialOperationType.InitialBalance, origin: "APERTURA", beneficiary: account.AccountNumber,
                    request.ActorUserId, request.ActorRole, cancellationToken);
            }

            _logger.LogInformation(
                "Cuenta secundaria {AccountNumber} abierta para {OwnerUserId} con saldo inicial {InitialBalance}.",
                account.AccountNumber, request.OwnerUserId, request.InitialBalance);

            return OperationResult<Guid>.Success(account.Id);
        }

        public async Task<OperationResult> CancelAsync(
            CancelSavingsAccountRequest request, CancellationToken cancellationToken = default)
        {
            var account = await _accounts.GetByIdAsync(request.AccountId, cancellationToken);
            if (account is null)
            {
                return OperationResult.Failure(AccountErrors.NotFound);
            }

            if (account.Type == SavingsAccountType.Principal)
            {
                _logger.LogWarning(
                    "Cancelación rechazada: la cuenta {AccountId} es principal y no se puede cancelar.", account.Id);
                return OperationResult.Failure(AccountErrors.CannotCancelPrincipal);
            }

            if (account.Status != SavingsAccountStatus.Active)
            {
                return OperationResult.Failure(AccountErrors.AlreadyCancelled);
            }

            if (account.Balance > 0)
            {
                var principal = await _accounts.GetPrincipalAccountAsync(account.OwnerUserId, cancellationToken);
                if (principal is null)
                {
                    _logger.LogError(
                        "Cancelación bloqueada: la cuenta {AccountId} tiene saldo pero {OwnerUserId} no tiene cuenta principal.",
                        account.Id, account.OwnerUserId);
                    return OperationResult.Failure(AccountErrors.PrincipalNotFound);
                }

                var transferRequest = new TransferFundsRequest
                {
                    SourceAccountId = account.Id,
                    DestinationAccountId = principal.Id,
                    Amount = account.Balance,
                    OperationType = FinancialOperationType.AccountCancellationTransfer,
                    ActorUserId = request.ActorUserId,
                    ActorRole = request.ActorRole
                };

                var transferResult = await _moneyTransfer.TransferAsync(transferRequest, cancellationToken);
                if (transferResult.IsFailure)
                {
                    return OperationResult.Failure(transferResult.Error);
                }

                // Re-fetch: the transfer already persisted the zeroed-out balance.
                account = await _accounts.GetByIdAsync(request.AccountId, cancellationToken);
                if (account is null)
                {
                    return OperationResult.Failure(AccountErrors.NotFound);
                }
            }

            account.Status = SavingsAccountStatus.Cancelled;
            await _accounts.UpdateAsync(account.Id, account, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Cuenta {AccountNumber} cancelada por {ActorUserId}.", account.AccountNumber, request.ActorUserId);

            return OperationResult.Success();
        }
    }
}
