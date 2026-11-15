using CargoTracking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Application.Abstractions.Persistence;

// Bu interface neden gerekli? : Örneğin CreateShipmentCommandHandler sınıfımız bir kargo kaydı oluşturduğunda veritabanına kaydetmesi gerekecek.
// Handler'ın doğrudan CargoTrackingDbContext kullanması yerine IApplicationDbContext kullanmasını sağlayacağız.

// Avantajları : Handler testlerinde gerçek SQL Server'a bağlanmak yerine interface'in sahte bir uygulamasını kullanabilirsin. Böylece kargo oluşturma iş mantığını veritabanından ayrı test edebilirsin.
// Application iş kurallarını yönetir; Infrastructure ise SQL Server, Redis ve RabbitMQ gibi teknik detayları uygular. Bu ayrım, proje büyüdükçe kodun sorumluluklarını anlamayı ve yönetmeyi kolaylaştırır.
// İleride veritabanı erişim altyapısını değiştirmek veya yeniden düzenlemek istediğinde Application kodunun doğrudan CargoTrackingDbContext sınıfına bağlı olmaması işini kolaylaştırır. Ancak SQL Server'dan PostgreSQL'e geçmek de tek başına interface sayesinde otomatik olarak kolaylaşmaz; EF Core sorguları ve veritabanına özgü davranışlar yine gözden geçirilir.

// Dezavantajı : Daha fazla kod yazmaya yol açar.
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<CourierProfile> CourierProfiles { get; }
    DbSet<WareHouseEmployeeProfile> WareHouseEmployeeProfiles { get; }
    DbSet<Address> Addresses { get; }
    DbSet<Warehouse> Warehouses { get; }
    DbSet<Shipment> Shipments { get; }
    DbSet<ShipmentTrackingEvent> ShipmentTrackingEvents { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }

    //RabbitMQ'da daha önce işlenmiş mesajları tutar.
    DbSet<ProcessedMessage> ProcessedMessages { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
