using CargoTracking.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Infrastructure.Services.Authentication;
public sealed class RefreshTokenService : IRefreshTokenService
{
    public string GenerateToken()
    {
        //kriptografik olarak güvenli 64 byte rastgele veri üretir
        var randomBytes = RandomNumberGenerator.GetBytes(64);

        //bu veriyi taşınabilir bir metne dönüştürür.
        return Convert.ToBase64String(randomBytes);
    }

    public string HashToken(string token)
    {
        var tokenBytes=Encoding.UTF8.GetBytes(token);

        //token'ın hash'ini hesaplar.
        var hashBytes =SHA256.HashData(tokenBytes);

        //hash'i 64 karakterlik hexadecimal metne çevirir.
        return Convert.ToHexString(hashBytes);
    }
}
