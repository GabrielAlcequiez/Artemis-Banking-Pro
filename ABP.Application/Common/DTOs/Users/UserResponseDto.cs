namespace ABP.Application.Common.DTOs.Users
{
    public class UserResponseDto
    {
        public bool HasError { get; set; }
        public string? Error { get; set; }
        public string? ErrorMessage { get; set; }
    }
}