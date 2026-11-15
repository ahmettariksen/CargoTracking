using CargoTracking.Application.Abstractions.Persistence;
using CargoTracking.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Application.Features.Couriers.Commands.UpdateLocation;
public sealed record UpdateCourierLocationCommand(
    double Latitude,
    double Longitude) : IRequest;
public sealed class UpdateCourierLocationCommandHandler : IRequestHandler<UpdateCourierLocationCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICourierLocationStore _courierLocationStore;

    public UpdateCourierLocationCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, ICourierLocationStore courierLocationStore)
    {
        _context = context;
        _currentUserService = currentUserService;
        _courierLocationStore = courierLocationStore;
    }

    public async Task Handle(UpdateCourierLocationCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        if (userId is null)
        {
            throw new UnauthorizedAccessException(
                "Authenticated user could not be identified.");
        }

        var courierProfile = await _context.CourierProfiles
            .FirstOrDefaultAsync(
                x => x.UserId == userId.Value,
                cancellationToken);

        if (courierProfile is null)
        {
            throw new InvalidOperationException(
                "Courier profile not found.");
        }

        courierProfile.UpdateLocation(
            request.Latitude,
            request.Longitude);

        await _context.SaveChangesAsync(cancellationToken);

        await _courierLocationStore.UpdateCourierLocationAsync(
    courierProfile.Id,
    request.Latitude,
    request.Longitude);
    }
}
