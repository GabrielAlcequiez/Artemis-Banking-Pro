using System.Text.Json.Serialization;

namespace ABP.Application.Common.DTOs.Users
{
    public class LoginDto
    {
        [JsonPropertyName("userName")]
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}