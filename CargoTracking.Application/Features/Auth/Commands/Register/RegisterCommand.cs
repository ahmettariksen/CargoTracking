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

namespace CargoTracking.Application.Features.Auth.Commands.Register;
public sealed record RegisterCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string PhoneNumber) : IRequest<Guid>;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<Guid> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // E - posta adresinin başındaki ve sonundaki boşlukları temizliyor, küçük harfe dönüştürüyor.
        //Aynı e-posta adresiyle kayıt olup olmadığını kontrol ediyor.
        //Parolayı IPasswordHasher üzerinden hash'liyor.
        //Kullanıcıyı varsayılan Customer rolüyle oluşturuyor.
        //Kullanıcıyı SQL Server'a kaydediyor ve kimliğini döndürüyor.

        var email = request.Email.Trim().ToLowerInvariant();
        var emailExists = await _context.Users.AnyAsync(x => x.Email == email, cancellationToken);
        if (emailExists)
        {
            throw new InvalidOperationException("This email address is already registered.");
        }
        var passwordHash = _passwordHasher.Hash(request.Password);

        var user = new User(
        request.FirstName.Trim(),
        request.LastName.Trim(),
        email,
        passwordHash,
        request.PhoneNumber.Trim(),
        UserRole.Customer);

        await _context.Users.AddAsync(user, cancellationToken);

        await _context.SaveChangesAsync();

        return user.Id;
    }
}


