using ABP.Application.Common;
using ABP.Application.Features.Accounts.Commands.CancelSavingsAccount;
using ABP.Application.Features.Accounts.DTOs;
using ABP.Application.Features.Accounts.Validation;
using ABP.TestDoubles;

namespace ABP.Application.UnitTests.Features.Accounts.Commands.CancelSavingsAccount
{
    public sealed class CancelSavingsAccountCommandTests
    {
        private static CancelSavingsAccountRequest ValidRequest() => new()
        {
            AccountId = Guid.NewGuid(),
            ActorUserId = "admin-1",
            ActorRole = "Administrator"
        };

        [Fact]
        public async Task Validator_reuses_shared_request_rules()
        {
            var validator = new CancelSavingsAccountCommandValidator( new CancelSavingsAccountRequestValidator());
            var invalidRequest = ValidRequest();
            invalidRequest.AccountId = Guid.Empty;
            var command = new CancelSavingsAccountCommand(invalidRequest);

            var result = await validator.ValidateAsync(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error => error.PropertyName == "Request.AccountId");
        }

        [Fact]
        public async Task Handler_delegates_to_the_admin_service()
        {
            var expected = OperationResult.Failure(new Error("accounts.not_found", "not found"));
            var adminService = new FakeSavingsAccountAdminService { CancelResult = expected };
            var handler = new CancelSavingsAccountCommandHandler(adminService);

            var result = await handler.Handle(
                new CancelSavingsAccountCommand(ValidRequest()), CancellationToken.None);

            Assert.Same(expected, result);
        }
    }
}
