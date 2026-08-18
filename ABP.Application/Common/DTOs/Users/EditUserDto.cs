using System.Text.Json.Serialization;

namespace ABP.Application.Common.DTOs.Users
{
    public class EditUserDto
    {
        // El id viene de la ruta directamente y el controller lo asigna.
        [JsonIgnore]
        public string Id { get; set; } = string.Empty;
        public required string FirstName { get; set; }
        public required string LastName { get; set; }   
        public required string Identification { get; set; }
        public required string Email { get; set; }
        public required string UserName { get; set; }
        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }
        public string? Role { get; set; }

        // No usado por la API (el contrato de edición usa additionalAmount)
        [JsonIgnore]
        public decimal? InitialBalance { get; set; }
        public decimal? AdditionalAmount { get; set; }
    }
}
