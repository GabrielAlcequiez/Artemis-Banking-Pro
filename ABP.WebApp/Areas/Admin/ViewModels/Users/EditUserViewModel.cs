using System.ComponentModel.DataAnnotations;

namespace ABP.WebApp.Areas.Admin.ViewModels.Users;

public sealed class EditUserViewModel : IValidatableObject
{
    public string Id { get; set; } = string.Empty;

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

    [DataType(DataType.Password)]
    [Display(Name = "Nueva contraseña")]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirmar contraseña")]
    public string? ConfirmPassword { get; set; }

    [Display(Name = "Tipo de usuario")]
    public string Role { get; set; } = string.Empty;

    [Display(Name = "Monto adicional")]
    public decimal? AdditionalAmount { get; set; }

    public bool IsClientRole =>
        string.Equals(Role, "Cliente", StringComparison.OrdinalIgnoreCase);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrEmpty(Password))
        {
            if (string.IsNullOrEmpty(ConfirmPassword))
            {
                yield return new ValidationResult(
                    "Debe confirmar la nueva contraseña.",
                    [nameof(ConfirmPassword)]);
            }
            else if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
            {
                yield return new ValidationResult(
                    "La contraseña y la confirmación de contraseña deben coincidir.",
                    [nameof(ConfirmPassword)]);
            }
        }

        if (AdditionalAmount < 0)
        {
            yield return new ValidationResult(
                "El monto adicional no puede ser negativo.",
                [nameof(AdditionalAmount)]);
        }
    }
}
