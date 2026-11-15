using CargoTracking.Application.Features.Couriers.Commands.CreateCourier;
using CargoTracking.Domain.Entities;
using CargoTracking.Domain.Enums;
using CargoTracking.Tests.Fakes;
using CargoTracking.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Tests.Features.Couriers;
public sealed class CreateCourierCommandHandlerTests
{
    // başarılı kurye oluşturma testi.
    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateCourierAndUser()
    {
        await using var context = TestDbContextFactory.Create();

        var passwordHasher = new FakePasswordHasher();

        var command = new CreateCourierCommand(
            "Ahmet",
            "Yılmaz",
            "ahmet@example.com",
            "StrongPass123!",
            "05551234567",
            VehicleType.Motorcycle,
            "81 ABC 123");

        var handler = new CreateCourierCommandHandler(
            context,
            passwordHasher);

        var courierId = await handler.Handle(
            command,
            CancellationToken.None);

        var courier = await context.CourierProfiles
            .SingleOrDefaultAsync(x => x.Id == courierId);

        Assert.NotNull(courier);
        Assert.Equal(VehicleType.Motorcycle, courier.VehicleType);
        Assert.Equal("81 ABC 123", courier.VehiclePlate);
        Assert.True(courier.IsAvailable);

        var user = await context.Users
            .SingleOrDefaultAsync(x => x.Id == courier.UserId);

        Assert.NotNull(user);
        Assert.Equal(UserRole.Courier, user.Role);
        Assert.Equal("ahmet@example.com", user.Email);
        Assert.True(user.IsActive);
        Assert.Equal("HASHED_StrongPass123!", user.PasswordHash);
    }
    //Aynı e-posta ile ikinci kez kurye kaydı yapılamamalı.
    [Fact]
    public async Task Handle_DuplicateEmail_ShouldThrowInvalidOperationException()
    {
        await using var context = TestDbContextFactory.Create();

        var passwordHasher = new FakePasswordHasher();

        var existingUser = new User(
            "Ahmet",
            "Yılmaz",
            "ahmet@example.com",
            "EXISTING_HASH",
            "05551234567",
            UserRole.Customer);

        await context.Users.AddAsync(existingUser);
        await context.SaveChangesAsync();

        var command = new CreateCourierCommand(
            "Mehmet",
            "Demir",
            " AHmet@example.com ",
            "StrongPass123!",
            "05559876543",
            VehicleType.Motorcycle,
            "81 DEF 456");

        var handler = new CreateCourierCommandHandler(
            context,
            passwordHasher);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Equal(
            "This email address is already registered.",
            exception.Message);

        Assert.Equal(1, await context.Users.CountAsync());
        Assert.Empty(await context.CourierProfiles.ToListAsync());
    }
    //Büyük harfli ve boşluklu e-posta gönderildiğinde kullanıcıya küçük harfli, boşluksuz e-posta kaydedilmeli.
    [Fact]
    public async Task Handle_EmailWithWhitespaceAndUppercase_ShouldNormalizeEmail()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var handler = new CreateCourierCommandHandler(
            context,
            new FakePasswordHasher());

        var command = new CreateCourierCommand(
            "Ahmet",
            "Yılmaz",
            "  AHMET@EXAMPLE.COM  ",
            "StrongPass123!",
            "05551234567",
            VehicleType.Motorcycle,
            "81 ABC 123");

        // Act
        var courierId = await handler.Handle(
            command,
            CancellationToken.None);

        // Assert
        var courier = await context.CourierProfiles
            .SingleAsync(x => x.Id == courierId);

        var user = await context.Users
            .SingleAsync(x => x.Id == courier.UserId);

        Assert.Equal("ahmet@example.com", user.Email);
    }
}
