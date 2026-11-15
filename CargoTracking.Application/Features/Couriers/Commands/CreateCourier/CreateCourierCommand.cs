using CargoTracking.Application.Abstractions.Persistence;
using CargoTracking.Application.Abstractions.Persistence.Authentication;
using CargoTracking.Domain.Entities;
using CargoTracking.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Application.Features.Couriers.Commands.CreateCourier;
public sealed record CreateCourierCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string PhoneNumber,
    VehicleType VehicleType,
    string VehiclePlate) : IRequest<Guid>;

public sealed class CreateCourierCommandHandler : IRequestHandler<CreateCourierCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public CreateCourierCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<Guid> Handle(
        CreateCourierCommand request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var emailExists = await _context.Users
            .AnyAsync(x => x.Email == email, cancellationToken);

        if (emailExists)
        {
            throw new InvalidOperationException(
                "This email address is already registered.");
        }

        var passwordHash = _passwordHasher.Hash(request.Password);

        var user = new User(
            request.FirstName.Trim(),
            request.LastName.Trim(),
            email,
            passwordHash,
            request.PhoneNumber.Trim(),
            UserRole.Courier);

        var courierProfile = new CourierProfile(
            user.Id,
            request.VehicleType,
            request.VehiclePlate.Trim());

        await _context.Users.AddAsync(user, cancellationToken);
        await _context.CourierProfiles.AddAsync(
            courierProfile, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return courierProfile.Id;
    }
}
