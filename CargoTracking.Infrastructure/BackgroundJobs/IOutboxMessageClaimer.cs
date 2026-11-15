using CargoTracking.Domain.Entities;
using MailKit;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CargoTracking.Infrastructure.BackgroundJobs;

//Mesaj sahiplenme işleminin sözleşmesi. Worker'ın SQL Server detaylarını bilmesini engeller.
//Bu arayüzün amacı, Outbox mesajlarını sahiplenme işlemini OutboxBackgroundService sınıfından ayırmaktır.
//Böylece BackgroundService SQL Server'ın nasıl kilitleme yaptığını bilmek zorunda kalmaz.
public interface IOutboxMessageClaimer
{
    //    batchSize
    //Bir seferde kaç mesaj sahiplenileceğini belirtir.Mevcut sistemimizde bu değer 20 olacak.

    //    maxRetryCount
    //Maksimum deneme sayısını belirtir.Mevcut Outbox yapımızda 5 kullanıyoruz.

    //    lockId
    //Mesajları sahiplenen worker'ın benzersiz kimliğidir. Böylece worker yalnızca kendi sahip olduğu mesajları tamamlayabilir veya kilidini kaldırabilir.

    //    CancellationToken
    //Uygulama kapanırken işlemin kontrollü şekilde durmasını sağlar.

    //    IReadOnlyList<OutboxMessage>
    //Sahiplenilen mesajları döndürür.Arayüz üzerinden dönen listeyi değiştirmeye ihtiyaç duymuyoruz.
    // Metodun amacı=> Bekleyen mesajları atomik olarak sahiplenir.
    Task<IReadOnlyList<OutboxMessage>> ClaimMessagesAsync(int batchSize, int maxRetryCount, Guid lockId, CancellationToken cancellationToken);

    // Metodun amacı=> Mesajı yalnızca kilidin sahibi tamamlayabilir.
    //  RabbitMQ'ya mesaj başarıyla gönderildiğinde çağrılacak.
    //- Mesajın LockId değeri kontrol edilecek.
    //- Mesajın ProcessedAt alanı doldurulacak.
    //- Error ve NextRetryAt temizlenecek.
    //- Kilit alanları sıfırlanacak.
    Task<bool> MarkAsProcessedAsync(
        Guid messageId,
        Guid lockId,
        CancellationToken cancellationToken);

    // Metodun amacı=> Mesajı yalnızca kilidin sahibi başarısız olarak işaretleyebilir.
    //  RabbitMQ'ya gönderim başarısız olduğunda çağrılacak.
    //- Mesajın LockId değeri kontrol edilecek.
    //- RetryCount artırılacak.
    //- NextRetryAt hesaplanacak.
    //- Deneme sınırı dolduysa DeadLetteredAt doldurulacak.
    //- Kilit alanları sıfırlanacak.
    Task<bool> MarkAsFailedAsync(
        Guid messageId,
        Guid lockId,
        int maxRetryCount,
        string error,
        CancellationToken cancellationToken);
}
