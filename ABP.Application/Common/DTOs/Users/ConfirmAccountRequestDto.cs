using System.Text.Json.Serialization;

namespace ABP.Application.Common.DTOs.Users
{
    public class ConfirmAccountRequestDto
    {
        [JsonPropertyName("token")]
        public required string Token { get; set; }
    }
}