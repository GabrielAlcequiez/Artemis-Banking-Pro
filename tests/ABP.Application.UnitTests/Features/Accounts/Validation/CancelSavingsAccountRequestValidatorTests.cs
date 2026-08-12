using ABP.Application.Features.Accounts.DTOs;
using ABP.Application.Features.Accounts.Validation;

namespace ABP.Application.UnitTests.Features.Accounts.Validation
{
    public sealed class CancelSavingsAccountRequestValidatorTests
    {
        private readonly CancelSavingsAccountRequestValidator _validator = new();

        private static CancelSavingsAccountRequest ValidRequest() => new()
        {
            AccountId = Guid.NewGuid(),
            ActorUserId = "admin-1",
            ActorRole = "Administrator"
        };

        [Fact]
        public void Valid_request_is_accepted()
        {
            var result = _validator.Validate(ValidRequest());
            Assert.True(result.IsValid);
        }

        [Fact]
        public void Account_id_is_required()
        {
            var request = ValidRequest();
            request.AccountId = Guid.Empty;

            var result = _validator.Validate(request);

            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(CancelSavingsAccountRequest.AccountId));
        }
    }
}
