using System.Text.Json.Serialization;

namespace ABP.Application.Common.DTOs.Users
{
    public class UserResponseDto
    {
        public bool HasError { get; set; }
        public string? Error { get; set; }
        public string? ErrorMessage { get; set; }

        [JsonIgnore]
        public bool IsConflict { get; set; }

        [JsonIgnore]
        public bool IsNotFound { get; set; }

        [JsonIgnore]
        public bool IsForbidden { get; set; }
    }
}