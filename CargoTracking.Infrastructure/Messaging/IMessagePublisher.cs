using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Infrastructure.Messaging;
public interface IMessagePublisher
{
    //messageId: Outbox tablosundaki mesajın benzersiz kimliği. bunu Consumer Idempotency için ekledik aynı mesaj birden fazla işlense bile işlem sonucunu yalnıza bir kez gerçekleşmesini sağlar.
    //messageType: Mesajın türü, örneğin ShipmentCreatedEvent.
    //payload: Mesajın JSON içeriği.
    //cancellationToken: İşlemin iptal edilmesini yönetmek için kullanılır.
    Task PublishAsync(Guid messageId, string messageType, string payload, CancellationToken cancellationToken);
}
