using ABP.Application.Common;
using ABP.Application.Features.Accounts.Commands.RemoveBeneficiary;
using ABP.TestDoubles;

namespace ABP.Application.UnitTests.Features.Accounts.Commands.RemoveBeneficiary
{
    public sealed class RemoveBeneficiaryCommandTests
    {
        [Fact]
        public void Owner_user_id_is_required()
        {
            var validator = new RemoveBeneficiaryCommandValidator();
            var command = new RemoveBeneficiaryCommand(string.Empty, Guid.NewGuid());

            var result = validator.Validate(command);

            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(RemoveBeneficiaryCommand.OwnerUserId));
        }

        [Fact]
        public void Beneficiary_id_is_required()
        {
            var validator = new RemoveBeneficiaryCommandValidator();
            var command = new RemoveBeneficiaryCommand("user-1", Guid.Empty);

            var result = validator.Validate(command);

            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(RemoveBeneficiaryCommand.BeneficiaryId));
        }

        [Fact]
        public async Task Handler_delegates_to_the_beneficiary_service()
        {
            var expected = OperationResult.Success();
            var beneficiaries = new FakeBeneficiaryService { RemoveResult = expected };
            var handler = new RemoveBeneficiaryCommandHandler(beneficiaries);

            var result = await handler.Handle(
                new RemoveBeneficiaryCommand("user-1", Guid.NewGuid()), CancellationToken.None);

            Assert.Same(expected, result);
        }
    }
}
