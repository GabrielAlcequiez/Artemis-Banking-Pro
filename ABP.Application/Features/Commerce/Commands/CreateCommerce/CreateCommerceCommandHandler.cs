using ABP.Application.Common;
using ABP.Application.Common.Interfaces.Services;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using MediatR;
using CommerceEntity = ABP.Domain.Entities.Commerce.Commerce;

namespace ABP.Application.Features.Commerce.Commands.CreateCommerce;

public sealed class CreateCommerceCommandHandler(
    ICommerceRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser)
    : IRequestHandler<CreateCommerceCommand, OperationResult<Guid>>
{
    public async Task<OperationResult<Guid>> Handle(
        CreateCommerceCommand command,
        CancellationToken cancellationToken)
    {
        if (!HasAuthenticatedAdministrator())
        {
            return OperationResult<Guid>.Failure(
                CommerceErrors.AdministratorRequired);
        }

        var request = command.Request;
        var data = CommerceDataNormalizer.Normalize(
            request.Name,
            request.Description,
            request.Email,
            request.PhoneNumber,
            request.Rnc);

        if (await repository.EmailExistsAsync(
                data.Email,
                cancellationToken: cancellationToken))
        {
            return OperationResult<Guid>.Failure(CommerceErrors.DuplicateEmail);
        }

        if (await repository.RncExistsAsync(
                data.Rnc,
                cancellationToken: cancellationToken))
        {
            return OperationResult<Guid>.Failure(CommerceErrors.DuplicateRnc);
        }

        var commerce = new CommerceEntity
        {
            Name = data.Name,
            Description = data.Description,
            Email = data.Email,
            PhoneNumber = data.PhoneNumber,
            Rnc = data.Rnc,
            Status = CommerceStatus.Active
        };

        var createdCommerce = await repository.AddAsync(
            commerce,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult<Guid>.Success(createdCommerce.Id);
    }

    private bool HasAuthenticatedAdministrator() =>
        currentUser.IsAuthenticated &&
        !string.IsNullOrWhiteSpace(currentUser.UserId) &&
        currentUser.IsInRole(Roles.Administrator.ToString());
}
