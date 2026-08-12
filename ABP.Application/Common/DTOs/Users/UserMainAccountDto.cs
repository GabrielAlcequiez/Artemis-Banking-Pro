namespace ABP.Application.Common.DTOs.Users
{
    public class UserMainAccountDto
    {
        public string AccountNumber { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public bool IsPrincipal { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}