using CargoTracking.Application.Interfaces;
using CargoTracking.Domain.Entities;
using RabbitMQ.Client;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Infrastructure.Redis;

//IConnectionMultiplexer: Uygulamanın Redis bağlantısını temsil eder.
//GetDatabase(): Redis üzerinde komut çalıştırabileceğimiz veritabanı arayüzünü verir.
//_database: İleride kurye koordinatlarını kaydetmek ve yakındaki kuryeleri bulmak için kullanacağımız nesnedir.
public sealed class RedisGeoService : ICourierLocationStore
{
    private readonly IDatabase _database;
    private const string CourierGeoKey = "couriers:locations";
    public RedisGeoService(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }
    //- couriers:locations: Tüm kuryelerin coğrafi konumlarını tutacağımız Redis anahtarı.
    //- courierId.ToString(): Her kuryeyi Redis içinde benzersiz bir üye olarak tanımlar.
    //- GeoAddAsync: Kurye konumunu Redis Geospatial yapısına ekler.Aynı kurye kimliğiyle tekrar çağrılırsa yeni bir üye oluşturmak yerine koordinatlarını günceller.
    //Önemli: Redis Geospatial metodunda parametre sırası longitude, latitude şeklindedir. API'mizdeki DTO ise Latitude, Longitude sırasını kullanıyor. Yukarıdaki kodda bu farkı doğru şekilde ele aldık.
    public async Task UpdateCourierLocationAsync(Guid courierId, double latitude, double longitude)
    {
        await _database.GeoAddAsync(CourierGeoKey, longitude, latitude, courierId.ToString());
    }
    public async Task SyncCourierLocationsAsync(
      IEnumerable<CourierProfile> couriers)
    {
        foreach (var courier in couriers)
        {
            if (courier.CurrentLatitude is null ||
                courier.CurrentLongitude is null)
            {
                continue;
            }

            await UpdateCourierLocationAsync(
                courier.Id,
                courier.CurrentLatitude.Value,
                courier.CurrentLongitude.Value);
        }
    }

    public async Task<IReadOnlyList<Guid>> FindNearbyCouriersAsync(double latitude, double longitude, double radiusKm, int count = 10)
    {
        //GeoRadiusAsync metodu, Redis'te kayıtlı kurye konumları arasından belirttiğimiz bir koordinatın çevresinde, belirlediğimiz mesafe içinde kalan kuryeleri bulur.

        //CourierGeoKey: Kurye koordinatlarının tutulduğu Redis anahtarı: couriers:locations
        //longitude: Aramanın merkezindeki boylam
        //latitude: Aramanın merkezindeki enlem
        //radiusKm: Arama yarıçapı, örneğin 5 km
        //GeoUnit.Kilometers: Mesafe biriminin kilometre olduğunu belirtir
        //count: En fazla kaç kurye döndürüleceği
        //Order.Ascending: Kuryeleri en yakından en uzağa sıralar
        if (radiusKm <= 0)
            throw new ArgumentOutOfRangeException(nameof(radiusKm));

        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        var results = await _database.GeoRadiusAsync(
            CourierGeoKey,
            longitude,
            latitude,
            radiusKm,
            GeoUnit.Kilometers,
            count,
            Order.Ascending);

        var courierIds = new List<Guid>();

        foreach (var result in results)
        {
            if (Guid.TryParse(
                result.Member.ToString(),
                out var courierId))
            {
                courierIds.Add(courierId);
            }
        }

        return courierIds;
    }
}
