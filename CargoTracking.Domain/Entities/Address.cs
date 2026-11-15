using CargoTracking.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Domain.Entities;
public sealed class Address : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = default!;
    public string City { get; private set; } = default!;
    public string District { get; private set; } = default!;
    public string Neighborhood { get; private set; } = default!;
    public string FullAddress { get; private set; } = default!;

    // Latitude ve Longitude propertyleri ileride Redis Geospatial kullanırken kurye ile   gönderinin/adresin konumunu karşılaştıracağız.Yani bu alanlar sadece "adres bilgisi" için değil, projenin kurye atama algoritmasında da kullanılacak.

    // Bunlar birer koordinat bilgisi
    // Bunlar ileride Redis Geospatial ile yakınlık hesaplamalarında kullanılabilir.
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public User User { get; private set; } = default!;
    private Address()
    {
    }
    public Address(
        Guid userId,
        string title,
        string city,
        string district,
        string neighborhood,
        string fullAddress,
        double latitude,
        double longitude)
    {
        UserId = userId;
        Title = title;
        City = city;
        District = district;
        Neighborhood = neighborhood;
        FullAddress = fullAddress;
        Latitude = latitude;
        Longitude = longitude;
    }
    public void Update(string title, string city, string district, string neighborhood, string fullAddress, double latitude, double longitude)
    {
        Title = title;
        City = city;
        District = district;
        Neighborhood = neighborhood;
        FullAddress = fullAddress;
        Latitude = latitude;
        Longitude = longitude;
        UpdatedAt = DateTime.Now;
    }
}
