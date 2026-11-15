using CargoTracking.Application.Abstractions.Persistence.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Tests.Fakes;
public sealed class FakePasswordHasher : IPasswordHasher
{
    public string? HashedPassword { get; private set; }
    public string Hash(string password)
    {
        HashedPassword = $"HASHED_{password}";
        return HashedPassword;
        //return "HASHED_PASSWORD";
        //return $"HASHED_{password}";
    }

    public bool Verify(string password, string passwordHash)
    {
        return passwordHash == $"HASHED_{password}";
        //return passwordHash == "HASHED_PASSWORD";
    }
}
