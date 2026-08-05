namespace ABP.Application.DTOs;

public class JwtResponseDto
{
    public bool HasError { get; set; }
    public string? Error { get; set; }
}
