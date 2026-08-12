using ABP.Application.Common.DTOs.Users;
using ABP.Application.Mappings;
using ABP.Domain.Entities;
using ABP.Domain.Enums;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;

namespace ABP.Application.UnitTests.Mappings;

public sealed class UserProfileTests
{
    private readonly IMapper _mapper = new MapperConfiguration(
        configuration => configuration.AddProfile<UserProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public void Create_mapping_preserves_identity_and_maps_the_requested_role()
    {
        var commerceId = Guid.NewGuid();
        var destination = new User("user-1");
        var source = new CreateUserDto
        {
            FirstName = "Ana",
            LastName = "Pérez",
            Identification = "00100000001",
            Email = "ana@example.test",
            UserName = "ana.perez",
            Password = "Password1!",
            ConfirmPassword = "Password1!",
            Role = Roles.Commerce.ToString(),
            CommerceId = commerceId
        };

        _mapper.Map(source, destination);

        Assert.Equal("user-1", destination.Id);
        Assert.Equal("Ana", destination.Name);
        Assert.Equal(Roles.Commerce, destination.Role);
        Assert.Equal(commerceId, destination.CommerceId);
    }

    [Fact]
    public void Edit_mapping_cannot_change_role_status_or_commerce_association()
    {
        var commerceId = Guid.NewGuid();
        var destination = new User("user-1")
        {
            Name = "Antes",
            LastName = "Usuario",
            Identification = "00100000001",
            Email = "antes@example.test",
            UserName = "antes",
            Role = Roles.Commerce,
            IsActive = true,
            CommerceId = commerceId
        };
        var source = new EditUserDto
        {
            Id = "otro-id",
            FirstName = "Después",
            LastName = "Usuario",
            Identification = "00100000001",
            Email = "despues@example.test",
            UserName = "despues",
            Role = Roles.Administrator.ToString()
        };

        _mapper.Map(source, destination);

        Assert.Equal("user-1", destination.Id);
        Assert.Equal("Después", destination.Name);
        Assert.Equal(Roles.Commerce, destination.Role);
        Assert.True(destination.IsActive);
        Assert.Equal(commerceId, destination.CommerceId);
    }
}
