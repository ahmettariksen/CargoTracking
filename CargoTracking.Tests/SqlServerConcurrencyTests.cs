using CargoTracking.Domain.Entities;
using CargoTracking.Domain.Enums;
using CargoTracking.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Tests;
public sealed class SqlServerConcurrencyTests
{
    [Fact]
    public async Task Database_ShouldApplyMigrationsSuccessfully()
    {
        await SqlServerTestDatabaseInitializer.InitializeAsync();

        await using var context = SqlServerTestDbContextFactory.Create();

        var canConnect = await context.Database.CanConnectAsync();

        Assert.True(canConnect);
    }

    [Fact]
    public async Task CourierProfile_WhenUpdatedByAnotherContext_ShouldThrowConcurrencyException()
    {
        // Arrange
        await SqlServerTestDatabaseInitializer.InitializeAsync();

        Guid courierProfileId;

        // Test kullanıcısını ve kurye profilini oluştur.
        await using (var setupContext = SqlServerTestDbContextFactory.Create())
        {
            var user = new User(
                "Test",
                "Courier",
                $"courier_{Guid.NewGuid()}@test.com",
                "HASHED_password",
                "05550000000",
                UserRole.Courier);

            var vehiclePlate = $"81 {Random.Shared.Next(100, 999)} {Random.Shared.Next(1000, 9999)}";

            var courierProfile = new CourierProfile(
                user.Id,
                VehicleType.Motorcycle,
                vehiclePlate);

            setupContext.Users.Add(user);
            setupContext.CourierProfiles.Add(courierProfile);

            await setupContext.SaveChangesAsync();

            courierProfileId = courierProfile.Id;
        }

        // İki bağımsız DbContext aynı kaydı yükler.
        await using var firstContext = SqlServerTestDbContextFactory.Create();
        await using var secondContext = SqlServerTestDbContextFactory.Create();

        var firstCourier = await firstContext.CourierProfiles
            .SingleAsync(x => x.Id == courierProfileId);

        var secondCourier = await secondContext.CourierProfiles
            .SingleAsync(x => x.Id == courierProfileId);

        // Act: İlk context kaydı günceller.
        firstCourier.SetAvailability(false);

        await firstContext.SaveChangesAsync();

        // İkinci context eski RowVersion ile güncellemeye çalışır.
        secondCourier.SetAvailability(false);

        // Assert: Eski RowVersion nedeniyle concurrency hatası beklenir.
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => secondContext.SaveChangesAsync());
    }
}
