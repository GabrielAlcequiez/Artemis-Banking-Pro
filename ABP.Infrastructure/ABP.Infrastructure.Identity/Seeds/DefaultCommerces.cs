using ABP.Domain.Entities.Commerce;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;

namespace ABP.Infrastructure.Identity.Seeds;

public static class DefaultCommerces
{
    private static readonly CommerceDefinition[] DefaultCommerceDefinitions =
    [
        new(
            Name: "Tienda Demo",
            Description: "Comercio de demostración para pruebas de la API.",
            Email: "tiendademo@artemisbanking.com",
            PhoneNumber: "8090000000",
            Rnc: "000000000")
    ];

    public static async Task<Guid> SeedDefaultCommercesAsync(
        ICommerceRepository commerceRepository,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken = default)
    {
        Guid? firstCommerceId = null;

        foreach (var definition in DefaultCommerceDefinitions)
        {
            var existing = (await commerceRepository.GetAllAsync(false, cancellationToken))
                .FirstOrDefault(commerce =>
                    string.Equals(commerce.Name, definition.Name, StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
            {
                firstCommerceId ??= existing.Id;
                continue;
            }

            var commerce = new Commerce
            {
                Name = definition.Name,
                Description = definition.Description,
                Email = definition.Email,
                PhoneNumber = definition.PhoneNumber,
                Rnc = definition.Rnc,
                Status = CommerceStatus.Active
            };

            await commerceRepository.AddAsync(commerce, cancellationToken);
            firstCommerceId ??= commerce.Id;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return firstCommerceId!.Value;
    }

    public static async Task LinkCommerceApiUserAsync(
        UserManager<AppUser> userManager,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        Guid commerceId,
        CancellationToken cancellationToken = default)
    {
        var appUser = await userManager.FindByNameAsync("commerceapi");
        if (appUser is null)
        {
            return;
        }

        var domainUser = await userRepository.GetByIdAsync(appUser.Id, cancellationToken);
        if (domainUser is null || domainUser.CommerceId.HasValue)
        {
            return;
        }

        domainUser.CommerceId = commerceId;
        await userRepository.UpdateAsync(appUser.Id, domainUser, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private sealed record CommerceDefinition(
        string Name,
        string? Description,
        string Email,
        string PhoneNumber,
        string Rnc);
}