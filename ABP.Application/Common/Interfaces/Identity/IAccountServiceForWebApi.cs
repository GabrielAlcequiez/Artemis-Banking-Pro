using ABP.Application.Common.DTOs.Users;

namespace ABP.Application.Common.Interfaces.Identity
{
    public interface IAccountServiceForWebApi : IBaseAccountService
    {
        Task<AuthenticationResponseDto> LoginAsync(LoginDto loginRequestDto);

        Task ConfirmAccountAsync(ConfirmAccountRequestDto request);

        Task GetResetTokenAsync(ForgotPasswordDto forgotPasswordDto);

        Task ChangeUserStatusAsync(string userId, ChangeUserStatusRequestDto request, string currentUserId);
    }
}