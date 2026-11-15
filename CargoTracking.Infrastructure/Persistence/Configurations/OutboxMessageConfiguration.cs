using CargoTracking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Infrastructure.Persistence.Configurations;
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(x => x.Payload)
            .IsRequired();

        builder.Property(x => x.ProcessedAt)
            .IsRequired(false);

        builder.Property(x => x.RetryCount)
            .IsRequired();

        builder.Property(x => x.Error)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(x => x.NextRetryAt)
            .IsRequired(false);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired(false);

        // Outbox mesajlarının sorgulanmasını hızlandırır.
        builder.HasIndex(x => new
        {
            x.ProcessedAt,
            x.DeadLetteredAt,
            x.NextRetryAt,
            x.LockedUntil
        });

        // Mesajın hangi zamana kadar kilitli olduğunu tutar.
        builder.Property(x => x.LockedUntil)
            .IsRequired(false);

        // Mesajı sahiplenen worker'ın benzersiz kimliği.
        builder.Property(x => x.LockId)
            .IsRequired(false);

        // Maksimum deneme sayısı aşıldığında doldurulur.
        builder.Property(x => x.DeadLetteredAt)
            .IsRequired(false);
    }
}
