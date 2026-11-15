using CargoTracking.Domain.Entities;
using CargoTracking.Infrastructure.Persistence.Context;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using StackExchange.Redis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Infrastructure.BackgroundJobs;

//Bu sınıfın görevi, bekleyen Outbox mesajlarını seçmek ve aynı veritabanı işlemi içerisinde bu mesajları sahiplenmektir.
public class SqlServerOutboxMessageClaimer : IOutboxMessageClaimer
{
    private readonly CargoTrackingDbContext _context;

    //Worker bir mesajı sahiplendiğinde iki dakikalık kilit oluşturuyoruz.
    //Worker çökerse bu süre dolduğunda başka bir worker mesajı tekrar sahiplenebilir.
    private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(2);

    public SqlServerOutboxMessageClaimer(CargoTrackingDbContext context)
    {
        _context = context;
    }
     
    public async Task<IReadOnlyList<OutboxMessage>> ClaimMessagesAsync(int batchSize, int maxRetryCount, Guid lockId, CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var lockedUntil = now.Add(LockDuration);

        //UPDLOCK: Seçilen satırlarda güncelleme kilidi alır.
        //READPAST: Başka işlemlerin kilitlediği satırları atlar.
        //ROWLOCK: Satır bazında kilitlemeyi tercih eder.

        // where şartından sonraki and sorgularının sonucunda gelmesi gereken sonuçlar
        //Bu koşullar yalnızca:
        //-Henüz yayınlanmamış,
        //-Kalıcı olarak başarısız olmamış,
        //-Deneme sınırını aşmamış,
        //-Yeniden deneme zamanı gelmiş,
        //-Başka worker tarafından hâlâ kilitlenmemiş
        //mesajları seçer.

        // update şartı ise atomik sahiplenmedir en önemli kısım orasıdır. Mesajları yalnızca okumuyoruz. Seçtiğimiz satırları aynı SQL işlemi içerisinde güncelliyoruz.
        //OUTPUT INSERTED.Id ise sahiplenilen mesajların kimliklerini döndürüyor.
        var sql = """
            ;WITH MessagesToClaim AS
            (
                SELECT TOP (@BatchSize) *
                FROM OutboxMessages WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE ProcessedAt IS NULL
                    AND DeadLetteredAt IS NULL
                    AND RetryCount < @MaxRetryCount
                    AND (NextRetryAt IS NULL OR NextRetryAt <= @Now)
                    AND (LockedUntil IS NULL OR LockedUntil <= @Now)
                ORDER BY CreatedAt
            )
            UPDATE MessagesToClaim
            SET
                LockId = @LockId,
                LockedUntil = @LockedUntil,
                UpdatedAt = @Now
            OUTPUT INSERTED.Id;
            """;

        var messageIds = new List<Guid>();

        //Mesaj seçme ve kilitleme işlemleri transaction içerisinde gerçekleşiyor. Commit sonrasında kilitler veritabanında kalıyor.
        //RabbitMQ gönderimi transaction dışında yapılacak
        await using var transaction =
    await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command =
                _context.Database.GetDbConnection().CreateCommand();

            command.Transaction =
                transaction.GetDbTransaction();

            command.CommandText = sql;

            command.Parameters.Add(new SqlParameter("@BatchSize", batchSize));
            command.Parameters.Add(new SqlParameter("@MaxRetryCount", maxRetryCount));
            command.Parameters.Add(new SqlParameter("@Now", now));
            command.Parameters.Add(new SqlParameter("@LockedUntil", lockedUntil));
            command.Parameters.Add(new SqlParameter("@LockId", lockId));

            if (command.Connection!.State != System.Data.ConnectionState.Open)
            {
                await command.Connection.OpenAsync(cancellationToken);
            }

            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                messageIds.Add(reader.GetGuid(0));
            }

            await reader.CloseAsync();

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        if (messageIds.Count == 0)
        {
            return Array.Empty<Domain.Entities.OutboxMessage>();
        }

