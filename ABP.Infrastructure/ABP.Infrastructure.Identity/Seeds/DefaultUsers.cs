using ABP.Application.Features.Accounts;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Domain.Entities;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace ABP.Infrastructure.Identity.Seeds;

public static class DefaultUsers
{
    private const string DefaultPasswordConfigurationKey = "SeedUsers:DefaultPassword";

    private static readonly SeedUserDefinition[] SeedUsers =
    [
        // Usuarios de la aplicación web (MVC).
        new(
            UserName: "admin",
            Email: "admin@artemisbanking.com",
            Name: "Default",
            LastName: "Administrator",
            Identification: "00000000001",
            Role: Roles.Administrator),
        new(
            UserName: "cashier",
            Email: "cashier@artemisbanking.com",
            Name: "Default",
            LastName: "Cashier",
            Identification: "00000000002",
            Role: Roles.Cashier),
        new(
            UserName: "client",
            Email: "client@artemisbanking.com",
            Name: "Default",
            LastName: "Client",
            Identification: "00000000003",
            Role: Roles.Client),
        // Usuarios por defecto de la Web API (Administrador y Comercio).
        new(
            UserName: "adminapi",
            Email: "adminapi@artemisbanking.com",
            Name: "Default",
            LastName: "Api Administrator",
            Identification: "00000000004",
            Role: Roles.Administrator),
        new(
            UserName: "commerceapi",
            Email: "commerceapi@artemisbanking.com",
            Name: "Default",
            LastName: "Api Commerce",
            Identification: "00000000005",
            Role: Roles.Commerce)
    ];

    public static async Task SeedDefaultUsersAsync(
        UserManager<AppUser> userManager,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPrimaryAccountProvisioner primaryAccountProvisioner,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var defaultPassword = configuration[DefaultPasswordConfigurationKey];

        if (string.IsNullOrWhiteSpace(defaultPassword))
        {
            throw new InvalidOperationException(
                $"The configuration value '{DefaultPasswordConfigurationKey}' is required.");
        }

        foreach (var definition in SeedUsers)
        {
            await SeedUserAsync(
                userManager,
                userRepository,
                definition,
                defaultPassword,
                cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var definition in SeedUsers.Where(x => x.Role is Roles.Client or Roles.Commerce))
        {
            var appUser = await userManager.FindByNameAsync(definition.UserName);
            if (appUser is null)
            {
                continue;
            }

            await ProvisionPrincipalAccountAsync(
                primaryAccountProvisioner,
                appUser.Id,
                cancellationToken);
        }
    }

    private static async Task ProvisionPrincipalAccountAsync(
        IPrimaryAccountProvisioner primaryAccountProvisioner,
        string ownerUserId,
        CancellationToken cancellationToken)
    {
        var result = await primaryAccountProvisioner.OpenPrincipalAccountAsync(
            ownerUserId,
            initialBalance: 0m,
            actorUserId: "system",
            actorRole: Roles.Administrator.ToString(),
            cancellationToken);

        if (result.IsFailure &&
            !string.Equals(result.Error.Code, AccountErrors.PrincipalAlreadyExists.Code, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(result.Error.Code, "accounts.principal_already_exists", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(result.Error.Description);
        }
    }

    private static async Task SeedUserAsync(
        UserManager<AppUser> userManager,
        IUserRepository userRepository,
        SeedUserDefinition definition,
        string password,
        CancellationToken cancellationToken)
    {
        var userByName = await userManager.FindByNameAsync(definition.UserName);
        var userByEmail = await userManager.FindByEmailAsync(definition.Email);

        if (userByName is not null &&
            userByEmail is not null &&
            userByName.Id != userByEmail.Id)
        {
            throw new InvalidOperationException(
                $"The seed username '{definition.UserName}' and email '{definition.Email}' belong to different users.");
        }

        var appUser = userByName ?? userByEmail;

        if (appUser is null)
        {
            appUser = new AppUser
            {
                UserName = definition.UserName,
                Email = definition.Email,
                EmailConfirmed = true,
                IsActive = true
            };

            var createResult = await userManager.CreateAsync(appUser, password);
            EnsureSucceeded(
                createResult,
                $"create seed user '{definition.UserName}'");
        }
        else
        {
            EnsureMatchesDefinition(appUser, definition);
        }

        var roleName = definition.Role.ToString();
        if (!await userManager.IsInRoleAsync(appUser, roleName))
        {
            var roleResult = await userManager.AddToRoleAsync(appUser, roleName);
            EnsureSucceeded(
                roleResult,
                $"assign role '{roleName}' to seed user '{definition.UserName}'");
        }

        if (await userRepository.GetByIdAsync(appUser.Id, cancellationToken) is not null)
        {
            return;
        }

        var domainUser = new User(appUser.Id)
        {
            Name = definition.Name,
            LastName = definition.LastName,
            Email = definition.Email,
            UserName = definition.UserName,
            Identification = definition.Identification,
            Role = definition.Role,
            IsActive = true
        };

        await userRepository.AddAsync(domainUser, cancellationToken);
    }

    private static void EnsureMatchesDefinition(
        AppUser appUser,
        SeedUserDefinition definition)
    {
        if (!string.Equals(
                appUser.UserName,
                definition.UserName,
                StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(
                appUser.Email,
                definition.Email,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Existing Identity user '{appUser.Id}' does not match the seed definition for '{definition.UserName}'.");
        }
    }

    private static void EnsureSucceeded(
        IdentityResult result,
        string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join(
            "; ",
            result.Errors.Select(error => $"{error.Code}: {error.Description}"));

        throw new InvalidOperationException(
            $"Unable to {operation}. {errors}");
    }

    private sealed record SeedUserDefinition(
        string UserName,
        string Email,
        string Name,
        string LastName,
        string Identification,
        Roles Role);
}
