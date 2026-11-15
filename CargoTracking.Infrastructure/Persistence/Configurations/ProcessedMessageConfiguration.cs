using CargoTracking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Infrastructure.Persistence.Configurations;
public sealed class ProcessedMessageConfiguration
    : IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedMessage> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MessageId)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(x => x.MessageId)
            .IsUnique();
    }
}
