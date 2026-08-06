namespace ABP.Application.Common.DTOs.Users
{
    public class EditUserDto
    {
         public required string Id { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }   
        public required string Identification { get; set; }
        public required string Email { get; set; }
        public required string UserName { get; set; }
        public required string Password { get; set; }
        public required string ConfirmPassword { get; set; }
        public required string Role { get; set; }

        // Si el usuario es cliente o comercio
        public decimal? InitialBalance { get; set; }     
    }
}