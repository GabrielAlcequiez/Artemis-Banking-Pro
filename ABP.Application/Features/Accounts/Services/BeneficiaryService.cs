using ABP.Application.Common;
using ABP.Application.Features.Accounts.DTOs;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Domain.Entities;
using ABP.Domain.Entities.Accounts;
using ABP.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ABP.Application.Features.Accounts.Services
{
    public sealed class BeneficiaryService : IBeneficiaryService
    {
        private readonly IBeneficiaryRepository _beneficiaries;
        private readonly ISavingsAccountRepository _accounts;
        private readonly IGenericRepository<User, string> _users;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BeneficiaryService> _logger;

        public BeneficiaryService(
            IBeneficiaryRepository beneficiaries,
            ISavingsAccountRepository accounts,
            IGenericRepository<User, string> users,
            IUnitOfWork unitOfWork,
            ILogger<BeneficiaryService> logger)
        {
            _beneficiaries = beneficiaries;
            _accounts = accounts;
            _users = users;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IReadOnlyCollection<BeneficiaryDto>> ListAsync(
            string ownerUserId, CancellationToken cancellationToken = default)
        {
            var owned = await _beneficiaries.GetByOwnerAsync(ownerUserId, cancellationToken);
            var result = new List<BeneficiaryDto>(owned.Count);

            foreach (var beneficiary in owned)
            {
                var account = await _accounts.GetByIdAsync(beneficiary.BeneficiaryAccountId, cancellationToken);
                if (account is null)
                {
                    continue;
                }

                var ownerName = await ResolveOwnerNameAsync(account.OwnerUserId, cancellationToken);

                result.Add(new BeneficiaryDto
                {
                    Id = beneficiary.Id,
                    BeneficiaryAccountId = account.Id,
                    BeneficiaryAccountNumber = account.AccountNumber,
                    BeneficiaryOwnerName = ownerName,
                    CreatedAtUtc = beneficiary.CreatedAtUtc
                });
            }

            return result;
        }

        public async Task<OperationResult<BeneficiaryDto>> AddAsync(
            AddBeneficiaryRequest request, CancellationToken cancellationToken = default)
        {
            var account = await _accounts.GetByAccountNumberAsync(request.BeneficiaryAccountNumber, cancellationToken);
            if (account is null)
            {
                return OperationResult<BeneficiaryDto>.Failure(
                    new Error("accounts.not_found", "The beneficiary account was not found."));
            }

            if (account.OwnerUserId == request.OwnerUserId)
            {
                return OperationResult<BeneficiaryDto>.Failure(
                    new Error("accounts.cannot_add_self", "You cannot add your own account as a beneficiary."));
            }

            var alreadyExists = await _beneficiaries.ExistsAsync(request.OwnerUserId, account.Id, cancellationToken);
            if (alreadyExists)
            {
                return OperationResult<BeneficiaryDto>.Failure(
                    new Error("accounts.beneficiary_already_exists", "This account is already registered as a beneficiary."));
            }

            var beneficiary = new Beneficiary(Guid.NewGuid())
            {
                OwnerUserId = request.OwnerUserId,
                BeneficiaryAccountId = account.Id
            };

            await _beneficiaries.AddAsync(beneficiary, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Beneficiario {BeneficiaryAccountNumber} agregado por {OwnerUserId}.",
                account.AccountNumber, request.OwnerUserId);

            var ownerName = await ResolveOwnerNameAsync(account.OwnerUserId, cancellationToken);

            var dto = new BeneficiaryDto
            {
                Id = beneficiary.Id,
                BeneficiaryAccountId = account.Id,
                BeneficiaryAccountNumber = account.AccountNumber,
                BeneficiaryOwnerName = ownerName,
                CreatedAtUtc = beneficiary.CreatedAtUtc
            };

            return OperationResult<BeneficiaryDto>.Success(dto);
        }

        public async Task<OperationResult> RemoveAsync(
            string ownerUserId, Guid beneficiaryId, CancellationToken cancellationToken = default)
        {
            var owned = await _beneficiaries.GetByOwnerAsync(ownerUserId, cancellationToken);
            var beneficiary = owned.FirstOrDefault(b => b.Id == beneficiaryId);

            if (beneficiary is null)
            {
                _logger.LogWarning(
                    "Eliminación de beneficiario rechazada: {BeneficiaryId} no pertenece a {OwnerUserId}.",
                    beneficiaryId, ownerUserId);
                return OperationResult.Failure(
                    new Error("accounts.beneficiary_not_found", "The beneficiary was not found."));
            }

            await _beneficiaries.DeleteAsync(beneficiary.Id, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Beneficiario {BeneficiaryId} eliminado por {OwnerUserId}.", beneficiaryId, ownerUserId);

            return OperationResult.Success();
        }

        private async Task<string> ResolveOwnerNameAsync(string ownerUserId, CancellationToken cancellationToken)
        {
            var owner = await _users.GetByIdAsync(ownerUserId, cancellationToken);
            return owner is null ? ownerUserId : $"{owner.Name} {owner.LastName}".Trim();
        }
    }
}
