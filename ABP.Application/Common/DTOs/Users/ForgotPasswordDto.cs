using System.Text.Json.Serialization;

namespace ABP.Application.Common.DTOs.Users
{
    public class ForgotPasswordDto
    {
        [JsonPropertyName("userName")]
        public string Username { get; set; } = string.Empty;
    }
}
