using ABP.Application.Common;
using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Domain.Entities.Accounts;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ABP.Application.Features.Accounts.Services
{
    /// <summary>Opens the single Principal SavingsAccount a new Client/Commerce user must have.</summary>
    public sealed class PrimaryAccountProvisioner : IPrimaryAccountProvisioner
    {
        private readonly ISavingsAccountRepository _accounts;
        private readonly IFinancialIdentifierGenerator _identifierGenerator;
        private readonly IAccountLedger _ledger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _clock;
        private readonly ILogger<PrimaryAccountProvisioner> _logger;

        public PrimaryAccountProvisioner(
            ISavingsAccountRepository accounts,
            IFinancialIdentifierGenerator identifierGenerator,
            IAccountLedger ledger,
            IUnitOfWork unitOfWork,
            IClock clock,
            ILogger<PrimaryAccountProvisioner> logger)
        {
            _accounts = accounts;
            _identifierGenerator = identifierGenerator;
            _ledger = ledger;
            _unitOfWork = unitOfWork;
            _clock = clock;
            _logger = logger;
        }

        public async Task<OperationResult<FinancialOperationReceipt>> OpenPrincipalAccountAsync(
            string ownerUserId, decimal initialBalance, string actorUserId, string actorRole,
            CancellationToken cancellationToken = default)
        {
            if (initialBalance < 0)
            {
                return OperationResult<FinancialOperationReceipt>.Failure(
                    new Error("accounts.invalid_amount", "The initial balance cannot be negative."));
            }

            var existing = await _accounts.GetPrincipalAccountAsync(ownerUserId, cancellationToken);
            if (existing is not null)
            {
                _logger.LogWarning(
                    "Apertura de cuenta principal rechazada: el usuario {OwnerUserId} ya tiene una.", ownerUserId);
                return OperationResult<FinancialOperationReceipt>.Failure(
                    new Error("accounts.principal_already_exists", $"User '{ownerUserId}' already has a Principal account."));
            }

            var accountNumber = await _identifierGenerator.GenerateNineDigitIdentifierAsync(
                FinancialIdentifierType.SavingsAccount, cancellationToken);

            var account = new SavingsAccount(Guid.NewGuid())
            {
                OwnerUserId = ownerUserId,
                AccountNumber = accountNumber,
                Type = SavingsAccountType.Principal,
                Status = SavingsAccountStatus.Active,
                Balance = initialBalance
            };
            // No hay interceptor global de auditoría: hay que setear CreatedAtUtc a mano.
            account.ApplyAudit(_clock.UtcNow, actorUserId);

            await _accounts.AddAsync(account, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var operationId = Guid.NewGuid();

            if (initialBalance > 0)
            {
                await _ledger.RecordApprovedAsync(
                    operationId, account.Id, initialBalance, TransactionDirection.Credit,
                    FinancialOperationType.InitialBalance, origin: "APERTURA", beneficiary: account.AccountNumber,
                    actorUserId, actorRole, cancellationToken);
            }

            _logger.LogInformation(
                "Cuenta principal {AccountNumber} abierta para {OwnerUserId} con saldo inicial {InitialBalance}.",
                account.AccountNumber, ownerUserId, initialBalance);

            var receipt = new FinancialOperationReceipt(operationId, initialBalance, DateTimeOffset.UtcNow);
            return OperationResult<FinancialOperationReceipt>.Success(receipt);
        }
    }
}
