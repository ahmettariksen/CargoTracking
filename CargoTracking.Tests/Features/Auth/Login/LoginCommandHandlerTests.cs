using CargoTracking.Application.Features.Auth.Commands.Login;
using CargoTracking.Domain.Entities;
using CargoTracking.Domain.Enums;
using CargoTracking.Infrastructure.Services.Authentication;
using CargoTracking.Tests.Fakes;
using CargoTracking.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Tests.Features.Login;
public sealed class LoginCommandHandlerTests
{
    // başarılı kullanıcı girişi
    [Fact]
    public async Task Handle_ValidCredentials_ShouldReturnLoginResponse()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var user = new User(
            "Ahmet",
            "Yılmaz",
            "ahmet@test.com",
            "HASHED_password123",
            "05551111111",
            UserRole.Customer);

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var passwordHasher = new FakePasswordHasher();
        var jwtTokenService = new FakeJwtTokenService();
        var refreshTokenService = new FakeRefreshTokenService();

        var handler = new LoginCommandHandler(
            context,
            passwordHasher,
            jwtTokenService,
            refreshTokenService);

        var command = new LoginCommand(
            "ahmet@test.com",
            "password123");

        // Act
        var response = await handler.Handle(
            command,
            CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("fake_access_token", response.AccessToken);
        Assert.True(response.ExpiresAt > DateTime.Now);
        Assert.True(response.ExpiresAt <= DateTime.Now.AddMinutes(30));
    }
    // Kullanıcı veritabanında bulunamadığında giriş reddedilmeli.
    [Fact]
    public async Task Handle_UserDoesNotExist_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var passwordHasher = new FakePasswordHasher();
        var jwtTokenService = new FakeJwtTokenService();

        var refreshTokenService = new FakeRefreshTokenService();

        var handler = new LoginCommandHandler(
            context,
            passwordHasher,
            jwtTokenService,
            refreshTokenService);

        var command = new LoginCommand(
            "unknown@test.com",
            "password123");

        // Act
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Equal("E-posta veya şifre hatalı.", exception.Message);
    }
    // Yanlış şifreyle giriş yapılmaya çalışılması.
    [Fact]
    public async Task Handle_WrongPassword_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var user = new User(
            "Ahmet",
            "Yılmaz",
            "ahmet@test.com",
            "HASHED_correctPassword",
            "05551111111",
            UserRole.Customer);

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var passwordHasher = new FakePasswordHasher();
        var jwtTokenService = new FakeJwtTokenService();

        var refreshTokenService = new FakeRefreshTokenService();

        var handler = new LoginCommandHandler(
            context,
            passwordHasher,
            jwtTokenService,
            refreshTokenService);

        var command = new LoginCommand(
            "ahmet@test.com",
            "wrongPassword");

        // Act
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Equal("E-posta veya şifre hatalı.", exception.Message);
    }
    //Pasif kullanıcı giriş yapamamalı.
    [Fact]
    public async Task Handle_InactiveUser_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var user = new User(
            "Ahmet",
            "Yılmaz",
            "ahmet@test.com",
            "HASHED_password123",
            "05551111111",
            UserRole.Customer);

        user.Deactivate();

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var passwordHasher = new FakePasswordHasher();
        var jwtTokenService = new FakeJwtTokenService();
        var refreshTokenService = new FakeRefreshTokenService();

        var handler = new LoginCommandHandler(
            context,
            passwordHasher,
            jwtTokenService,
            refreshTokenService);

        var command = new LoginCommand(
            "ahmet@test.com",
            "password123");

        // Act
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Equal("E-posta veya şifre hatalı.", exception.Message);
    }
    //başında/sonunda boşluk ve büyük harf bulunan e-posta ile giriş yapılabilmeli.
    [Fact]
    public async Task Handle_EmailWithWhitespaceAndUppercase_ShouldLoginSuccessfully()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var user = new User(
            "Ahmet",
            "Yılmaz",
            "ahmet@test.com",
            "HASHED_password123",
            "05551111111",
            UserRole.Customer);

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var passwordHasher = new FakePasswordHasher();
        var jwtTokenService = new FakeJwtTokenService();
        var refreshTokenService = new FakeRefreshTokenService();

        var handler = new LoginCommandHandler(
            context,
            passwordHasher,
            jwtTokenService,
            refreshTokenService);

        var command = new LoginCommand(
            "  AHMET@TEST.COM  ",
            "password123");

        // Act
        var response = await handler.Handle(
            command,
            CancellationToken.None);

        // Assert
        Assert.Equal("fake_access_token", response.AccessToken);
    }
    //JWT oluşturulurken doğru kullanıcı bilgilerinin token servisine gönderilmesi.
    [Fact]
    public async Task Handle_ValidCredentials_ShouldGenerateTokenWithCorrectUserInformation()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var user = new User(
            "Ahmet",
            "Yılmaz",
            "ahmet@test.com",
            "HASHED_password123",
            "05551111111",
            UserRole.Customer);

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var passwordHasher = new FakePasswordHasher();
        var jwtTokenService = new FakeJwtTokenService();
        var refreshTokenService = new FakeRefreshTokenService();

        var handler = new LoginCommandHandler(
            context,
            passwordHasher,
            jwtTokenService,
            refreshTokenService);

        var command = new LoginCommand(
            "ahmet@test.com",
            "password123");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(user.Id, jwtTokenService.UserId);
        Assert.Equal(user.Email, jwtTokenService.Email);
        Assert.Equal(user.Role.ToString(), jwtTokenService.Role);
    }
}
