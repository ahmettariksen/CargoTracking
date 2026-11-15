using CargoTracking.Infrastructure.Redis;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Tests.Features.Couriers;
public sealed class RedisGeoServiceTests
{
    [Fact]
    public async Task FindNearbyCouriersAsync_ShouldReturnCourierWithinRadius()
    {
        //// Arrange
        //using var redis =
        //    await ConnectionMultiplexer.ConnectAsync("localhost:1999");

        //var redisGeoService = new RedisGeoService(redis);

        //var expectedCourierId =
        //    Guid.Parse("8F02E634-09A8-45C8-B45D-E805229F2151");

        //// Act
        //var nearbyCourierIds =
        //    await redisGeoService.FindNearbyCouriersAsync(
        //        latitude: 40.8438,
        //        longitude: 31.1565,
        //        radiusKm: 1,
        //        count: 10);

        //// Assert
        //Assert.Contains(expectedCourierId, nearbyCourierIds);

        // Arrange
        var redisConnectionString =
            Environment.GetEnvironmentVariable("REDIS_TEST_CONNECTION_STRING")
            ?? "localhost:1999";

        using var redis =
            await ConnectionMultiplexer.ConnectAsync(redisConnectionString);

        var redisGeoService = new RedisGeoService(redis);

        var expectedCourierId = Guid.NewGuid();

        // Test için kurye konumunu Redis'e ekle.
        await redisGeoService.UpdateCourierLocationAsync(
            expectedCourierId,
            latitude: 40.8438,
            longitude: 31.1565);

        // Act
        var nearbyCourierIds =
            await redisGeoService.FindNearbyCouriersAsync(
                latitude: 40.8438,
                longitude: 31.1565,
                radiusKm: 1,
                count: 10);

        // Assert
        Assert.Contains(expectedCourierId, nearbyCourierIds);
    }
}
