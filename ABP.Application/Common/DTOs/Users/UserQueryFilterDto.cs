namespace ABP.Application.Common.DTOs.Users
{
    public class UserQueryFilterDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Role { get; set; }
        public bool IsCommerceOnly { get; set; } = false;
    }
}
