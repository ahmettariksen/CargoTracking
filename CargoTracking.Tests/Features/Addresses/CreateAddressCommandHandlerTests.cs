using CargoTracking.Application.Features.Addresses.Commands.CreateAddress;
using CargoTracking.Domain.Entities;
using CargoTracking.Domain.Enums;
using CargoTracking.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Tests.Features.Addresses;
public sealed class CreateAddressCommandHandlerTests
{
    // geçerli adres oluşturma testi
    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateAddress()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var user = new User(
            "Ahmet",
            "Yılmaz",
            "ahmet@test.com",
            "hashedPassword",
            "05551111111",
            UserRole.Customer);

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new CreateAddressCommandHandler(context);

        var command = new CreateAddressCommand(
            user.Id,
            "Ev",
            "Düzce",
            "Merkez",
            "Aziziye",
            "Örnek Mahalle, Örnek Sokak No: 10",
            40.8438,
            31.1565);

        // Act
        var result = await handler.Handle(
            command,
            CancellationToken.None);

        // Assert
        var address = await context.Addresses.FindAsync(result);

        Assert.NotEqual(Guid.Empty, result);
        Assert.NotNull(address);
        Assert.Equal(user.Id, address.UserId);
        Assert.Equal("Ev", address.Title);
        Assert.Equal("Düzce", address.City);
        Assert.Equal(40.8438, address.Latitude);
        Assert.Equal(31.1565, address.Longitude);
    }

    // Kullanıcı veritabanında yoksa adres oluşturulmamalı testi
    [Fact]
    public async Task Handle_UserDoesNotExist_ShouldThrowException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var handler = new CreateAddressCommandHandler(context);

        var command = new CreateAddressCommand(
            Guid.NewGuid(),
            "Ev",
            "Düzce",
            "Merkez",
            "Aziziye",
            "Örnek adres",
            40.8438,
            31.1565);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Equal("User does not exist or is inactive.", exception.Message);
        Assert.Empty(context.Addresses);
    }

    // kullanıcı mevcut ama aktif değil.
    [Fact]
    public async Task Handle_InactiveUser_ShouldThrowException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var user = new User(
            "Ahmet",
            "Yılmaz",
            "ahmet@test.com",
            "hashedPassword",
            "05551111111",
            UserRole.Customer);

        user.Deactivate();

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new CreateAddressCommandHandler(context);

        var command = new CreateAddressCommand(
            user.Id,
            "Ev",
            "Düzce",
            "Merkez",
            "Aziziye",
            "Örnek adres",
            40.8438,
            31.1565);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Equal("User does not exist or is inactive.", exception.Message);
        Assert.Empty(context.Addresses);
    }

    //Enlem (Latitude) değerinin -90 ile 90 arasında olmadığı durumda hata fırlatılması.
    [Fact]
    public async Task Handle_InvalidLatitude_ShouldThrowException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var user = new User(
            "Ahmet",
            "Yılmaz",
            "ahmet@test.com",
            "hashedPassword",
            "05551111111",
            UserRole.Customer);

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new CreateAddressCommandHandler(context);

        var command = new CreateAddressCommand(
            user.Id,
            "Ev",
            "Düzce",
            "Merkez",
            "Aziziye",
            "Örnek adres",
            91,
            31.1565);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Equal("Latitude must be between -90 and 90.", exception.Message);
        Assert.Empty(context.Addresses);
    }
    //Bu testte adres alanlarına başında ve sonunda boşluk bulunan değerler göndereceğiz. Trim sayesinde bu boşluklar silinecek 
    [Fact]
    public async Task Handle_ValidCommand_ShouldTrimAddressFields()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var user = new User(
            "Ahmet",
            "Yılmaz",
            "ahmet@test.com",
            "hashedPassword",
            "05551111111",
            UserRole.Customer);

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new CreateAddressCommandHandler(context);

        var command = new CreateAddressCommand(
            user.Id,
            " Ev ",
            " Düzce ",
            " Merkez ",
            " Aziziye ",
            " Örnek adres ",
            40.8438,
            31.1565);

        // Act
        var result = await handler.Handle(
            command,
            CancellationToken.None);

        // Assert
        var address = await context.Addresses.FindAsync(result);

        Assert.NotNull(address);
        Assert.Equal("Ev", address.Title);
        Assert.Equal("Düzce", address.City);
        Assert.Equal("Merkez", address.District);
        Assert.Equal("Aziziye", address.Neighborhood);
        Assert.Equal("Örnek adres", address.FullAddress);
    }
}
