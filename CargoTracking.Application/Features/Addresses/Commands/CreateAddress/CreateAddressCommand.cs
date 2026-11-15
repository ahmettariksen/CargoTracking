using CargoTracking.Application.Abstractions.Persistence;
using CargoTracking.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Application.Features.Addresses.Commands.CreateAddress;
public sealed record CreateAddressCommand(
    Guid UserId,
    string Title,
    string City,
    string District,
    string Neighborhood,
    string FullAddress,
    double Latitude,
    double Longitude) : IRequest<Guid>;

public sealed class CreateAddressCommandHandler : IRequestHandler<CreateAddressCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateAddressCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateAddressCommand request, CancellationToken cancellationToken)
    {
        var userExists = await _context.Users.AnyAsync(x => x.Id == request.UserId && x.IsActive, cancellationToken);

        if (!userExists)
        {
            throw new InvalidOperationException("User does not exist or is inactive.");
        }
        if (request.Latitude < -90 || request.Latitude > 90)
        {
            throw new InvalidOperationException("Latitude must be between -90 and 90.");
        }
        if (request.Longitude < -180 || request.Longitude > 180)
        {
            throw new InvalidOperationException("Longitude must be between -180 and 180.");
        }

        var address = new Address(
            request.UserId,
            request.Title.Trim(),
            request.City.Trim(),
            request.District.Trim(),
            request.Neighborhood.Trim(),
            request.FullAddress.Trim(),
            request.Latitude,
            request.Longitude);

        await _context.Addresses.AddAsync(address, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return address.Id;
    }
}

