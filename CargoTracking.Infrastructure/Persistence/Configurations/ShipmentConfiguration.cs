using CargoTracking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Infrastructure.Persistence.Configurations;
public sealed class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("Shipments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RowVersion)
    .IsRowVersion();

        builder.Property(x => x.TrackingNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(x => x.TrackingNumber)
            .IsUnique();

        builder.Property(x => x.Weight)
            .IsRequired()
            .HasPrecision(10, 2);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.EstimatedDeliveryDate)
            .IsRequired(false);

        builder.Property(x => x.DeliveredAt)
            .IsRequired(false);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired(false);

        // Sender
        builder.HasOne(x => x.Sender)
            .WithMany()
            .HasForeignKey(x => x.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        // Receiver
        builder.HasOne(x => x.Receiver)
            .WithMany()
            .HasForeignKey(x => x.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);

        // Sender Address
        builder.HasOne(x => x.SenderAddress)
            .WithMany()
            .HasForeignKey(x => x.SenderAddressId)
            .OnDelete(DeleteBehavior.Restrict);

        // Receiver Address
        builder.HasOne(x => x.ReceiverAddress)
            .WithMany()
            .HasForeignKey(x => x.ReceiverAddressId)
            .OnDelete(DeleteBehavior.Restrict);

        // Current Warehouse
        builder.HasOne(x => x.CurrentWarehouse)
            .WithMany()
            .HasForeignKey(x => x.CurrentWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Pickup Courier: göndericiden kargoyu alan kurye
        builder.HasOne(x => x.PickupCourier)
            .WithMany()
            .HasForeignKey(x => x.PickupCourierId)
            .OnDelete(DeleteBehavior.Restrict);

        // Delivery Courier: son teslimatı gerçekleştiren kurye
        builder.HasOne(x => x.DeliveryCourier)
            .WithMany()
            .HasForeignKey(x => x.DeliveryCourierId)
            .OnDelete(DeleteBehavior.Restrict);

    //    builder.HasOne(x => x.CurrentWarehouse)
    //.WithMany()
    //.HasForeignKey(x => x.CurrentWarehouseId)
    //.OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TargetWarehouse)
            .WithMany()
            .HasForeignKey(x => x.TargetWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Return Courier: kargoyu göndericiye iade edecek kurye
        builder.HasOne(x => x.ReturnCourier)
            .WithMany()
            .HasForeignKey(x => x.ReturnCourierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
