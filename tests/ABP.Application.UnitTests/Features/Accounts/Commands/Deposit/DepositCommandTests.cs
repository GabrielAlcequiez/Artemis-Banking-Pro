using ABP.Application.Common;
using ABP.Application.Features.Accounts.Commands.Deposit;
using ABP.Application.Features.Accounts.DTOs;
using ABP.Application.Features.Accounts.Validation;
using ABP.TestDoubles;

namespace ABP.Application.UnitTests.Features.Accounts.Commands.Deposit
{
    public sealed class DepositCommandTests
    {
        private static DepositRequest ValidRequest() => new()
        {
            DestinationAccountNumber = "100000001",
            Amount = 100m,
            ActorUserId = "cashier-1",
            ActorRole = "Cashier"
        };

        [Fact]
        public async Task Validator_reuses_shared_request_rules()
        {
            var validator = new DepositCommandValidator(new DepositRequestValidator());
            var invalidRequest = ValidRequest();
            invalidRequest.DestinationAccountNumber = string.Empty;
            var command = new DepositCommand(invalidRequest);

            var result = await validator.ValidateAsync(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error => error.PropertyName == "Request.DestinationAccountNumber");
        }

        [Fact]
        public async Task Handler_delegates_to_the_money_transfer_service()
        {
            var fakeReceipt = OperationResult<FinancialOperationReceipt>.Success(
                new FinancialOperationReceipt(Guid.NewGuid(), 100m, DateTimeOffset.UtcNow));
            var moneyTransfer = new FakeMoneyTransferService { DepositResult = fakeReceipt };
            var handler = new DepositCommandHandler(moneyTransfer);

            var result = await handler.Handle(new DepositCommand(ValidRequest()), CancellationToken.None);

            Assert.Same(fakeReceipt, result);
        }
    }
}
