using CargoTracking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Application.Interfaces;
public interface ICourierLocationStore
{
    Task UpdateCourierLocationAsync(
        Guid courierId,
        double latitude,
        double longitude);

    Task<IReadOnlyList<Guid>> FindNearbyCouriersAsync(
    //latitude, longitude: Göndericinin adres koordinatları.
    //radiusKm: Arama yarıçapı; örneğin 5 kilometre.
    //count: En fazla kaç kurye döndürüleceği.
    //IReadOnlyList<Guid>: Bulunan kuryelerin profil ID'leri.
    double latitude,
    double longitude,
    double radiusKm,
    int count = 10);
    Task SyncCourierLocationsAsync(
    IEnumerable<CourierProfile> couriers);
}

