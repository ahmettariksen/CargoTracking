using CargoTracking.Application.Interfaces;
using System.Security.Claims;

namespace CargoTracking.API.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    //IHttpContextAccessor, mevcut HTTP isteğine erişmemizi sağlar.
    //HttpContext.User, JWT doğrulamasından geçmiş kullanıcının claim'lerini içerir.
    //Guid.TryParse, bulunan kullanıcı kimliğinin geçerli bir Guid olup olmadığını kontrol eder.
    //Kimlik bulunamazsa null döner.
    public Guid? UserId
    {
        get
        {
            var userIdValue = _httpContextAccessor.HttpContext?
                .User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (Guid.TryParse(userIdValue, out var userId))
            {
                return userId;
            }

            return null;
        }
    }
}
