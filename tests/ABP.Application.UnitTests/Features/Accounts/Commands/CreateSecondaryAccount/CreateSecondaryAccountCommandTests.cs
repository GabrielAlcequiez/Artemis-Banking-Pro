using ABP.Application.Common;
using ABP.Application.Features.Accounts.Commands.CreateSecondaryAccount;
using ABP.Application.Features.Accounts.DTOs;
using ABP.Application.Features.Accounts.Validation;
using ABP.TestDoubles;

namespace ABP.Application.UnitTests.Features.Accounts.Commands.CreateSecondaryAccount
{
    public sealed class CreateSecondaryAccountCommandTests
    {
        private static CreateSecondaryAccountRequest ValidRequest() => new()
        {
            OwnerUserId = "user-1",
            InitialBalance = 0m,
            ActorUserId = "admin-1",
            ActorRole = "Administrator"
        };

        [Fact]
        public async Task Validator_reuses_shared_request_rules()
        {
            var validator = new CreateSecondaryAccountCommandValidator(
                new CreateSecondaryAccountRequestValidator());
            var invalidRequest = ValidRequest();
            invalidRequest.OwnerUserId = string.Empty;
            var command = new CreateSecondaryAccountCommand(invalidRequest);

            var result = await validator.ValidateAsync(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error => error.PropertyName == "Request.OwnerUserId");
        }

        [Fact]
        public async Task Handler_delegates_to_the_admin_service()
        {
            var expected = OperationResult<Guid>.Success(Guid.NewGuid());
            var adminService = new FakeSavingsAccountAdminService { CreateSecondaryAccountResult = expected };
            var handler = new CreateSecondaryAccountCommandHandler(adminService);

            var result = await handler.Handle(
                new CreateSecondaryAccountCommand(ValidRequest()), CancellationToken.None);

            Assert.Same(expected, result);
        }
    }
}
