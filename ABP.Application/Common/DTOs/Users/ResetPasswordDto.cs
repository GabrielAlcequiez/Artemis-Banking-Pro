namespace ABP.Application.Common.DTOs.Users
{
    public class ResetPasswordDto
    {
        
        public required string UserId { get; set; }
        public required string Token { get; set; }
        public required string Password { get; set; }
        public required string ConfirmPassword {get; set;}
    }
}