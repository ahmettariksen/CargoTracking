using CargoTracking.Domain.Common;
using CargoTracking.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Domain.Entities;
public sealed class User : BaseEntity
{
    // Private ile rastgele değer atamalarını önlüyoruz. (Encapsulation)
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string PhoneNumber { get; private set; } = default!;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }

    private User()
    {
        // Entity Framework Core, veritabanından veri çekerken nesneyi yansıma (reflection) yoluyla oluşturabilmek için parametresiz bir constructor'a ihtiyaç duyar. Bunu private yaparak dış dünyanın bu boş nesneyi yaratmasını engellersiniz.

        // Ayrıca heryerden new ile örneklenerek müdahale edilmesini önlüyoruz
    }
    public User(
        string firstName,
        string lastName,
        string email,
        string passwordHash,
        string phoneNumber,
        UserRole role
        )
    {
        // Domain rules eklememizi kolaylaştırıyor 
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PasswordHash = passwordHash;
        PhoneNumber = phoneNumber;
        Role = role;
        IsActive = true;
    }
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.Now;
    }
}
