using System.Text.Json.Serialization;

namespace ABP.Application.Common.DTOs.Users
{
    public class CreateUserDto
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Identification { get; set; }
        public required string Email { get; set; }
        public required string UserName { get; set; }
        public required string Password { get; set; }
        public required string ConfirmPassword { get; set; }
        public required string Role { get; set; }

        // Si el usuario es cliente o comercio
        [JsonIgnore]
        public decimal? InitialBalance { get; set; }

        [JsonPropertyName("initialAmount")]
        public decimal? InitialAmount { get => InitialBalance; set => InitialBalance = value; }
        public Guid? CommerceId { get; set; }
    }
}