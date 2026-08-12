using ABP.Application.Exceptions;
using ABP.Application.Features.Commerce;
using ABP.Application.Features.Commerce.Commands.ChangeCommerceStatus;
using ABP.Application.Features.Commerce.DTOs;
using ABP.Application.Features.Commerce.Validation;
using ABP.Domain.Enums;
using CommerceEntity = ABP.Domain.Entities.Commerce.Commerce;

namespace ABP.Application.UnitTests.Features.Commerce.Commands;

public sealed class ChangeCommerceStatusCommandTests
{
    [Fact]
    public async Task Validator_reuses_shared_status_rules()
    {
        var validator = new ChangeCommerceStatusCommandValidator(
            new ChangeCommerceStatusRequestValidator());
        var command = new ChangeCommerceStatusCommand(
            new ChangeCommerceStatusRequest(Guid.Empty, false));

        var result = await validator.ValidateAsync(command);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == "Request.CommerceId");
    }

    [Fact]
    public async Task Deactivate_sets_status_and_delegates_the_atomic_commit()
    {
        var commerceId = Guid.NewGuid();
        var commerce = CreateCommerce(CommerceStatus.Active);
        var repository = new CommerceRepositoryStub { CommerceForUpdate = commerce };
        var unitOfWork = new CommerceUnitOfWorkStub();
        var inactivation = new CommerceUserInactivationServiceStub();
        var handler = CreateHandler(repository, unitOfWork, inactivation);

        var result = await handler.Handle(
            Command(commerceId, isActive: false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(CommerceStatus.Inactive, commerce.Status);
        Assert.Equal(commerceId, inactivation.ReceivedCommerceId);
        Assert.Equal(1, inactivation.Calls);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Repeated_deactivation_repairs_any_inconsistent_active_users()
    {
        var commerceId = Guid.NewGuid();
        var repository = new CommerceRepositoryStub
        {
            CommerceForUpdate = CreateCommerce(CommerceStatus.Inactive)
        };
        var inactivation = new CommerceUserInactivationServiceStub();
        var handler = CreateHandler(
            repository,
            new CommerceUnitOfWorkStub(),
            inactivation);

        var result = await handler.Handle(
            Command(commerceId, isActive: false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, inactivation.Calls);
    }

    [Fact]
    public async Task Reactivate_only_activates_commerce_and_never_its_users()
    {
        var commerce = CreateCommerce(CommerceStatus.Inactive);
        var repository = new CommerceRepositoryStub { CommerceForUpdate = commerce };
        var unitOfWork = new CommerceUnitOfWorkStub();
        var inactivation = new CommerceUserInactivationServiceStub();
        var handler = CreateHandler(repository, unitOfWork, inactivation);

        var result = await handler.Handle(
            Command(Guid.NewGuid(), isActive: true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(CommerceStatus.Active, commerce.Status);
        Assert.Equal(1, unitOfWork.SaveCalls);
        Assert.Equal(0, inactivation.Calls);
    }

    [Fact]
    public async Task Repeated_activation_is_a_no_op()
    {
        var repository = new CommerceRepositoryStub
        {
            CommerceForUpdate = CreateCommerce(CommerceStatus.Active)
        };
        var unitOfWork = new CommerceUnitOfWorkStub();
        var inactivation = new CommerceUserInactivationServiceStub();
        var handler = CreateHandler(repository, unitOfWork, inactivation);

        var result = await handler.Handle(
            Command(Guid.NewGuid(), isActive: true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, unitOfWork.SaveCalls);
        Assert.Equal(0, inactivation.Calls);
    }

    [Fact]
    public async Task Missing_commerce_returns_not_found_without_side_effects()
    {
        var unitOfWork = new CommerceUnitOfWorkStub();
        var inactivation = new CommerceUserInactivationServiceStub();
        var handler = CreateHandler(
            new CommerceRepositoryStub(),
            unitOfWork,
            inactivation);

        var result = await handler.Handle(
            Command(Guid.NewGuid(), isActive: false),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CommerceErrors.NotFound, result.Error);
        Assert.Equal(0, unitOfWork.SaveCalls);
        Assert.Equal(0, inactivation.Calls);
    }

    [Fact]
    public async Task Handler_requires_authenticated_administrator()
    {
        var repository = new CommerceRepositoryStub
        {
            CommerceForUpdate = CreateCommerce(CommerceStatus.Active)
        };
        var inactivation = new CommerceUserInactivationServiceStub();
        var handler = CreateHandler(
            repository,
            new CommerceUnitOfWorkStub(),
            inactivation,
            CommerceCurrentUserStub.Client());

        var result = await handler.Handle(
            Command(Guid.NewGuid(), isActive: false),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CommerceErrors.AdministratorRequired, result.Error);
        Assert.Null(repository.ReceivedCommerceId);
        Assert.Equal(0, inactivation.Calls);
    }

    [Fact]
    public async Task Concurrency_failure_from_atomic_deactivation_is_propagated()
    {
        var repository = new CommerceRepositoryStub
        {
            CommerceForUpdate = CreateCommerce(CommerceStatus.Active)
        };
        var inactivation = new CommerceUserInactivationServiceStub
        {
            Exception = new FinancialConcurrencyException(
                new InvalidOperationException("stale rowversion"))
        };
        var handler = CreateHandler(
            repository,
            new CommerceUnitOfWorkStub(),
            inactivation);

        await Assert.ThrowsAsync<FinancialConcurrencyException>(
            () => handler.Handle(
                Command(Guid.NewGuid(), isActive: false),
                CancellationToken.None));
    }

    private static ChangeCommerceStatusCommandHandler CreateHandler(
        CommerceRepositoryStub repository,
        CommerceUnitOfWorkStub unitOfWork,
        CommerceUserInactivationServiceStub inactivation,
        CommerceCurrentUserStub? currentUser = null) =>
        new(
            repository,
            unitOfWork,
            inactivation,
            currentUser ?? CommerceCurrentUserStub.Administrator());

    private static ChangeCommerceStatusCommand Command(
        Guid commerceId,
        bool isActive) =>
        new(new ChangeCommerceStatusRequest(commerceId, isActive));

    private static CommerceEntity CreateCommerce(CommerceStatus status) => new()
    {
        Name = "Tienda Demo",
        Email = "contacto@tiendademo.com",
        PhoneNumber = "8095551234",
        Rnc = "101999999",
        Status = status
    };
}
