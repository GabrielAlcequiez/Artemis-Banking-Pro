using ABP.Application.Common;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Domain.Entities.Accounts;
using ABP.Domain.Enums;
using ABP.Infrastructure.Persistence.Context;

namespace ABP.Infrastructure.Persistence.Temporary
{
    // TEMPORAL - Implementación provisional para que P1/P3/P4 puedan ejecutar antes de que P2 entregue
    // la suya. Al entregar P2: eliminar esta carpeta y cambiar el registro DI en ServicesRegistration.cs.
    public sealed class PrimaryAccountProvisioner : IPrimaryAccountProvisioner
    {
        private readonly AppDbContext _context;
        private readonly IFinancialIdentifierGenerator _identifierGenerator;

        public PrimaryAccountProvisioner(AppDbContext context, IFinancialIdentifierGenerator identifierGenerator)
        {
            _context = context;
            _identifierGenerator = identifierGenerator;
        }

        public async Task<OperationResult<FinancialOperationReceipt>> OpenPrincipalAccountAsync(
            string ownerUserId,
            decimal initialBalance,
            string actorUserId,
            string actorRole,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(ownerUserId))
            {
                return OperationResult<FinancialOperationReceipt>.Failure(
                    new Error("INVALID_OWNER", "El propietario de la cuenta es obligatorio."));
            }

            if (initialBalance < 0)
            {
                return OperationResult<FinancialOperationReceipt>.Failure(
                    new Error("NEGATIVE_INITIAL_BALANCE", "El monto inicial no puede ser negativo."));
            }

            var accountNumber = await _identifierGenerator
                .GenerateNineDigitIdentifierAsync(FinancialIdentifierType.SavingsAccount, cancellationToken);

            var account = new SavingsAccount(Guid.NewGuid())
            {
                OwnerUserId = ownerUserId,
                AccountNumber = accountNumber,
                Balance = initialBalance,
                Type = SavingsAccountType.Principal,
                Status = SavingsAccountStatus.Active
            };

            var operationId = Guid.NewGuid();

            if (initialBalance > 0)
            {
                _context.AccountTransactions.Add(new AccountTransaction(Guid.NewGuid())
                {
                    AccountId = account.Id,
                    OperationId = operationId,
                    Amount = initialBalance,
                    Direction = TransactionDirection.Credit,
                    OperationType = FinancialOperationType.InitialBalance,
                    Origin = "Apertura de cuenta de ahorro principal",
                    Status = TransactionStatus.Approved,
                    ActorUserId = actorUserId,
                    ActorRole = actorRole
                });
            }

            _context.SavingsAccounts.Add(account);
            await _context.SaveChangesAsync(cancellationToken);

            return OperationResult<FinancialOperationReceipt>.Success(
                new FinancialOperationReceipt(operationId, initialBalance, DateTimeOffset.UtcNow));
        }
    }
}
