using ABP.Application.Common;
using ABP.Application.Features.Accounts.DTOs;
using ABP.Application.Features.Accounts.Services.Interfaces;

namespace ABP.TestDoubles
{
    public class FakeBeneficiaryService : IBeneficiaryService
    {
        public OperationResult<BeneficiaryDto>? AddResult { get; set; }

        public OperationResult? RemoveResult { get; set; }

        private readonly Dictionary<string, List<BeneficiaryDto>> _beneficiariesByOwner = new();

        public void SeedBeneficiaries(string ownerUserId, params BeneficiaryDto[] beneficiaries)
        {
            _beneficiariesByOwner[ownerUserId] = beneficiaries.ToList();
        }

        public Task<IReadOnlyCollection<BeneficiaryDto>> ListAsync(
            string ownerUserId, CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<BeneficiaryDto> result = _beneficiariesByOwner.TryGetValue(ownerUserId, out var list)
                ? list
                : Array.Empty<BeneficiaryDto>();

            return Task.FromResult(result);
        }

        public Task<OperationResult<BeneficiaryDto>> AddAsync(
            AddBeneficiaryRequest request, CancellationToken cancellationToken = default)
        {
            var result = AddResult ?? OperationResult<BeneficiaryDto>.Success(new BeneficiaryDto
            {
                Id = Guid.NewGuid(),
                BeneficiaryAccountId = Guid.NewGuid(),
                BeneficiaryAccountNumber = request.BeneficiaryAccountNumber,
                BeneficiaryOwnerName = "Fake Owner",
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            return Task.FromResult(result);
        }

        public Task<OperationResult> RemoveAsync(
            string ownerUserId, Guid beneficiaryId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(RemoveResult ?? OperationResult.Success());
        }
    }
}
