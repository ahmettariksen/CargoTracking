using CargoTracking.Application.Abstractions.Persistence;
using CargoTracking.Application.Abstractions.Persistence.Authentication;
using CargoTracking.Application.Interfaces;
using CargoTracking.Domain.Entities;
using MediatR;
using MediatR.Pipeline;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Application.Features.Auth.Commands.Login;
public sealed record LoginCommand(
     string Email,
     string Password) : IRequest<LoginResponse>;

//AccessToken: API isteklerinde kullanılacak kısa ömürlü JWT.
//ExpiresAt: Access Token'ın bitiş zamanı.
//RefreshToken: Yeni Access Token almak için kullanılacak token.
//RefreshTokenExpiresAt: Refresh Token'ın bitiş zamanı.
public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;

    public LoginCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher, IJwtTokenService jwtTokenService, IRefreshTokenService refreshTokenService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        //Kullanıcının e-posta ve şifresini doğruladık.
        //JWT servisinden Access Token ürettik.
        //Güvenli rastgele Refresh Token ürettik.
        //Refresh Token'ın SHA256 hash değerini hesapladık.
        //Hash'i RefreshTokens tablosuna kaydettik.
        //Gerçek Refresh Token'ı kullanıcıya döndürdük.
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .SingleOrDefaultAsync(
                u => u.Email == email && u.IsActive,
                cancellationToken);

        if (user is null ||
            !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException(
                "E-posta veya şifre hatalı.");
        }

        //Access Token oluştur.
        var accessToken = _jwtTokenService.GenerateToken(
            user.Id,
            user.Email,
            user.Role.ToString());

        // Refresh Token oluştur.
        var refreshToken = _refreshTokenService.GenerateToken();

        // Veritabanında saklanacak hash değerini hesapla.
        var refreshTokenHash =
            _refreshTokenService.HashToken(refreshToken);

        // Refresh Token 7 gün geçerli olacak.
        var refreshTokenExpiresAt = DateTime.Now.AddDays(7);

        var refreshTokenEntity = new RefreshToken(
           user.Id,
           refreshTokenHash,
           refreshTokenExpiresAt);

        _context.RefreshTokens.Add(refreshTokenEntity);

        await _context.SaveChangesAsync(cancellationToken);

        return new LoginResponse(
            AccessToken: accessToken.Token,
            ExpiresAt: accessToken.ExpiresAt,
            RefreshToken: refreshToken,
            RefreshTokenExpiresAt: refreshTokenExpiresAt);
    }
}
