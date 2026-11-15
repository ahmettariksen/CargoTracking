using CargoTracking.Application.Features.Auth.Commands.Register;
using CargoTracking.Domain.Entities;
using CargoTracking.Domain.Enums;
using CargoTracking.Tests.Fakes;
using CargoTracking.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Tests.Features.Auth.Register;
public sealed class RegisterCommandHandlerTests
{
    //Başarılı kayıt
    [Fact]
    public async Task Handle_ValidCommand_ShouldRegisterUser()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var passwordHasher = new FakePasswordHasher();

        var handler = new RegisterCommandHandler(
            context,
            passwordHasher);

        var command = new RegisterCommand(
            "Ahmet",
            "Yılmaz",
            "ahmet@test.com",
            "password123",
            "05551111111");

        // Act
        var result = await handler.Handle(
            command,
            CancellationToken.None);

        // Assert
        var user = await context.Users.FindAsync(result);

        Assert.NotEqual(Guid.Empty, result);
        Assert.NotNull(user);
        Assert.Equal("Ahmet", user.FirstName);
        Assert.Equal("Yılmaz", user.LastName);
        Assert.Equal("ahmet@test.com", user.Email);
        Assert.Equal("HASHED_password123", user.PasswordHash);
        Assert.Equal(UserRole.Customer, user.Role);
        Assert.True(user.IsActive);
    }
    //Aynı e-posta adresiyle ikinci kez kayıt yapılması engellenmeli.
    [Fact]
    public async Task Handle_EmailAlreadyExists_ShouldThrowException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var existingUser = new User(
            "Ahmet",
            "Yılmaz",
            "ahmet@test.com",
            "HASHED_password123",
            "05551111111",
            UserRole.Customer);

        context.Users.Add(existingUser);
        await context.SaveChangesAsync();

        var passwordHasher = new FakePasswordHasher();

        var handler = new RegisterCommandHandler(
            context,
            passwordHasher);

        var command = new RegisterCommand(
            "Mehmet",
            "Demir",
            "ahmet@test.com",
            "password456",
            "05552222222");

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Equal(
            "This email address is already registered.",
            exception.Message);

        Assert.Single(context.Users);
    }
    //Kayıt sırasında başında/sonunda boşluk bulunan ve büyük harfli e-posta gönderildiğinde veritabanına küçük harfli ve boşluksuz kaydedilmesini doğrulayacağız.
    [Fact]
    public async Task Handle_EmailWithWhitespaceAndUppercase_ShouldNormalizeEmail()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var passwordHasher = new FakePasswordHasher();

        var handler = new RegisterCommandHandler(
            context,
            passwordHasher);

        var command = new RegisterCommand(
            "Ahmet",
            "Yılmaz",
            "  AHMET@TEST.COM  ",
            "password123",
            "05551111111");

        // Act
        var result = await handler.Handle(
            command,
            CancellationToken.None);

        // Assert
        var user = await context.Users.FindAsync(result);

        Assert.NotNull(user);
        Assert.Equal("ahmet@test.com", user.Email);
    }
    //Ad, soyad ve telefon numarasındaki boşluklar temizlenmeli.
    [Fact]
    public async Task Handle_ValidCommand_ShouldTrimUserInformation()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var passwordHasher = new FakePasswordHasher();

        var handler = new RegisterCommandHandler(
            context,
            passwordHasher);

        var command = new RegisterCommand(
            " Ahmet ",
            " Yılmaz ",
            " ahmet@test.com ",
            "password123",
            " 05551111111 ");

        // Act
        var result = await handler.Handle(
            command,
            CancellationToken.None);

        // Assert
        var user = await context.Users.FindAsync(result);

        Assert.NotNull(user);
        Assert.Equal("Ahmet", user.FirstName);
        Assert.Equal("Yılmaz", user.LastName);
        Assert.Equal("05551111111", user.PhoneNumber);
    }
    //Kayıt sırasında şifre hash işleminin doğru kullanıldığını doğrulayalım.
    [Fact]
    public async Task Handle_ShouldHashPasswordBeforeSavingUser()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var passwordHasher = new FakePasswordHasher();

        var handler = new RegisterCommandHandler(
            context,
            passwordHasher);

        var command = new RegisterCommand(
            "Ahmet",
            "Yılmaz",
            "ahmet@test.com",
            "password123",
            "05551111111");

        // Act
        var result = await handler.Handle(
            command,
            CancellationToken.None);

        // Assert
        var user = await context.Users.FindAsync(result);

        Assert.NotNull(user);
        Assert.Equal("HASHED_password123", user.PasswordHash);
        Assert.NotEqual("password123", user.PasswordHash);
        Assert.Equal(user.PasswordHash, passwordHasher.HashedPassword);
    }
}
