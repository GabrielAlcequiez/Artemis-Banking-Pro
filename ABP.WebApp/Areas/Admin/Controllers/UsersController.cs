using ABP.Application.Common.DTOs.Users;
using ABP.Application.Common.Interfaces.Identity;
using ABP.Application.Common.Interfaces.Services;
using ABP.Domain.Enums;
using ABP.WebApp.Areas.Admin.ViewModels.Users;
using ABP.WebApp.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = nameof(Roles.Administrator))]
public sealed class UsersController(
    IAccountServiceForWebApp accountService,
    ICurrentUserService currentUser) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        int pageSize = 20,
        string? role = null)
    {
        if (!TryParseRole(role, out var roleFilter))
        {
            ModelState.AddModelError(
                nameof(role),
                "El filtro de rol debe ser Todos, Administrador, Cajero o Cliente.");
        }

        var model = new UsersIndexViewModel
        {
            Page = page,
            PageSize = pageSize,
            Role = role
        };

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        model.Result = await accountService.GetUsersPagedAsync(new UserQueryFilterDto
        {
            Page = page,
            PageSize = pageSize,
            Role = roleFilter
        });

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateUserViewModel { InitialAmount = 0 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        var uniqueness = await accountService.CheckRegistrationUniquenessAsync(
            model.Identification,
            model.Email,
            model.UserName);
        AddUniquenessErrors(uniqueness);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await accountService.RegisterUserAsync(
            new CreateUserDto
            {
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                Identification = model.Identification.Trim(),
                Email = model.Email.Trim(),
                UserName = model.UserName.Trim(),
                Password = model.Password,
                ConfirmPassword = model.ConfirmPassword,
                Role = model.Role,
                InitialBalance = model.IsClientRole ? model.InitialAmount : null
            },
            origin: $"{Request.Scheme}://{Request.Host}",
            isApi: false);

        if (result.HasError)
        {
            AddOperationErrors(result.ErrorList ?? [result.Error ?? "Ocurrió un error inesperado."]);
            return View(model);
        }

        TempData[SuccessMessageKey] = "El usuario fue creado correctamente. Se ha enviado un correo de activación.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        if (string.Equals(id, currentUser.UserId, StringComparison.Ordinal))
        {
            TempData[ErrorMessageKey] = "No puede editar su propia cuenta desde este módulo.";
            return RedirectToAction(nameof(Index));
        }

        var user = await accountService.GetUserByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        return View(new EditUserViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Identification = user.Identification,
            Email = user.Email,
            UserName = user.UserName,
            Role = RoleNames.ToSpanish(user.Role)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserViewModel model)
    {
        var persisted = await accountService.GetUserByIdAsync(model.Id);
        if (persisted is null)
        {
            return NotFound();
        }

        if (string.Equals(model.Id, currentUser.UserId, StringComparison.Ordinal))
        {
            TempData[ErrorMessageKey] = "No puede editar su propia cuenta desde este módulo.";
            return RedirectToAction(nameof(Index));
        }

        model.Role = RoleNames.ToSpanish(persisted.Role);

        var uniqueness = await accountService.CheckRegistrationUniquenessAsync(
            model.Identification,
            model.Email,
            model.UserName,
            excludeUserId: model.Id);
        AddUniquenessErrors(uniqueness);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await accountService.EditUserAsync(
            new EditUserDto
            {
                Id = model.Id,
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                Identification = model.Identification.Trim(),
                Email = model.Email.Trim(),
                UserName = model.UserName.Trim(),
                Password = string.IsNullOrEmpty(model.Password) ? null : model.Password,
                ConfirmPassword = string.IsNullOrEmpty(model.ConfirmPassword) ? null : model.ConfirmPassword,
                Role = persisted.Role,
                AdditionalAmount = model.AdditionalAmount
            },
            currentUser.UserId ?? string.Empty);

        if (result.HasError)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Ocurrió un error inesperado.");
            return View(model);
        }

        TempData[SuccessMessageKey] = "El usuario fue actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmStatus(string id)
    {
        if (string.Equals(id, currentUser.UserId, StringComparison.Ordinal))
        {
            TempData[ErrorMessageKey] = "No puede modificar el estado de su propia cuenta.";
            return RedirectToAction(nameof(Index));
        }

        var user = await accountService.GetUserByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        return View(ToChangeStatusViewModel(user, !user.IsActive));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(ChangeUserStatusViewModel model)
    {
        var user = await accountService.GetUserByIdAsync(model.Id);
        if (user is null)
        {
            return NotFound();
        }

        CopyUserPresentation(user, model);

        if (!ModelState.IsValid)
        {
            return View(nameof(ConfirmStatus), model);
        }

        var result = await accountService.ChangeUserStatusAsync(
            model.Id,
            model.TargetIsActive,
            currentUser.UserId ?? string.Empty);

        if (result.HasError)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Ocurrió un error inesperado.");
            return View(nameof(ConfirmStatus), model);
        }

        TempData[SuccessMessageKey] = model.TargetIsActive
            ? "El usuario fue activado correctamente."
            : "El usuario fue inactivado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    private void AddOperationErrors(IEnumerable<string> errors)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }
    }

    private void AddUniquenessErrors(UserUniquenessResponseDto uniqueness)
    {
        if (uniqueness.IdentificationError is not null)
        {
            ModelState.AddModelError(
                nameof(CreateUserViewModel.Identification),
                uniqueness.IdentificationError);
        }

        if (uniqueness.EmailError is not null)
        {
            ModelState.AddModelError(
                nameof(CreateUserViewModel.Email),
                uniqueness.EmailError);
        }

        if (uniqueness.UserNameError is not null)
        {
            ModelState.AddModelError(
                nameof(CreateUserViewModel.UserName),
                uniqueness.UserNameError);
        }
    }

    private static ChangeUserStatusViewModel ToChangeStatusViewModel(
        GetUserDto user,
        bool targetIsActive) => new()
        {
            Id = user.Id,
            UserName = user.UserName,
            FullName = $"{user.FirstName} {user.LastName}".Trim(),
            IsActive = user.IsActive,
            TargetIsActive = targetIsActive
        };

    private static void CopyUserPresentation(
        GetUserDto user,
        ChangeUserStatusViewModel model)
    {
        model.UserName = user.UserName;
        model.FullName = $"{user.FirstName} {user.LastName}".Trim();
        model.IsActive = user.IsActive;
    }

    private static bool TryParseRole(string? value, out string? role)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized) ||
            string.Equals(normalized, "Todos", StringComparison.OrdinalIgnoreCase))
        {
            role = null;
            return true;
        }

        role = normalized.ToLowerInvariant() switch
        {
            "administrador" => "Administrador",
            "cajero" => "Cajero",
            "cliente" => "Cliente",
            _ => null
        };

        return role is not null;
    }

    private const string SuccessMessageKey = "SuccessMessage";
    private const string ErrorMessageKey = "ErrorMessage";
}
