using ABP.Application.Common.DTOs.Users;

namespace ABP.Application.Common.Interfaces.Identity
{
    public interface IAccountServiceForWebApp : IBaseAccountService
    {
        Task<LoginResponseDto> LoginAsync(LoginDto loginRequestDto);
        Task LogoutAsync();
    }
}