using CargoTracking.Application.Interfaces;
using CargoTracking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Tests.Fakes;
public sealed class FakeCourierLocationStore(
    IReadOnlyList<Guid> nearbyCourierIds) : ICourierLocationStore
{
    public Task<IReadOnlyList<Guid>> FindNearbyCouriersAsync(double latitude, double longitude, double radiusKm, int count = 10)
    {
        IReadOnlyList<Guid> result = nearbyCourierIds
            .Take(count)
            .ToList();

        return Task.FromResult(result);
    }

    public Task SyncCourierLocationsAsync(IEnumerable<CourierProfile> couriers)
    {
        return Task.CompletedTask;
    }

    public Task UpdateCourierLocationAsync(Guid courierId, double latitude, double longitude)
    {
        return Task.CompletedTask;
    }
}
