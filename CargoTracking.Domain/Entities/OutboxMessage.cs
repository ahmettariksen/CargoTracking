using CargoTracking.Domain.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Domain.Entities;
// Shipment, ShipmentTrackingEvent ve OutboxMessage aynı SaveChangesAsync() çağrısında kaydedilirse SQL Server tarafında atomiklikten yararlanabiliriz. Böylece kargo kaydedilip Outbox kaydının kaybolması riski azaltılır.
// Atomiklik, veritabanında birden fazla işlemin ya hep birlikte başarılı olması ya da hata durumunda hep birlikte geri alınması demektir.
// OutboxMessage sınıfına RabbitMQ'ya gönderilecek mesajları geçici olarak veritabanında saklayan bir sınıf diyebiliriz.
public sealed class OutboxMessage : BaseEntity
{
    // Bu entity özellikle RabbitMQ + Outbox Pattern için çok önemli.

    // Mesajın tipini tutacak:
        // ShipmentCreated
        // ShipmentStatusChanged
        // CourierAssigned
        // ShipmentDelivered gibi.
    public string Type { get; private set; } = default!;

    // Mesajın JSON içeriğini tutacak
       // {
       //  "shipmentId": "...",
       //  "trackingNumber": "TR123456"
       // } gibi.
    public string Payload { get; private set; } = default!;

    // Mesajın RabbitMQ'ya başarıyla gönderilip gönderilmediğini anlamamızı sağlayacak.
    // null: Henüz işlenmedi, tarih varsa: işlendi gibi.
    public DateTime? ProcessedAt { get; private set; }

    // RabbitMQ'ya gönderim başarısız olursa kaç kez denendiğini tutacağız.
    // RetryCount tek başına yeterli değil. Arka plan servisi belirli bir deneme sınırına, bekleme stratejisine ve kalıcı olarak başarısız olan mesajları ele alma politikasına ihtiyaç duyacak.
    public int RetryCount { get; private set; }

    // Son başarısızlığın hata bilgisini tutabiliriz.
    public string? Error { get; private set; }

    //Bu alan, mesajın bir sonraki ne zaman denenebileceğini tutacak.
    public DateTime? NextRetryAt { get; private set; }

    // **** outobox kontrolü sonrasında eklenen yeni props'lar ****

    // Bu alan, bir Outbox mesajının hangi zamana kadar başka bir worker tarafından işlenemeyeceğini belirtir.
    //    Örneğin iki uygulama örneğinin aynı anda çalıştığını düşün:
    //- Worker A mesajı veritabanından aldı.
    //- Worker B de aynı mesajı veritabanından aldı.
    //- İkisi de RabbitMQ'ya aynı mesajı gönderdi.
    //Sonuç: Aynı event iki kez yayınlandı Bunu önlemek için Worker A mesajı sahiplenirken şöyle bir işlem yapar: => LockedUntil = DateTime.Now.AddMinutes(1); =>Bu, mesajın bir dakika boyunca başka bir worker tarafından sahiplenilmemesi gerektiğini belirtir. Worker çökerse kilit süresi dolduğunda başka bir worker mesajı tekrar deneyebilir.
    public DateTime? LockedUntil { get; private set; }

    //Bu alan mesajı sahiplenen worker'a ait benzersiz bir kimlik tutar.
    //Mesajın hangi worker tarafından sahiplenildiği
    //Worker A bu mesajı sahiplenmiştir.
    //Worker B aynı mesajı işlemeye çalıştığında mesajın başka bir worker'a ait olduğunu anlayabilir.
    public Guid? LockId { get; private set; }

    //Kalıcı olarak başarısız olan mesajın tarihi
    public DateTime? DeadLetteredAt { get; private set; }

    private OutboxMessage()
    {
    }
    public OutboxMessage(string type, string payload)
    {
        Type = type;
        Payload = payload;
        RetryCount = 0;
    }
    public void MarkAsProcessed()
    {
        ProcessedAt = DateTime.Now;
        Error = null;
        NextRetryAt = null;
    }

    //Bu, Exponential Backoff yaklaşımıdır.
    public void MarkAsFailed(string error)
    {
        RetryCount++;
        Error = error;

        var delaySeconds = 5 * Math.Pow(2, RetryCount - 1);

        NextRetryAt = DateTime.Now.AddSeconds(delaySeconds);
    }
}
