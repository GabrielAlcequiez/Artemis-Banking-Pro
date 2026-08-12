using ABP.Domain.Enums;

namespace ABP.WebApp.Helpers;
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
