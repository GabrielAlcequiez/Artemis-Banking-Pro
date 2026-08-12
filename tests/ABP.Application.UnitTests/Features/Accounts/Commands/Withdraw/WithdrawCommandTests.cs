using ABP.Application.Common;
using ABP.Application.Features.Accounts.Commands.Withdraw;
using ABP.Application.Features.Accounts.DTOs;
using ABP.Application.Features.Accounts.Validation;
using ABP.TestDoubles;

namespace ABP.Application.UnitTests.Features.Accounts.Commands.Withdraw
{
    public sealed class WithdrawCommandTests
    {
        private static WithdrawalRequest ValidRequest() => new()
        {
            SourceAccountNumber = "100000001",
            Amount = 50m,
            ActorUserId = "cashier-1",
            ActorRole = "Cashier"
        };

        [Fact]
        public async Task Validator_reuses_shared_request_rules()
        {
            var validator = new WithdrawCommandValidator(new WithdrawalRequestValidator());
            var invalidRequest = ValidRequest();
            invalidRequest.Amount = -5m;
            var command = new WithdrawCommand(invalidRequest);

            var result = await validator.ValidateAsync(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error => error.PropertyName == "Request.Amount");
        }

        [Fact]
        public async Task Handler_delegates_to_the_money_transfer_service()
        {
            var fakeReceipt = OperationResult<FinancialOperationReceipt>.Success(
                new FinancialOperationReceipt(Guid.NewGuid(), 50m, DateTimeOffset.UtcNow));
            var moneyTransfer = new FakeMoneyTransferService { WithdrawResult = fakeReceipt };
            var handler = new WithdrawCommandHandler(moneyTransfer);

            var result = await handler.Handle(new WithdrawCommand(ValidRequest()), CancellationToken.None);

            Assert.Same(fakeReceipt, result);
        }
    }
}
