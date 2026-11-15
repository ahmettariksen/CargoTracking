using CargoTracking.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Domain.Entities;
public sealed class Warehouse:BaseEntity
{
    public string Name { get; private set; } = default!;
    public string City { get; private set; } = default!;
    public string District { get; private set; } = default!;
    public string Address { get; private set; } = default!;
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public bool IsActive { get; private set; }
    private Warehouse()
    {
    }
    public Warehouse(
        string name,
        string city,
        string district,
        string address,
        double latitude,
        double longitude)
    {
        Name = name;
        City = city;
        District = district;
        Address = address;
        Latitude = latitude;
        Longitude = longitude;
        IsActive = true;
    }
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.Now;
        
    }
    public void UpdateLocation(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
        UpdatedAt = DateTime.Now;
    }
    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.Now;
    }
}
