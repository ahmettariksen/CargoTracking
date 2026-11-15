using CargoTracking.Domain.Common;
using CargoTracking.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Domain.Entities;
public sealed class CourierProfile : BaseEntity
{
    // Concurrency yapısı için ekledik, concurrency ile iki farklı kargo siparişine aynı anda gönderilen isteklerde aynı kuryeyi atamayı önlemek için veya bir kuryeye iki farklı kargo siparişini önlemek için kullanacağız.
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
    // Private ile rastgele değer atamalarını önlüyoruz. (Encapsulation)
    public Guid UserId { get; private set; }
    public VehicleType VehicleType { get; private set; }
    public string VehiclePlate { get; private set; } = default!;
    public bool IsAvailable { get; private set; }
    public double? CurrentLatitude { get; private set; }
    public double? CurrentLongitude { get; private set; }
    public DateTime? LastLocationUpdate { get; private set; }
    public User User { get; private set; } = default!;
    private CourierProfile()
    {
        // Entity Framework Core, veritabanından veri çekerken nesneyi yansıma (reflection) yoluyla oluşturabilmek için parametresiz bir constructor'a ihtiyaç duyar. Bunu private yaparak dış dünyanın bu boş nesneyi yaratmasını engellersiniz.

        // Ayrıca heryerden new ile örneklenerek müdahale edilmesini önlüyoruz
    }
    public CourierProfile(
        Guid userId,
        VehicleType vehicleType,
        string vehiclePlate)
    {
        UserId = userId;
        VehicleType = vehicleType;
        VehiclePlate = vehiclePlate;
        IsAvailable = true;
    }
    public void UpdateLocation(double latitude, double longitude)
    {
        // Konum güncelleme işlemleri için yazdık
        // Entity kendi durumunu kendi güncelleyecek dışarıdan propertyler ile müdahele edilerek yapılmayacak.
        CurrentLatitude = latitude;
        CurrentLongitude = longitude;
        LastLocationUpdate = DateTime.Now;
    }
    public void SetAvailability(bool isAvailable)
    {
        IsAvailable = isAvailable;
        UpdatedAt = DateTime.Now;
    }
}
