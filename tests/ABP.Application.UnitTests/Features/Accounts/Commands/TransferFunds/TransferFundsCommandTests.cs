using ABP.Application.Common;
using ABP.Application.Features.Accounts.Commands.TransferFunds;
using ABP.Application.Features.Accounts.DTOs;
using ABP.Application.Features.Accounts.Validation;
using ABP.Domain.Enums;
using ABP.TestDoubles;

namespace ABP.Application.UnitTests.Features.Accounts.Commands.TransferFunds
{
    public sealed class TransferFundsCommandTests
    {
        private static TransferFundsRequest ValidRequest() => new()
        {
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountNumber = "100000002",
            Amount = 50m,
            OperationType = FinancialOperationType.ExpressTransfer,
            ActorUserId = "user-1",
            ActorRole = "Client"
        };

        [Fact]
        public async Task Validator_reuses_shared_request_rules()
        {
            var validator = new TransferFundsCommandValidator(new TransferFundsRequestValidator());
            var invalidRequest = ValidRequest();
            invalidRequest.Amount = 0m;
            var command = new TransferFundsCommand(invalidRequest);

            var result = await validator.ValidateAsync(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error => error.PropertyName == "Request.Amount");
        }

        [Fact]
        public async Task Handler_delegates_to_the_money_transfer_service()
        {
            var fakeReceipt = OperationResult<FinancialOperationReceipt>.Success(
                new FinancialOperationReceipt(Guid.NewGuid(), 50m, DateTimeOffset.UtcNow));
            var moneyTransfer = new FakeMoneyTransferService { TransferResult = fakeReceipt };
            var handler = new TransferFundsCommandHandler(moneyTransfer);

            var result = await handler.Handle(new TransferFundsCommand(ValidRequest()), CancellationToken.None);

            Assert.Same(fakeReceipt, result);
        }
    }
}
