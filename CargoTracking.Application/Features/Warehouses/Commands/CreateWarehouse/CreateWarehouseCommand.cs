using CargoTracking.Application.Abstractions.Persistence;
using CargoTracking.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Application.Features.Warehouses.Commands.CreateWarehouse;
public sealed record CreateWarehouseCommand(
    string Name,
    string City,
    string District,
    string Address,
    double Latitude,
    double Longitude) : IRequest<Guid>;
public sealed class CreateWarehouseCommandHandler : IRequestHandler<CreateWarehouseCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateWarehouseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var warehouse = new Warehouse(
            request.Name,
            request.City,
            request.District,
            request.Address,
            request.Latitude,
            request.Longitude);

        await _context.Warehouses.AddAsync(
            warehouse,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return warehouse.Id;
    }
}
