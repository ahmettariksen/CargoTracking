using CargoTracking.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Tests.Fakes;
public sealed class FakeJwtTokenService : IJwtTokenService
{
    public Guid? UserId { get; private set; }
    public string? Email { get; private set; }
    public string? Role { get; private set; }
    public (string Token,DateTime ExpiresAt) GenerateToken(Guid userId, string email, string role)
    {
        UserId = userId;
        Email = email;
        Role = role;

        return ("fake_access_token", DateTime.Now.AddMinutes(30));
    }
}
