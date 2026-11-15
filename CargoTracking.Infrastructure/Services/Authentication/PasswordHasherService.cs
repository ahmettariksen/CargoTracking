using CargoTracking.Application.Abstractions.Persistence.Authentication;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Infrastructure.Services.Authentication;
public sealed class PasswordHasherService : IPasswordHasher
{
    //Hash() : Parolayı hash'ler. Veritabanına düz parola yerine hash değeri kaydedilir.
    //Verify() : Kullanıcının girdiği parolayı kayıtlı hash ile doğrular.
    //PasswordHasher<object>: ASP.NET Core Identity'nin yerleşik hashleme mekanizmasını kullanır; kendi hash algoritmamızı yazmamıza gerek kalmaz.
    private readonly PasswordHasher<object> _passwordHasher = new();
    public string Hash(string password)
    {
        return _passwordHasher.HashPassword(new object(), password);
    }

    public bool Verify(string password, string passwordHash)
    {
        var result = _passwordHasher.VerifyHashedPassword(new object(), passwordHash, password);

        return result != PasswordVerificationResult.Failed;
    }
}
