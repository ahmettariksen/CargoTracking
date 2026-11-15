using CargoTracking.Application.Features.Couriers.Commands.FindNearbyCouriers;
using CargoTracking.Domain.Entities;
using CargoTracking.Domain.Enums;
using CargoTracking.Tests.Fakes;
using CargoTracking.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Tests.Features.Couriers;
public sealed class FindNearbyCouriersQueryHandlerTests
{
    //İki kurye profili oluşturuluyor.
    //İkisinin de konumu kaydediliyor.
    //İlk kurye müsait bırakılıyor, ikinci kurye müsait olmaktan çıkarılıyor.
    //Sahte konum deposu iki kurye ID'sini de döndürüyor.
    //Handler'ın yalnızca müsait kurye ID'sini döndürmesi bekleniyor.
    [Fact]
    public async Task Handle_ShouldReturnOnlyAvailableNearbyCouriers()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var availableUser = new User(
            "Ahmet",
            "Yılmaz",
            "available@example.com",
            "HASH",
            "05551111111",
            UserRole.Courier);

        var unavailableUser = new User(
            "Mehmet",
            "Demir",
            "unavailable@example.com",
            "HASH",
            "05552222222",
            UserRole.Courier);

        await context.Users.AddRangeAsync(
            availableUser,
            unavailableUser);

        await context.SaveChangesAsync();

        var availableCourier = new CourierProfile(
            availableUser.Id,
            VehicleType.Motorcycle,
            "81 ABC 111");

        var unavailableCourier = new CourierProfile(
            unavailableUser.Id,
            VehicleType.Motorcycle,
            "81 ABC 222");

        availableCourier.UpdateLocation(40.8438, 31.1565);
        unavailableCourier.UpdateLocation(40.8440, 31.1567);
        unavailableCourier.SetAvailability(false);

        await context.CourierProfiles.AddRangeAsync(
            availableCourier,
            unavailableCourier);

        await context.SaveChangesAsync();

        var locationStore = new FakeCourierLocationStore(
            new[]
            {
                availableCourier.Id,
                unavailableCourier.Id
            });

        var handler = new FindNearbyCouriersQueryHandler(
            context,
            locationStore);

        var query = new FindNearbyCouriersQuery(
            Latitude: 40.8438,
            Longitude: 31.1565,
            RadiusKm: 5,
            Count: 10);

        // Act
        var result = await handler.Handle(
            query,
            CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(availableCourier.Id, result[0]);
        Assert.DoesNotContain(unavailableCourier.Id, result);
    }
}
