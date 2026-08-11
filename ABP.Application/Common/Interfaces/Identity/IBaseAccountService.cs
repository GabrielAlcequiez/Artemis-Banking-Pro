using ABP.Application.Common.DTOs.Users;

namespace ABP.Application.Common.Interfaces.Identity
{
    public interface IBaseAccountService
    {
         Task<RegisterResponseDto> RegisterUserAsync(CreateUserDto createUserDto, string? origin, bool isApi = false);
        Task<UserResponseDto> EditUserAsync(EditUserDto editUserDto, string currentUserId, string? origin = null, bool isApi = false);
        Task<string> ConfirmAccountAsync(string userId, string token);
        Task<string?> ValidateResetTokenAsync(string userId, string token);
        Task<string> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto, string? origin = null, bool isApi = false);
        Task<string> ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
        Task<GetUserDto?> GetUserByIdAsync(string userId);
        Task<GetUserDto?> GetUserByUsernameAsync(string username);
        Task<IReadOnlyList<GetUserDto>> GetAllUsersAsync();
        Task<ABP.Application.Common.DTOs.Common.PagedResultDto<GetUserDto>> GetUsersPagedAsync(UserQueryFilterDto filter);
        Task<UserResponseDto> ChangeUserStatusAsync(string userId, bool isActive, string currentUserId);
    }
}