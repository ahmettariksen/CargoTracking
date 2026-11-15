using CargoTracking.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Tests.Fakes;
public sealed class FakeRefreshTokenService : IRefreshTokenService
{
    public string GenerateToken()
    {
        return "fake_refresh_token";
    }

    public string HashToken(string token)
    {
        return Convert.ToHexString(
           System.Security.Cryptography.SHA256.HashData(
               System.Text.Encoding.UTF8.GetBytes(token)));
    }
}
