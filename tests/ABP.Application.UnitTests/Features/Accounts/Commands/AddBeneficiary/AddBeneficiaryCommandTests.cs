using ABP.Application.Common;
using ABP.Application.Features.Accounts.Commands.AddBeneficiary;
using ABP.Application.Features.Accounts.DTOs;
using ABP.Application.Features.Accounts.Validation;
using ABP.TestDoubles;

namespace ABP.Application.UnitTests.Features.Accounts.Commands.AddBeneficiary
{
    public sealed class AddBeneficiaryCommandTests
    {
        private static AddBeneficiaryRequest ValidRequest() => new()
        {
            OwnerUserId = "user-1",
            BeneficiaryAccountNumber = "100000002"
        };

        [Fact]
        public async Task Validator_reuses_shared_request_rules()
        {
            var validator = new AddBeneficiaryCommandValidator(new AddBeneficiaryRequestValidator());
            var invalidRequest = ValidRequest();
            invalidRequest.BeneficiaryAccountNumber = "123";
            var command = new AddBeneficiaryCommand(invalidRequest);

            var result = await validator.ValidateAsync(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error => error.PropertyName == "Request.BeneficiaryAccountNumber");
        }

        [Fact]
        public async Task Handler_delegates_to_the_beneficiary_service()
        {
            var expected = OperationResult<BeneficiaryDto>.Success(new BeneficiaryDto
            {
                Id = Guid.NewGuid(),
                BeneficiaryAccountId = Guid.NewGuid(),
                BeneficiaryAccountNumber = "100000002",
                BeneficiaryOwnerName = "Someone",
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
            var beneficiaries = new FakeBeneficiaryService { AddResult = expected };
            var handler = new AddBeneficiaryCommandHandler(beneficiaries);

            var result = await handler.Handle(new AddBeneficiaryCommand(ValidRequest()), CancellationToken.None);

            Assert.Same(expected, result);
        }
    }
}
