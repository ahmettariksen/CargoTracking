using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Application.Interfaces;
public interface IRefreshTokenService
{
    //Kullanıcıya verilecek rastgele Refresh Token'ı üretir.
    string GenerateToken();

    //Üretilen token'ın SHA256 hash değerini hesaplar. Veritabanına bu değer kaydedilir.
    string HashToken(string token);
}
