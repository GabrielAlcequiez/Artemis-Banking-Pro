using ABP.Application.Common.DTOs.Users;

namespace ABP.Application.Common.Interfaces.Identity
{
    public interface IAccountServiceForWebApi
    {
        Task<AuthenticationResponseDto> LoginAsync(LoginDto loginRequestDto);
    }
}
