using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Application.Features.Shipments.Events;

// ShipmentCreatedEvent, RabbitMQ'ya gönderilecek mesajın veri sözleşmesini temsil edecek. OutboxMessage ise bu olayın JSON içeriğini veritabanında saklayacak.
public sealed record ShipmentCreatedEvent(
    Guid ShipmentId,
    string TrackingNumber,
    Guid SenderId,
    Guid ReceiverId,
    string Status, // Status alanını string olarak tanımladık. Böylece JSON mesajında durum adını ("Created") saklayabiliriz; enum değerinin sayısal karşılığını (1) göndermek zorunda kalmayız.
    DateTime CreatedAt);
