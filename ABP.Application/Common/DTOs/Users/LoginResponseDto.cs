namespace ABP.Application.Common.DTOs.Users
{
    public class LoginResponseDto
    {
         public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsVerified { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Identification { get; set; } = string.Empty;
        public List<string>? Roles { get; set; }
    }
}