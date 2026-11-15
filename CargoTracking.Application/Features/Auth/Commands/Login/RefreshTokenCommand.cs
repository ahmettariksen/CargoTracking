using CargoTracking.Application.Abstractions.Persistence;
using CargoTracking.Application.Interfaces;
using CargoTracking.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Application.Features.Auth.Commands.Login;
public sealed record RefreshTokenCommand(
    string RefreshToken) : IRequest<LoginResponse>;


//Gelen Refresh Token'ın hash'ini hesaplıyor.
//Hash üzerinden veritabanında token'ı arıyor.
//Token'ın aktif ve süresinin geçmemiş olduğunu kontrol ediyor.
//Kullanıcının hâlâ aktif olduğunu doğruluyor.
//Yeni Access Token ve Refresh Token üretiyor.
//Eski token'ı iptal ediyor.
//Yeni token'ın hash'ini veritabanına kaydediyor.
public sealed class RefreshTokenCommandHandler
    : IRequestHandler<RefreshTokenCommand, LoginResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;

    public RefreshTokenCommandHandler(
        IApplicationDbContext context,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<LoginResponse> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        // İstemcinin gönderdiği token'ın hash değerini hesapla.
        var tokenHash = _refreshTokenService.HashToken(
            request.RefreshToken);

        // Veritabanında karşılığını bul.
        var existingToken = await _context.RefreshTokens
            .SingleOrDefaultAsync(
                x => x.TokenHash == tokenHash,
                cancellationToken);

        if (existingToken is null || !existingToken.IsActive)
        {
            throw new UnauthorizedAccessException(
                "Refresh token geçersiz veya süresi dolmuş.");
        }

        // Token'ın sahibi olan kullanıcıyı bul.
        var user = await _context.Users
            .SingleOrDefaultAsync(
                x => x.Id == existingToken.UserId && x.IsActive,
                cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedAccessException(
                "Kullanıcı bulunamadı veya hesabı pasif.");
        }

        // Yeni Access Token üret.
        var accessTokenResult = _jwtTokenService.GenerateToken(
            user.Id,
            user.Email,
            user.Role.ToString());

        // Yeni Refresh Token üret.
        var newRefreshToken = _refreshTokenService.GenerateToken();

        var newRefreshTokenHash =
            _refreshTokenService.HashToken(newRefreshToken);

        var newRefreshTokenExpiresAt = DateTime.Now.AddDays(7);

        // Eski token'ı iptal et ve yenisiyle ilişkilendir.
        existingToken.Revoke(newRefreshTokenHash);

        // Yeni token'ın hash değerini veritabanına ekle.
        var newTokenEntity = new RefreshToken(
            user.Id,
            newRefreshTokenHash,
            newRefreshTokenExpiresAt);

        _context.RefreshTokens.Add(newTokenEntity);

        await _context.SaveChangesAsync(cancellationToken);

        return new LoginResponse(
            AccessToken: accessTokenResult.Token,
            ExpiresAt: accessTokenResult.ExpiresAt,
            RefreshToken: newRefreshToken,
            RefreshTokenExpiresAt: newRefreshTokenExpiresAt);
    }
}
