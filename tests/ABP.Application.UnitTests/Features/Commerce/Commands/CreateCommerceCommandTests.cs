using ABP.Application.Features.Commerce;
using ABP.Application.Features.Commerce.Commands.CreateCommerce;
using ABP.Application.Features.Commerce.DTOs;
using ABP.Application.Features.Commerce.Validation;
using ABP.Domain.Enums;

namespace ABP.Application.UnitTests.Features.Commerce.Commands;

public sealed class CreateCommerceCommandTests
{
    [Fact]
    public async Task Validator_reuses_shared_create_rules()
    {
        var validator = new CreateCommerceCommandValidator(
            new CreateCommerceRequestValidator());
        var command = new CreateCommerceCommand(
            new CreateCommerceRequest("", null, "invalid", "", ""));

        var result = await validator.ValidateAsync(command);

        Assert.Contains(result.Errors, error => error.PropertyName == "Request.Name");
        Assert.Contains(result.Errors, error => error.PropertyName == "Request.Email");
        Assert.Contains(result.Errors, error => error.PropertyName == "Request.PhoneNumber");
        Assert.Contains(result.Errors, error => error.PropertyName == "Request.Rnc");
    }

    [Fact]
    public async Task Handler_creates_active_normalized_commerce_and_commits_once()
    {
        var repository = new CommerceRepositoryStub();
        var unitOfWork = new CommerceUnitOfWorkStub();
        var handler = CreateHandler(repository, unitOfWork);

        var result = await handler.Handle(
            new CreateCommerceCommand(new CreateCommerceRequest(
                " Tienda Demo ",
                "   ",
                " contacto@tiendademo.com ",
                " 8095551234 ",
                " 101999999 ")),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var commerce = Assert.IsType<ABP.Domain.Entities.Commerce.Commerce>(
            repository.AddedCommerce);
        Assert.Equal(commerce.Id, result.Value);
        Assert.Equal("Tienda Demo", commerce.Name);
        Assert.Null(commerce.Description);
        Assert.Equal("contacto@tiendademo.com", commerce.Email);
        Assert.Equal("8095551234", commerce.PhoneNumber);
        Assert.Equal("101999999", commerce.Rnc);
        Assert.Equal(CommerceStatus.Active, commerce.Status);
        Assert.Equal(1, repository.AddCalls);
        Assert.Equal(1, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Handler_rejects_duplicate_email_without_adding_or_committing()
    {
        var repository = new CommerceRepositoryStub { EmailExistsResult = true };
        var unitOfWork = new CommerceUnitOfWorkStub();
        var handler = CreateHandler(repository, unitOfWork);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CommerceErrors.DuplicateEmail, result.Error);
        Assert.Equal("contacto@tiendademo.com", repository.ReceivedEmail);
        Assert.Equal(0, repository.RncExistsCalls);
        Assert.Equal(0, repository.AddCalls);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Handler_rejects_duplicate_rnc_without_adding_or_committing()
    {
        var repository = new CommerceRepositoryStub { RncExistsResult = true };
        var unitOfWork = new CommerceUnitOfWorkStub();
        var handler = CreateHandler(repository, unitOfWork);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CommerceErrors.DuplicateRnc, result.Error);
        Assert.Equal("101999999", repository.ReceivedRnc);
        Assert.Equal(0, repository.AddCalls);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Handler_requires_authenticated_administrator()
    {
        var repository = new CommerceRepositoryStub();
        var unitOfWork = new CommerceUnitOfWorkStub();
        var handler = CreateHandler(
            repository,
            unitOfWork,
            CommerceCurrentUserStub.Client());

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CommerceErrors.AdministratorRequired, result.Error);
        Assert.Equal(0, repository.EmailExistsCalls);
        Assert.Equal(0, repository.AddCalls);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    private static CreateCommerceCommandHandler CreateHandler(
        CommerceRepositoryStub repository,
        CommerceUnitOfWorkStub unitOfWork,
        CommerceCurrentUserStub? currentUser = null) =>
        new(
            repository,
            unitOfWork,
            currentUser ?? CommerceCurrentUserStub.Administrator());

    private static CreateCommerceCommand ValidCommand() =>
        new(new CreateCommerceRequest(
            "Tienda Demo",
            null,
            "contacto@tiendademo.com",
            "8095551234",
            "101999999"));
}
