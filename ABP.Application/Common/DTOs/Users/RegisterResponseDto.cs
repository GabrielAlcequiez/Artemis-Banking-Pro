using System.Text.Json.Serialization;

namespace ABP.Application.Common.DTOs.Users
{
    public class RegisterResponseDto
    {
        public required string Id { get; set; }
        public bool IsVerified { get; set; }
        public bool HasError { get; set; }
        public string? Error { get; set; }
        public List<string>? ErrorList { get; set; }

        [JsonIgnore]
        public bool IsConflict { get; set; }

        [JsonIgnore]
        public bool IsNotFound { get; set; }
    }
}