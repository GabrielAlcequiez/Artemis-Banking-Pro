using ABP.Application.Exceptions;
using ABP.Application.Features.Commerce;
using ABP.Application.Features.Commerce.Commands.UpdateCommerce;
using ABP.Application.Features.Commerce.DTOs;
using ABP.Application.Features.Commerce.Validation;
using ABP.Domain.Enums;
using CommerceEntity = ABP.Domain.Entities.Commerce.Commerce;

namespace ABP.Application.UnitTests.Features.Commerce.Commands;

public sealed class UpdateCommerceCommandTests
{
    [Fact]
    public async Task Validator_reuses_shared_update_rules()
    {
        var validator = new UpdateCommerceCommandValidator(
            new UpdateCommerceRequestValidator());
        var command = new UpdateCommerceCommand(
            new UpdateCommerceRequest(Guid.Empty, "", null, "invalid", "", ""));

        var result = await validator.ValidateAsync(command);

        Assert.Contains(result.Errors, error => error.PropertyName == "Request.CommerceId");
        Assert.Contains(result.Errors, error => error.PropertyName == "Request.Name");
        Assert.Contains(result.Errors, error => error.PropertyName == "Request.Email");
    }

    [Fact]
    public async Task Handler_updates_normalized_data_without_changing_status()
    {
        var commerceId = Guid.NewGuid();
        var commerce = CreateCommerce(CommerceStatus.Inactive);
        var repository = new CommerceRepositoryStub { CommerceForUpdate = commerce };
        var unitOfWork = new CommerceUnitOfWorkStub();
        var handler = CreateHandler(repository, unitOfWork);

        var result = await handler.Handle(
            new UpdateCommerceCommand(new UpdateCommerceRequest(
                commerceId,
                " Tienda Actualizada ",
                " Nueva descripción ",
                " nuevo@tienda.com ",
                " 8095559876 ",
                " 101888888 ")),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(commerceId, repository.ReceivedCommerceId);
        Assert.Equal(commerceId, repository.ReceivedEmailExclusion);
        Assert.Equal(commerceId, repository.ReceivedRncExclusion);
        Assert.Equal("Tienda Actualizada", commerce.Name);
        Assert.Equal("Nueva descripción", commerce.Description);
        Assert.Equal("nuevo@tienda.com", commerce.Email);
        Assert.Equal("8095559876", commerce.PhoneNumber);
        Assert.Equal("101888888", commerce.Rnc);
        Assert.Equal(CommerceStatus.Inactive, commerce.Status);
        Assert.Equal(1, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Handler_returns_not_found_without_committing()
    {
        var unitOfWork = new CommerceUnitOfWorkStub();
        var handler = CreateHandler(new CommerceRepositoryStub(), unitOfWork);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CommerceErrors.NotFound, result.Error);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Handler_rejects_duplicate_email_without_mutating_or_committing()
    {
        var commerce = CreateCommerce(CommerceStatus.Active);
        var repository = new CommerceRepositoryStub
        {
            CommerceForUpdate = commerce,
            EmailExistsResult = true
        };
        var unitOfWork = new CommerceUnitOfWorkStub();
        var handler = CreateHandler(repository, unitOfWork);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CommerceErrors.DuplicateEmail, result.Error);
        Assert.Equal("Nombre original", commerce.Name);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Handler_rejects_duplicate_rnc_without_mutating_or_committing()
    {
        var commerce = CreateCommerce(CommerceStatus.Active);
        var repository = new CommerceRepositoryStub
        {
            CommerceForUpdate = commerce,
            RncExistsResult = true
        };
        var unitOfWork = new CommerceUnitOfWorkStub();
        var handler = CreateHandler(repository, unitOfWork);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CommerceErrors.DuplicateRnc, result.Error);
        Assert.Equal("Nombre original", commerce.Name);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Handler_requires_authenticated_administrator()
    {
        var repository = new CommerceRepositoryStub
        {
            CommerceForUpdate = CreateCommerce(CommerceStatus.Active)
        };
        var unitOfWork = new CommerceUnitOfWorkStub();
        var handler = CreateHandler(
            repository,
            unitOfWork,
            CommerceCurrentUserStub.Client());

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CommerceErrors.AdministratorRequired, result.Error);
        Assert.Null(repository.ReceivedCommerceId);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Handler_propagates_concurrency_conflict_for_global_409_mapping()
    {
        var repository = new CommerceRepositoryStub
        {
            CommerceForUpdate = CreateCommerce(CommerceStatus.Active)
        };
        var unitOfWork = new CommerceUnitOfWorkStub
        {
            SaveException = new FinancialConcurrencyException(
                new InvalidOperationException("stale rowversion"))
        };
        var handler = CreateHandler(repository, unitOfWork);

        await Assert.ThrowsAsync<FinancialConcurrencyException>(
            () => handler.Handle(ValidCommand(), CancellationToken.None));

        Assert.Equal(1, unitOfWork.SaveCalls);
    }

    private static UpdateCommerceCommandHandler CreateHandler(
        CommerceRepositoryStub repository,
        CommerceUnitOfWorkStub unitOfWork,
        CommerceCurrentUserStub? currentUser = null) =>
        new(
            repository,
            unitOfWork,
            currentUser ?? CommerceCurrentUserStub.Administrator());

    private static UpdateCommerceCommand ValidCommand() =>
        new(new UpdateCommerceRequest(
            Guid.NewGuid(),
            "Nombre nuevo",
            null,
            "nuevo@tienda.com",
            "8095559876",
            "101888888"));

    private static CommerceEntity CreateCommerce(CommerceStatus status) => new()
    {
        Name = "Nombre original",
        Description = "Descripción original",
        Email = "original@tienda.com",
        PhoneNumber = "8095551234",
        Rnc = "101999999",
        Status = status
    };
}
