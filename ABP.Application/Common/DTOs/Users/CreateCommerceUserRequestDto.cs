using System.Text.Json.Serialization;

namespace ABP.Application.Common.DTOs.Users
{
    public class CreateCommerceUserRequestDto
    {
        [JsonPropertyName("firstName")]
        public required string FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public required string LastName { get; set; }

        [JsonPropertyName("identification")]
        public required string Identification { get; set; }

        [JsonPropertyName("email")]
        public required string Email { get; set; }

        [JsonPropertyName("userName")]
        public required string UserName { get; set; }

        [JsonPropertyName("password")]
        public required string Password { get; set; }

        [JsonPropertyName("confirmPassword")]
        public required string ConfirmPassword { get; set; }

        [JsonPropertyName("initialAmount")]
        public decimal? InitialAmount { get; set; }
    }
}