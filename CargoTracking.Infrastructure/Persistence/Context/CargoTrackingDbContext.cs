using CargoTracking.Application.Abstractions.Persistence;
using CargoTracking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Infrastructure.Persistence.Context;
public sealed class CargoTrackingDbContext : DbContext, IApplicationDbContext
{
    public CargoTrackingDbContext(DbContextOptions<CargoTrackingDbContext> options) : base(options)
    {
    }
    public DbSet<User> Users => Set<User>();
    public DbSet<CourierProfile> CourierProfiles => Set<CourierProfile>();
    public DbSet<WareHouseEmployeeProfile> WareHouseEmployeeProfiles => Set<WareHouseEmployeeProfile>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentTrackingEvent> ShipmentTrackingEvents => Set<ShipmentTrackingEvent>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CargoTrackingDbContext).Assembly);

        if (Database.IsSqlServer())
        {
            modelBuilder.Entity<Shipment>()
                .Property(x => x.RowVersion)
                .IsRowVersion();

            modelBuilder.Entity<CourierProfile>()
                .Property(x => x.RowVersion)
                .IsRowVersion();
        }

        base.OnModelCreating(modelBuilder);
    }
}
