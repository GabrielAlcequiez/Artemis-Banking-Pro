namespace ABP.Application.Common.DTOs
{
    public class JwtResponseDto
    {
        public bool HasError { get; set; }
        public string? Error { get; set; }
    }
}
