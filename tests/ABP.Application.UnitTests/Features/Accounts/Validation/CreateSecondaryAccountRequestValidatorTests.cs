using ABP.Application.Features.Accounts.DTOs;
using ABP.Application.Features.Accounts.Validation;

namespace ABP.Application.UnitTests.Features.Accounts.Validation
{
    public sealed class CreateSecondaryAccountRequestValidatorTests
    {
        private readonly CreateSecondaryAccountRequestValidator _validator = new();

        private static CreateSecondaryAccountRequest ValidRequest() => new()
        {
            OwnerUserId = "user-1",
            InitialBalance = 0m,
            ActorUserId = "admin-1",
            ActorRole = "Administrator"
        };

        [Fact]
        public void Valid_request_is_accepted()
        {
            var result = _validator.Validate(ValidRequest());
            Assert.True(result.IsValid);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Owner_user_id_is_required(string ownerUserId)
        {
            var request = ValidRequest();
            request.OwnerUserId = ownerUserId;

            var result = _validator.Validate(request);

            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(CreateSecondaryAccountRequest.OwnerUserId));
        }

        [Fact]
        public void Initial_balance_cannot_be_negative()
        {
            var request = ValidRequest();
            request.InitialBalance = -1m;

            var result = _validator.Validate(request);

            Assert.Contains(result.Errors, error =>
                error.PropertyName == nameof(CreateSecondaryAccountRequest.InitialBalance));
        }
    }
}
