using CargoTracking.Application.Abstractions.Persistence;
using CargoTracking.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Application.Features.Auth.Commands.Login;

//Kullanıcı çıkış yaptığında Refresh Token veritabanında iptal edilecek.
public sealed record LogoutCommand(
    string RefreshToken) : IRequest;

public sealed class LogoutCommandHandler
    : IRequestHandler<LogoutCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IRefreshTokenService _refreshTokenService;

    public LogoutCommandHandler(
        IApplicationDbContext context,
        IRefreshTokenService refreshTokenService)
    {
        _context = context;
        _refreshTokenService = refreshTokenService;
    }

    public async Task Handle(
        LogoutCommand request,
        CancellationToken cancellationToken)
    {
        var tokenHash = _refreshTokenService.HashToken(
            request.RefreshToken);

        var refreshToken = await _context.RefreshTokens
            .SingleOrDefaultAsync(
                x => x.TokenHash == tokenHash,
                cancellationToken);

        if (refreshToken is null || !refreshToken.IsActive)
        {
            throw new UnauthorizedAccessException(
                "Refresh token geçersiz veya zaten iptal edilmiş.");
        }

        refreshToken.Revoke();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
