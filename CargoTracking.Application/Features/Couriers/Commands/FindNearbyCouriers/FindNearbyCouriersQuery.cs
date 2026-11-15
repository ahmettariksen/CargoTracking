using CargoTracking.Application.Abstractions.Persistence;
using CargoTracking.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Application.Features.Couriers.Commands.FindNearbyCouriers;
public sealed record FindNearbyCouriersQuery(
     double Latitude,
    double Longitude,
    double RadiusKm,
    int Count = 10):IRequest<IReadOnlyList<Guid>>;

public sealed class FindNearbyCouriersQueryHandler(
    IApplicationDbContext _context,
    ICourierLocationStore _courierLocationStore) : IRequestHandler<FindNearbyCouriersQuery, IReadOnlyList<Guid>>
{
    public async Task<IReadOnlyList<Guid>> Handle(FindNearbyCouriersQuery request, CancellationToken cancellationToken)
    {
        // 1. Redis'ten yakın kuryeleri mesafeye göre al.
        var nearbyCourierIds =
            await _courierLocationStore.FindNearbyCouriersAsync(
                request.Latitude,
                request.Longitude,
                request.RadiusKm,
                request.Count);
        if (nearbyCourierIds.Count == 0)
        {
            return Array.Empty<Guid>();
        }
        // 2. Son 5 dakikada konum güncellemiş kuryeleri belirle.
       // var locationCutoff = DateTime.Now.AddMinutes(-5);

        // 3. SQL Server'dan uygun kurye profillerini al.
        var eligibleCourierIds = await _context.CourierProfiles
            .AsNoTracking()
            .Where(c =>
                nearbyCourierIds.Contains(c.Id) &&
                c.IsAvailable &&
                c.CurrentLatitude != null &&
                c.CurrentLongitude != null)// &&
                //c.LastLocationUpdate != null &&
                //c.LastLocationUpdate >= locationCutoff)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        // 4. Redis'in mesafe sıralamasını koru.
        var eligibleCourierIdSet = eligibleCourierIds.ToHashSet();

        return nearbyCourierIds
            .Where(id => eligibleCourierIdSet.Contains(id))
            .ToList();
    }
}
