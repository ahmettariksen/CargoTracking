using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Application.Interfaces;
public interface IJwtTokenService
{
    //Burada tuple kullanıyoruz. Böylece JWT üretildiği anda belirlenen gerçek bitiş tarihini Login handler'a aktarabileceğiz.
    (string Token,DateTime ExpiresAt) GenerateToken(Guid userId, string email, string role);
}
