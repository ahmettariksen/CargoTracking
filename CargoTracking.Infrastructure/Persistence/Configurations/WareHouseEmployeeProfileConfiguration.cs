using CargoTracking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Infrastructure.Persistence.Configurations;
public sealed class WareHouseEmployeeProfileConfiguration : IEntityTypeConfiguration<WareHouseEmployeeProfile>
{
    public void Configure(EntityTypeBuilder<WareHouseEmployeeProfile> builder)
    {
        builder.ToTable("WareHouseEmployeeProfiles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
           .IsRequired();

        builder.HasOne(x => x.User)
            .WithOne()
            .HasForeignKey<WareHouseEmployeeProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WareHouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