        return await _context.OutboxMessages
            //Mesajları yalnızca yayınlamak için okuduğumuzdan EF Core'un bunları takip etmesine gerek yok.
            .AsNoTracking()
            .Where(x => messageIds.Contains(x.Id) && x.LockId == lockId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> MarkAsProcessedAsync(Guid messageId, Guid lockId, CancellationToken cancellationToken)
    {
        //messageId: Hangi mesajı güncelleyeceğimizi belirler.
        //lockId: Mesajın gerçekten bu worker tarafından sahiplenildiğini kontrol eder.
        //LockedUntil > now: Kilidin süresinin henüz dolmadığını doğrular.
        //ProcessedAt == null: Daha önce tamamlanmış mesajın tekrar güncellenmesini engeller.
        //ExecuteUpdateAsync: Şartlara uyan kaydı doğrudan veritabanında günceller.
        //affectedRows == 1: Bir kayıt güncellendiyse true, güncellenmediyse false döndürür.
        //İşlem başarılı olduğunda kilit bilgileri temizlenir.
        var now = DateTime.Now;

        var affectedRows = await _context.OutboxMessages
            .Where(x => x.Id == messageId
                && x.LockId == lockId
                && x.LockedUntil > now
                && x.ProcessedAt == null
                && x.DeadLetteredAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.ProcessedAt, (DateTime?)now)
            .SetProperty(x => x.Error, (string?)null)
            .SetProperty(x => x.NextRetryAt, (DateTime?)null)
            .SetProperty(x => x.LockId, (Guid?)null)
            .SetProperty(x => x.LockedUntil, (DateTime?)null)
            .SetProperty(x => x.UpdatedAt, (DateTime?)now),
                cancellationToken);

        return affectedRows == 1;
    }

    public async Task<bool> MarkAsFailedAsync(Guid messageId, Guid lockId, int maxRetryCount, string error, CancellationToken cancellationToken)
    {
        //Önce mesajın mevcut RetryCount değerini ve kilit sahipliğini kontrol ediyoruz.
        //Her başarısızlıkta RetryCount bir artırılıyor.
        //Tekrar deneme aralıkları sırasıyla 5, 10, 20, 40 ve 80 saniye oluyor.
        //Maksimum deneme sayısına ulaşıldığında DeadLetteredAt dolduruluyor. Böylece mesaj artık tekrar işlenmeyecek.
        //RetryCount == currentRetryCount.Value koşulu, okuma ile güncelleme arasında başka bir işlemin mesajı değiştirmesi ihtimaline karşı ek bir koruma sağlıyor.
        //Mesaj yeniden denemeye bırakıldığında veya dead-letter durumuna geçtiğinde kilit bilgileri temizleniyor.
        var now = DateTime.Now;

        if (error.Length > 2000)
        {
            error = error[..2000];
        }

        var currentRetryCount = await _context.OutboxMessages
            .AsNoTracking()
            .Where(x => x.Id == messageId
                && x.LockId == lockId
                && x.LockedUntil > now
                && x.ProcessedAt == null
                && x.DeadLetteredAt == null)
            .Select(x => (int?)x.RetryCount)
            .SingleOrDefaultAsync(cancellationToken);

        if (currentRetryCount is null)
        {
            return false;
        }

        var newRetryCount = currentRetryCount.Value + 1;

        var isDeadLetter = newRetryCount >= maxRetryCount;

        DateTime? nextRetryAt = isDeadLetter
            ? null
            : now.AddSeconds(5 * Math.Pow(2, newRetryCount - 1));

        DateTime? deadLetteredAt = isDeadLetter
            ? now
            : null;

        var affectedRows = await _context.OutboxMessages
            .Where(x => x.Id == messageId
                && x.LockId == lockId
                && x.LockedUntil > now
                && x.ProcessedAt == null
                && x.DeadLetteredAt == null
                && x.RetryCount == currentRetryCount.Value)
            .ExecuteUpdateAsync(setters => setters
                 .SetProperty(x => x.RetryCount, newRetryCount)
            .SetProperty(x => x.Error, error)
            .SetProperty(x => x.NextRetryAt, nextRetryAt)
            .SetProperty(x => x.DeadLetteredAt, deadLetteredAt)
            .SetProperty(x => x.LockId, (Guid?)null)
            .SetProperty(x => x.LockedUntil, (DateTime?)null)
            .SetProperty(x => x.UpdatedAt, (DateTime?)now),
                cancellationToken);

        return affectedRows == 1;
    }
}
