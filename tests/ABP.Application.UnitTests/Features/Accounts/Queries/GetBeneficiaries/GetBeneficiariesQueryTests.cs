using ABP.Application.Features.Accounts.DTOs;
using ABP.Application.Features.Accounts.Queries.GetBeneficiaries;
using ABP.TestDoubles;

namespace ABP.Application.UnitTests.Features.Accounts.Queries.GetBeneficiaries
{
    public sealed class GetBeneficiariesQueryTests
    {
        [Fact]
        public void Owner_user_id_is_required()
        {
            var validator = new GetBeneficiariesQueryValidator();
            var query = new GetBeneficiariesQuery(string.Empty);

            var result = validator.Validate(query);

            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(GetBeneficiariesQuery.OwnerUserId));
        }

        [Fact]
        public async Task Handler_returns_the_beneficiaries_from_the_service()
        {
            var beneficiaries = new FakeBeneficiaryService();
            beneficiaries.SeedBeneficiaries("user-1", new BeneficiaryDto
            {
                Id = Guid.NewGuid(),
                BeneficiaryAccountId = Guid.NewGuid(),
                BeneficiaryAccountNumber = "100000002",
                BeneficiaryOwnerName = "Someone",
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
            var handler = new GetBeneficiariesQueryHandler(beneficiaries);

            var result = await handler.Handle(new GetBeneficiariesQuery("user-1"), CancellationToken.None);

            Assert.Single(result);
        }
    }
}
