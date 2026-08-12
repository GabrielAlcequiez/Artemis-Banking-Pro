using System.ComponentModel.DataAnnotations;

namespace ABP.WebApp.Areas.Admin.ViewModels.Users;

public sealed class CreateUserViewModel : IValidatableObject
{
    public static readonly string[] ValidRoles = ["Administrador", "Cajero", "Cliente"];

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [Display(Name = "Nombre")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [Display(Name = "Apellido")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La cédula es obligatoria.")]
    [Display(Name = "Cédula")]
    public string Identification { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del correo electrónico es inválido.")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    [Display(Name = "Nombre de usuario")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "La confirmación de contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "La contraseña y la confirmación de contraseña deben coincidir.")]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo de usuario es obligatorio.")]
    [Display(Name = "Tipo de usuario")]
    public string Role { get; set; } = string.Empty;

    [Display(Name = "Monto inicial")]
    public decimal? InitialAmount { get; set; }

    public bool IsClientRole =>
        string.Equals(Role, "Cliente", StringComparison.OrdinalIgnoreCase);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrEmpty(Role) && !ValidRoles.Contains(Role))
        {
            yield return new ValidationResult(
                "El tipo de usuario debe ser Administrador, Cajero o Cliente.",
                [nameof(Role)]);
        }

        if (IsClientRole && InitialAmount < 0)
        {
            yield return new ValidationResult(
                "El monto inicial no puede ser negativo.",
                [nameof(InitialAmount)]);
        }
    }
}
