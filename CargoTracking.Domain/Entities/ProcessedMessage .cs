using CargoTracking.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Domain.Entities;

// consumer ıdempotency için ekledik
//ProcessedMessage : BaseEntity: Diğer entity'lerimiz gibi otomatik Id ve CreatedAt değerlerini alır.
//MessageId: RabbitMQ'dan gelen mesajın benzersiz kimliğini tutar.
//private set: Mesaj kimliğinin dışarıdan rastgele değiştirilmesini engeller.
//private ProcessedMessage() : EF Core'un veritabanından nesne oluşturabilmesini sağlar.
//ProcessedMessage(string messageId) : Yeni bir işlenmiş mesaj kaydı oluşturur.
//string.IsNullOrWhiteSpace: Boş mesaj kimliğinin kaydedilmesini engeller.
public sealed class ProcessedMessage : BaseEntity
{
    public string MessageId { get; private set; } = default!;

    private ProcessedMessage()
    {
    }

    public ProcessedMessage(string messageId)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            throw new ArgumentException(
                "MessageId cannot be empty.",
                nameof(messageId));
        }

        MessageId = messageId;
    }
}
