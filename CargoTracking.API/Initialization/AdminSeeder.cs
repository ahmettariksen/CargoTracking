using CargoTracking.API.Configuration;
using CargoTracking.Application.Abstractions.Persistence;
using CargoTracking.Application.Abstractions.Persistence.Authentication;
using CargoTracking.Domain.Entities;
using CargoTracking.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CargoTracking.API.Initialization;

public sealed class AdminSeeder
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly AdminOptions _adminOptions;

    public AdminSeeder(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IOptions<AdminOptions> adminOptions)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _adminOptions = adminOptions.Value;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var email = _adminOptions.Email.Trim().ToLowerInvariant();

        var adminExists = await _context.Users
            .AnyAsync(x => x.Role == UserRole.Admin, cancellationToken);

        if (adminExists)
            return;

        var emailExists = await _context.Users
            .AnyAsync(x => x.Email == email, cancellationToken);

        if (emailExists)
        {
            throw new InvalidOperationException(
                "The initial admin email is already used by another user.");
        }

        var passwordHash = _passwordHasher.Hash(_adminOptions.Password);

        var admin = new User(
            _adminOptions.FirstName.Trim(),
            _adminOptions.LastName.Trim(),
            email,
            passwordHash,
            _adminOptions.PhoneNumber.Trim(),
            UserRole.Admin);

        await _context.Users.AddAsync(admin, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
