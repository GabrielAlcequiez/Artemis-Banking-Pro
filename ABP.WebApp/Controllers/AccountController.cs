using ABP.Application.Common.DTOs.Users;
using ABP.Application.Common.Interfaces.Identity;
using ABP.Domain.Enums;
using ABP.WebApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.WebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountServiceForWebApp _accountServiceForWebApp;

        public AccountController(IAccountServiceForWebApp accountServiceForWebApp)
        {
            _accountServiceForWebApp = accountServiceForWebApp;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? reason)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToRoleHome();
            }

            var model = new LoginViewModel();
            if (string.Equals(reason, "unauthorized", StringComparison.OrdinalIgnoreCase))
            {
                model.Error = "No tiene permiso para acceder a esta sección.";
            }

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _accountServiceForWebApp.LoginAsync(new LoginDto
            {
                Username = model.Username,
                Password = model.Password
            });

            if (result.HasError)
            {
                model.Error = result.Error;
                return View(model);
            }

            return RedirectToRoleHome(result.Roles?.FirstOrDefault());
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Activate(string userId, string token)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            {
                return View(new AccountMessageViewModel { Message = "El enlace de activación no es válido." });
            }

            var error = await _accountServiceForWebApp.ConfirmAccountAsync(userId, token);
            if (string.IsNullOrEmpty(error))
            {
                TempData["AccountMessage"] = "Su cuenta ha sido activada correctamente. Ya puede iniciar sesión.";
                return RedirectToAction(nameof(Login));
            }

            return View(new AccountMessageViewModel { Message = MapActivationError(error) });
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var error = await _accountServiceForWebApp.ForgotPasswordAsync(
                new ForgotPasswordDto { Username = model.Username },
                origin: $"{Request.Scheme}://{Request.Host}");

            if (string.IsNullOrEmpty(error))
            {
                TempData["AccountMessage"] = "Se ha enviado un enlace de restablecimiento de contraseña al correo electrónico registrado.";
                return RedirectToAction(nameof(Login));
            }

            model.Error = error;
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(string userId, string token)
        {
            var model = new ResetPasswordViewModel
            {
                UserId = userId,
                Token = token
            };

            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            {
                model.TokenError = "El enlace de restablecimiento no es válido.";
                return View(model);
            }

            var error = await _accountServiceForWebApp.ValidateResetTokenAsync(userId, token);
            if (!string.IsNullOrEmpty(error))
            {
                model.TokenError = error;
            }

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var error = await _accountServiceForWebApp.ResetPasswordAsync(new ResetPasswordDto
            {
                UserId = model.UserId,
                Token = model.Token,
                Password = model.Password,
                ConfirmPassword = model.ConfirmPassword
            });

            if (string.IsNullOrEmpty(error))
            {
                TempData["AccountMessage"] = "Su contraseña ha sido restablecida correctamente. Ya puede iniciar sesión.";
                return RedirectToAction(nameof(Login));
            }

            model.Error = error;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _accountServiceForWebApp.LogoutAsync();
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private IActionResult RedirectToRoleHome(string? role = null)
        {
            var roleName = role ?? GetCurrentRole();

            switch (roleName)
            {
                case nameof(Roles.Administrator):
                    return RedirectToAction("Index", "Home", new { area = "Admin" });
                case nameof(Roles.Cashier):
                    return RedirectToAction("Index", "Home", new { area = "Cashier" });
                case nameof(Roles.Client):
                    return RedirectToAction("Index", "Home", new { area = "Client" });
                default:
                    return RedirectToAction(nameof(AccessDenied));
            }
        }

        private string? GetCurrentRole()
        {
            if (User.IsInRole(Roles.Administrator.ToString()))
            {
                return Roles.Administrator.ToString();
            }

            if (User.IsInRole(Roles.Cashier.ToString()))
            {
                return Roles.Cashier.ToString();
            }

            if (User.IsInRole(Roles.Client.ToString()))
            {
                return Roles.Client.ToString();
            }

            return null;
        }

        private static string MapActivationError(string error)
        {
            if (error.Contains("ya ha sido utilizado", StringComparison.OrdinalIgnoreCase))
            {
                return "Este enlace de activación ya fue utilizado.";
            }

            if (error.Contains("ya ha sido confirmada", StringComparison.OrdinalIgnoreCase))
            {
                return "La cuenta ya ha sido confirmada previamente.";
            }

            return "El enlace de activación no es válido.";
        }
    }
}
