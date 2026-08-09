using ABP.Application.Common.DTOs.Users;

namespace ABP.Application.Common.Interfaces.Identity
{
    public interface IJwtTokenService
    {
        string GenerateToken(TokenGenerationRequest request);
    }
}
