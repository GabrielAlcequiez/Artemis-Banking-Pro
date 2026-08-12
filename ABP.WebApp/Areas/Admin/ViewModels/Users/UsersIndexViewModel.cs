using ABP.Application.Common.DTOs.Common;
using ABP.Application.Common.DTOs.Users;

namespace ABP.WebApp.Areas.Admin.ViewModels.Users;

public sealed class UsersIndexViewModel
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string? Role { get; set; }

    public PagedResultDto<GetUserDto>? Result { get; set; }
}
