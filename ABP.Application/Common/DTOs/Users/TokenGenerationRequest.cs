namespace ABP.Application.Common.DTOs.Users
{
    public class TokenGenerationRequest
    {
        public required string UserId { get; set; }
        public required string UserName { get; set; }
        public required string Role { get; set; }
        public Guid? CommerceId { get; set; }
    }
}
