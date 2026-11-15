using CargoTracking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Infrastructure.Persistence.Configurations;
public sealed class CourierProfileConfiguration : IEntityTypeConfiguration<CourierProfile>
{
    public void Configure(EntityTypeBuilder<CourierProfile> builder)
    {
        builder.ToTable("CourierProfiles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.RowVersion)
    .IsRowVersion();

        builder.Property(x => x.VehicleType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.VehiclePlate)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(x => x.VehiclePlate)
            .IsUnique();

        builder.Property(x => x.IsAvailable)
            .IsRequired();

        builder.Property(x => x.CurrentLatitude)
            .IsRequired(false);

        builder.Property(x => x.CurrentLongitude)
            .IsRequired(false);

        builder.Property(x => x.LastLocationUpdate)
            .IsRequired(false);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired(false);

        builder.HasOne(x => x.User)
            .WithOne()
            .HasForeignKey<CourierProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
