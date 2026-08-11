using System.ComponentModel.DataAnnotations;

namespace ABP.WebApp.ViewModels;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    public string Username { get; set; } = string.Empty;

    public string? Error { get; set; }
}
