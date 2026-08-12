using ABP.Application.Common;

namespace ABP.Application.Features.Commerce;

public static class CommerceErrors
{
    public static readonly Error NotFound = new(
        "Commerce.NotFound",
        "El comercio indicado no existe.");

    public static readonly Error DuplicateEmail = new(
        "Commerce.DuplicateEmail",
        "Ya existe un comercio con el mismo correo electrónico.");

    public static readonly Error DuplicateRnc = new(
        "Commerce.DuplicateRnc",
        "Ya existe un comercio con el mismo RNC.");

    public static readonly Error AdministratorRequired = new(
        "Commerce.AdministratorRequired",
        "Se requiere un administrador autenticado para administrar comercios.");

    public static readonly Error ConcurrencyConflict = new(
        "Commerce.ConcurrencyConflict",
        "El comercio fue modificado por otra operación. Intente nuevamente.");
}
