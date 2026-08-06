namespace ABP.Application.Common.DTOs.Users
{
    public class CreateUserDto
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Identification { get; set; }
        public required string Email { get; set; }
        public required string UserName { get; set; }
        public required string Password { get; set; }
        public required string ConfirmPassword { get; set; }
        public required string Role { get; set; }

        // Si el usuario es cliente
        public decimal? InitialBalance { get; set; }
    }
}