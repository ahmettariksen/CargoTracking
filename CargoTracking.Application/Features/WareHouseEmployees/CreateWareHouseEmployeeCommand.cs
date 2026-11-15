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

namespace CargoTracking.Application.Features.WareHouseEmployees;
public sealed record CreateWareHouseEmployeeCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string PhoneNumber,
    Guid WarehouseId) :IRequest<Guid>;

public sealed class CreateWareHouseEmployeeCommandHandler(
    IApplicationDbContext _context,
    IPasswordHasher _passwordHasher) : IRequestHandler<CreateWareHouseEmployeeCommand, Guid>
{
    public async Task<Guid> Handle(CreateWareHouseEmployeeCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var emailExists = await _context.Users
            .AnyAsync(x => x.Email == email, cancellationToken);

        if (emailExists)
        {
            throw new InvalidOperationException(
                "This email address is already registered.");
        }
        var warehouseExists = await _context.Warehouses
           .AnyAsync(
               x => x.Id == request.WarehouseId,
               cancellationToken);

        if (!warehouseExists)
        {
            throw new InvalidOperationException(
                "Warehouse does not exist.");
        }

        var passwordHash = _passwordHasher.Hash(request.Password);

        var user = new User(
            request.FirstName.Trim(),
            request.LastName.Trim(),
            email,
            passwordHash,
            request.PhoneNumber.Trim(),
            UserRole.WarehouseEmployee);

        var warehouseEmployee = new WareHouseEmployeeProfile(user.Id,request.WarehouseId);

        await _context.Users.AddAsync(user, cancellationToken);
        await _context.WareHouseEmployeeProfiles.AddAsync(warehouseEmployee,cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return warehouseEmployee.Id;
    }
}
