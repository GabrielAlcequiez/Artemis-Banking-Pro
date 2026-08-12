using System.Text.Json.Serialization;

namespace ABP.Application.Common.DTOs.Users
{
    public class ChangeUserStatusRequestDto
    {
        [JsonPropertyName("status")]
        public bool? Status { get; set; }
    }
}