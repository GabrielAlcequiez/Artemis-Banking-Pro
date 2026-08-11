using System.ComponentModel.DataAnnotations;

namespace ABP.WebApp.ViewModels;

public class ResetPasswordViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "La confirmación de contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "La contraseña y la confirmación de contraseña deben coincidir.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? TokenError { get; set; }

    public string? Error { get; set; }
}
