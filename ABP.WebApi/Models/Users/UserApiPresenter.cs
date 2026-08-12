using ABP.Application.Common.DTOs.Common;
using ABP.Application.Common.DTOs.Users;
using ABP.Domain.Enums;

namespace ABP.WebApi.Models.Users
{
    // esto se encarga de traducir nombres y roles de la API a los nombres que se muestran en el front o api
    public static class UserApiPresenter
    {
        public static UserListItemApiResponse ToApiItem(this GetUserDto dto) => new()
        {
            Id = dto.Id,
            UserName = dto.UserName,
            Identification = dto.Identification,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            Role = RoleNames.ToSpanish(dto.Role),
            IsActive = dto.IsActive,
            CommerceId = dto.CommerceId,
            CommerceName = dto.CommerceName
        };

        public static PagedResultDto<UserListItemApiResponse> ToApiPage(this PagedResultDto<GetUserDto> page) => new()
        {
            Page = page.Page,
            PageSize = page.PageSize,
            TotalRecords = page.TotalRecords,
            TotalPages = page.TotalPages,
            Data = page.Data.Select(x => x.ToApiItem()).ToList()
        };

        public static CreateUserApiResponse ToCreated(CreateUserDto dto, RegisterResponseDto result) => new()
        {
            Id = result.Id,
            UserName = dto.UserName,
            Email = dto.Email,
            Role = RoleNames.ToSpanish(dto.Role),
            IsActive = false,
            CommerceId = dto.CommerceId
        };

        public static CreateUserApiResponse ToCreated(CreateCommerceUserRequestDto dto, RegisterResponseDto result, Guid commerceId) => new()
        {
            Id = result.Id,
            UserName = dto.UserName,
            Email = dto.Email,
            Role = RoleNames.ToSpanish(Roles.Commerce.ToString()),
            IsActive = false,
            CommerceId = commerceId
        };

        public static UserDetailApiResponse ToApiDetail(this UserDetailDto dto) => new()
        {
            Id = dto.Id,
            UserName = dto.UserName,
            Identification = dto.Identification,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            Role = RoleNames.ToSpanish(dto.Role),
            IsActive = dto.IsActive,
            CreatedAtUtc = dto.CreatedAtUtc,
            MainAccount = dto.MainAccount is null
                ? null
                : new UserMainAccountApiResponse
                {
                    AccountNumber = dto.MainAccount.AccountNumber,
                    Balance = dto.MainAccount.Balance,
                    IsPrincipal = dto.MainAccount.IsPrincipal,
                    Status = AccountStatusNames.ToSpanish(dto.MainAccount.Status)
                }
        };
    }



    // Nombres de rol según el documento funcional (español).

    public static class RoleNames
    {
        public static string ToSpanish(string role) => role switch
        {
            nameof(Roles.Administrator) => "Administrador",
            nameof(Roles.Cashier) => "Cajero",
            nameof(Roles.Client) => "Cliente",
            nameof(Roles.Commerce) => "Comercio",
            _ => role
        };
    }

    // Nombres de estado de cuenta de ahorro según el documento funcional.

    public static class AccountStatusNames
    {
        public static string ToSpanish(string status) => status switch
        {
            nameof(SavingsAccountStatus.Active) => "Activa",
            nameof(SavingsAccountStatus.Cancelled) => "Cancelada",
            _ => status
        };
    }
}