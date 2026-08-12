using ABP.Application.Features.Accounts.DTOs;
using ABP.Application.Features.Accounts.Validation;
using ABP.Domain.Enums;

namespace ABP.Application.UnitTests.Features.Accounts.Validation
{
    public sealed class TransferFundsRequestValidatorTests
    {
        private readonly TransferFundsRequestValidator _validator = new();

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
        public void Valid_request_is_accepted()
        {
            // Given
            var request = ValidRequest();
            // When
            var result = _validator.Validate(request);
            // Then
            Assert.True(result.IsValid);
        }

        [Fact]
        public void Source_account_id_is_required()
        {
            // Arrange
            var request = ValidRequest();
            request.SourceAccountId = Guid.Empty;

            // Act
            var result = _validator.Validate(request);

            // Assert
            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(TransferFundsRequest.SourceAccountId));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Amount_must_be_positive(decimal amount)
        {
            var request = ValidRequest();
            request.Amount = amount;

            var result = _validator.Validate(request);

            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(TransferFundsRequest.Amount));
        }

        [Fact]
        public void Operation_type_must_be_a_transfer_type()
        {
            var request = ValidRequest();
            request.OperationType = FinancialOperationType.Deposit;

            var result = _validator.Validate(request);

            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(TransferFundsRequest.OperationType));
        }

        [Fact]
        public void Destination_account_number_or_id_is_required()
        {
            var request = ValidRequest();
            request.DestinationAccountNumber = null;
            request.DestinationAccountId = null;

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void Destination_account_id_alone_satisfies_the_rule()
        {
            var request = ValidRequest();
            request.DestinationAccountNumber = null;
            request.DestinationAccountId = Guid.NewGuid();

            var result = _validator.Validate(request);

            Assert.True(result.IsValid);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Actor_user_id_is_required(string actorUserId)
        {
            var request = ValidRequest();
            request.ActorUserId = actorUserId;

            var result = _validator.Validate(request);

            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(TransferFundsRequest.ActorUserId));
        }
    }
}
