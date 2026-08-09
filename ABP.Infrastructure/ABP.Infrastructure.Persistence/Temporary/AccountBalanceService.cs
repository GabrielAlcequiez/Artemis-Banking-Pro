using ABP.Application.Common;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Domain.Entities.Accounts;
using ABP.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infrastructure.Persistence.Temporary
{
    // TEMPORAL, Implementación provisional hasta que P2 complete la suya.
    // Al entregar P2: eliminar esta carpeta y cambiar los registros DI en ServicesRegistration.cs.
    public sealed class AccountBalanceService : IAccountBalanceService
    {
        private readonly ISavingsAccountRepository _savingsAccountRepository;
        private readonly IUnitOfWork _unitOfWork;

        public AccountBalanceService(ISavingsAccountRepository savingsAccountRepository, IUnitOfWork unitOfWork)
        {
            _savingsAccountRepository = savingsAccountRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<OperationResult> CreditAsync(Guid accountId, decimal amount, CancellationToken cancellationToken = default)
        {
            if (amount <= 0)
            {
                return OperationResult.Failure(new Error("INVALID_AMOUNT", "El monto a acreditar debe ser mayor que cero."));
            }

            for (var attempt = 1; attempt <= 3; attempt++)
            {
                var account = await _savingsAccountRepository.GetByIdAsync(accountId, cancellationToken);
                if (account is null)
                {
                    return OperationResult.Failure(new Error("ACCOUNT_NOT_FOUND", "La cuenta especificada no existe."));
                }

                account.Balance += amount;
                await _savingsAccountRepository.UpdateAsync(account.Id, account, cancellationToken);

                try
                {
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    return OperationResult.Success();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (attempt == 3)
                    {
                        return OperationResult.Failure(new Error("CONCURRENCY", "Conflicto de concurrencia al intentar acreditar el saldo."));
                    }
                }
            }

            return OperationResult.Failure(new Error("UNEXPECTED", "No se pudo completar el crédito."));
        }

        public Task<OperationResult> DebitAsync(Guid accountId, decimal amount, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
