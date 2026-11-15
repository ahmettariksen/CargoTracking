using CargoTracking.Application.Abstractions.Persistence;
using CargoTracking.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Claims;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Infrastructure.BackgroundJobs;
public sealed class OutboxBackgroundService : BackgroundService
{
    // BU SERVİSİN SORUMLULUKLARI NELERDİR?
    //Veritabanındaki işlenmemiş Outbox mesajlarını bulmak.
    //Mesajları IMessagePublisher üzerinden RabbitMQ'ya göndermek.
    //Gönderim başarılıysa ProcessedAt alanını güncellemek.
    //Hata olursa mesajı başarısız olarak işaretlemek ve yeniden denemek.

    // EKLENEN ISERVİCESCOPEFACTORY  VE ProcessOutboxMessagesAsync İLE NELER DEĞİŞTİ

    //Her 5 saniyede bir işlenmemiş mesajları arıyoruz.
    //Her turda en fazla 20 mesaj alıyoruz.
    //Mesaj RabbitMQ'ya gönderildikten sonra ProcessedAt güncelleniyor.
    //Gönderim başarısız olursa RetryCount artırılıyor ve hata bilgisi kaydediliyor.
    //Hatalar loglanıyor; tek bir mesajdaki hata servis döngüsünü tamamen durdurmuyor.

    //YENİ Güncelleme , yeni eklenenlerin yanına "ye" yazacak
    // bu background sınıfına atomiklik ekliyoruz.(12/11/2026)

    private readonly ILogger<OutboxBackgroundService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    private const int MaxRetryCount = 5;
    private const int BatchSize = 20; //ye

    public OutboxBackgroundService(ILogger<OutboxBackgroundService> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //BackgroundService, uygulama çalışırken arka planda devam eden bir servis oluşturur.
        //ExecuteAsync, servis başladığında çalıştırılır.
        //stoppingToken, uygulama kapatılırken döngünün durmasına yardımcı olur.
        //Task.Delay, servisin sürekli işlem yaparak CPU tüketmesini önler.

        _logger.LogInformation("Outbox background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing Outbox messages.");
            }
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        //Artık doğrudan DbContext kullanmıyoruz
        //var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        // Mesajları alma, kilitleme, başarılı veya başarısız olarak işaretleme sorumluluğu IOutboxMessageClaimer sınıfına geçti.
        //Bu sayede SQL Server'a özel atomik kilitleme işlemleri Infrastructure içerisinde kalıyor.
        var claimer = scope.ServiceProvider.GetRequiredService<IOutboxMessageClaimer>(); //ye

        var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

        // Her işlem döngüsü için benzersiz bir worker kimliği oluşturulur.
        var lockId = Guid.NewGuid(); //ye

        //Bu sorgunun problemi şu: İki farklı uygulama örneği aynı anda çalışırsa ikisi de aynı 20 mesajı okuyabilir.
        //Bunu SQL Server üzerinde atomik bir işlemle çözeceğiz. Mesajlar seçilirken aynı işlem içerisinde kilitlenecek ve her worker yalnızca kendi sahiplendiği mesajları işleyecek.
        // Bu sorunun çözümü için SQL Server üzerinde atomik mesaj sahiplenme kullanacağız.

        //eski sorgumuz

        //var messages = await context.OutboxMessages
        //    .Where(x => x.ProcessedAt == null
        //    && x.RetryCount < MaxRetryCount
        //    && (x.NextRetryAt == null || x.NextRetryAt <= DateTime.Now))
        //    .OrderBy(x => x.CreatedAt)
        //    .Take(20)
        //    .ToListAsync(cancellationToken);

        var messages = await claimer.ClaimMessagesAsync(
           BatchSize,
           MaxRetryCount,
           lockId,
           cancellationToken);

        //eski dongü
        //foreach (var message in messages)
        //{
        //    try
        //    {
        //        await publisher.PublishAsync(
        //           message.Type,
        //           message.Payload,
        //           cancellationToken);

        //        message.MarkAsProcessed();
        //        await context.SaveChangesAsync(cancellationToken);

        //        _logger.LogInformation(
        //            "Outbox message {MessageId} published successfully.",
        //            message.Id);
        //    }
        //    catch (OperationCanceledException)
        //     when (cancellationToken.IsCancellationRequested)
        //    {
        //        throw;
        //    }
        //    catch (Exception ex)
        //    {
        //        // Error sütununun uzunluk sınırını aşmasını önlemeye yardımcı olur.
        //        var errorMessage = ex.Message.Length > 2000
        //            ? ex.Message[..2000]
        //            : ex.Message;

        //        message.MarkAsFailed(errorMessage);
        //        await context.SaveChangesAsync(cancellationToken);


        //        if (message.RetryCount >= MaxRetryCount)
        //        {
        //            _logger.LogError(
        //                ex,
        //                "Outbox message {MessageId} reached the maximum retry count of {MaxRetryCount}.",
        //                message.Id,
        //                MaxRetryCount);
        //        }
        //        else
        //        {
        //            _logger.LogWarning(
        //                ex,
        //                "Failed to publish Outbox message {MessageId}. Retry attempt {RetryCount} of {MaxRetryCount}.",
        //                message.Id,
        //                message.RetryCount,
        //                MaxRetryCount);
        //        }
        //    }
        //}

        
        foreach (var message in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Mesaj RabbitMQ'ya gönderilir.
                await publisher.PublishAsync(
                    message.Id,
                    message.Type,
                    message.Payload,
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Hata mesajının veritabanındaki uzunluk sınırını aşması engellenir.
                var errorMessage = ex.Message.Length > 2000
                    ? ex.Message[..2000]
                    : ex.Message;

                var markedAsFailed = await claimer.MarkAsFailedAsync(
                    message.Id,
                    lockId,
                    MaxRetryCount,
                    errorMessage,
                    cancellationToken);

                if (!markedAsFailed)
                {
                    _logger.LogWarning(
                        ex,
                        "Outbox message {MessageId} could not be marked as failed because the worker no longer owns its lock.",
                        message.Id);

                    continue;
                }

                var retryCount = message.RetryCount + 1;

                if (retryCount >= MaxRetryCount)
                {
                    _logger.LogError(
                        ex,
                        "Outbox message {MessageId} reached the maximum retry count and was moved to dead-letter state.",
                        message.Id);
                }
                else
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to publish Outbox message {MessageId}. Retry attempt {RetryCount} of {MaxRetryCount}.",
                        message.Id,
                        retryCount,
                        MaxRetryCount);
                }

                continue;
            }

            // Yayın başarılıysa mesaj yalnızca sahibi tarafından tamamlanabilir.
            var markedAsProcessed = await claimer.MarkAsProcessedAsync(
                message.Id,
                lockId,
                cancellationToken);

            if (!markedAsProcessed)
            {
                _logger.LogWarning(
                    "Outbox message {MessageId} was published, but could not be marked as processed because the worker no longer owns its lock.",
                    message.Id);

                continue;
            }

            _logger.LogInformation(
                "Outbox message {MessageId} published and marked as processed successfully.",
                message.Id);
        }
    }
}
