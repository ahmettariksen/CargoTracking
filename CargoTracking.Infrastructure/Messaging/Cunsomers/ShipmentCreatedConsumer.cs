using CargoTracking.Application.Abstractions.Persistence;
using CargoTracking.Application.Features.Shipments.Events;
using CargoTracking.Domain.Entities;
using CargoTracking.Infrastructure.Options;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CargoTracking.Infrastructure.Messaging.Cunsomers;
public sealed class ShipmentCreatedConsumer : BackgroundService
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<ShipmentCreatedConsumer> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public ShipmentCreatedConsumer(IOptions<RabbitMqOptions> options, ILogger<ShipmentCreatedConsumer> logger,IServiceScopeFactory scopeFactory)
    {
        _options = options.Value;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //BasicConsumeAsync: Kuyruğu dinlemeye başlar.
        //autoAck: false: Mesajın otomatik olarak onaylanmasını engeller.
        //BasicAckAsync: Mesaj başarıyla işlenince RabbitMQ'ya onay gönderir.
        //BasicNackAsync: İşlem başarısız olursa mesajı yeniden kuyruğa koymadan reddeder. Şimdilik dead-letter yapılandırmamız olmadığı için bu mesaj kaybolabilir; daha sonra hata yönetimini geliştireceğiz.
        //BasicQosAsync: Consumer'ın aynı anda kontrolsüz şekilde çok sayıda onaysız mesaj almasını sınırlar.

        //var factory = new ConnectionFactory
        //{
        //    HostName = _options.HostName,
        //    Port = _options.Port,
        //    UserName = _options.UserName,
        //    Password = _options.Password
        //};

        //await using var connection = await factory.CreateConnectionAsync(stoppingToken);

        //await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        //await channel.QueueDeclareAsync(
        //   queue: "cargo.shipment-events",
        //   durable: true,
        //   exclusive: false,
        //   autoDelete: false,
        //   cancellationToken: stoppingToken);

        //await channel.BasicQosAsync(
        //    prefetchSize: 0,
        //    prefetchCount: 1,
        //    global: false,
        //    cancellationToken: stoppingToken);

        //var consumer = new AsyncEventingBasicConsumer(channel);

        //consumer.ReceivedAsync += async (_, ea) =>
        //{
        //    try
        //    {
        //        var messageId = ea.BasicProperties.MessageId;

        //        if (string.IsNullOrWhiteSpace(messageId))
        //        {
        //            throw new JsonException(
        //                "RabbitMQ message does not contain a MessageId.");
        //        }

        //        var payload = Encoding.UTF8.GetString(ea.Body.ToArray());

        //        await using var scope = _scopeFactory.CreateAsyncScope();

        //        var context = scope.ServiceProvider
        //            .GetRequiredService<IApplicationDbContext>();

        //        var alreadyProcessed = await context.ProcessedMessages
        //            .AnyAsync(x => x.MessageId == messageId, stoppingToken);

        //        if (alreadyProcessed)
        //        {
        //            _logger.LogInformation(
        //                "Duplicate message detected. MessageId: {MessageId}",
        //                messageId);

        //            await channel.BasicAckAsync(
        //                deliveryTag: ea.DeliveryTag,
        //                multiple: false,
        //                cancellationToken: stoppingToken);

        //            return;
        //        }

        //        var shipmentEvent =
        //            JsonSerializer.Deserialize<ShipmentCreatedEvent>(payload);

        //        if (shipmentEvent is null)
        //        {
        //            throw new JsonException(
        //                "ShipmentCreatedEvent payload is empty.");
        //        }

        //        _logger.LogInformation(
        //            "Shipment created event received. ShipmentId: {ShipmentId}, TrackingNumber: {TrackingNumber}, Status: {Status}",
        //            shipmentEvent.ShipmentId,
        //            shipmentEvent.TrackingNumber,
        //            shipmentEvent.Status);

        //        await channel.BasicAckAsync(
        //            deliveryTag: ea.DeliveryTag,
        //            multiple: false,
        //            cancellationToken: stoppingToken);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(
        //            ex,
        //            "Failed to process shipment created event.");

        //        await channel.BasicNackAsync(
        //            deliveryTag: ea.DeliveryTag,
        //            multiple: false,
        //            requeue: false,
        //            cancellationToken: stoppingToken);
        //    }
        //};

        //await channel.BasicConsumeAsync(
        //   queue: "cargo.shipment-events",
        //   autoAck: false,
        //   consumer: consumer,
        //   cancellationToken: stoppingToken);

        //_logger.LogInformation(
        //    "ShipmentCreatedConsumer is listening to cargo.shipment-events.");

        //await Task.Delay(Timeout.Infinite, stoppingToken);


        // IDEMPOTENCY SONRASI GÜNCEL KOD
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password
        };

        await using var connection =
            await factory.CreateConnectionAsync(stoppingToken);

        await using var channel =
            await connection.CreateChannelAsync(
                cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: "cargo.shipment-events",
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                // 1. RabbitMQ mesajının benzersiz kimliğini alıyoruz.
                var messageId = ea.BasicProperties.MessageId;

                if (string.IsNullOrWhiteSpace(messageId))
                {
                    throw new JsonException(
                        "RabbitMQ message does not contain a MessageId.");
                }

                // 2. Mesajın JSON içeriğini okuyoruz.
                var payload = Encoding.UTF8.GetString(ea.Body.ToArray());

                var shipmentEvent =
                    JsonSerializer.Deserialize<ShipmentCreatedEvent>(payload);

                if (shipmentEvent is null)
                {
                    throw new JsonException(
                        "ShipmentCreatedEvent payload is empty.");
                }

                // 3. Scoped DbContext almak için yeni bir scope oluşturuyoruz.
                await using var scope = _scopeFactory.CreateAsyncScope();

                var context = scope.ServiceProvider
                    .GetRequiredService<IApplicationDbContext>();

                // 4. Mesajı daha önce işlenmiş olarak kaydetmeye çalışıyoruz.
                context.ProcessedMessages.Add(
                    new ProcessedMessage(messageId));

                try
                {
                    await context.SaveChangesAsync(stoppingToken);
                }
                catch (DbUpdateException ex)
                    when (IsDuplicateMessage(ex))
                {
                    // 5. Mesaj daha önce kaydedilmişse tekrar işlemiyoruz.
                    _logger.LogInformation(
                        "Duplicate message detected. MessageId: {MessageId}",
                        messageId);

                    await channel.BasicAckAsync(
                        ea.DeliveryTag,
                        false,
                        stoppingToken);

                    return;
                }

                // 6. İlk kez gelen mesajın işlenmesini logluyoruz.
                _logger.LogInformation(
                    "Shipment created event processed. MessageId: {MessageId}, ShipmentId: {ShipmentId}, TrackingNumber: {TrackingNumber}, Status: {Status}",
                    messageId,
                    shipmentEvent.ShipmentId,
                    shipmentEvent.TrackingNumber,
                    shipmentEvent.Status);

                // 7. Başarılı işlem sonrası RabbitMQ'ya ACK gönderiyoruz.
                await channel.BasicAckAsync(
                    ea.DeliveryTag,
                    false,
                    stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process shipment created event.");

                // İşlem başarısızsa mesajı yeniden kuyruğa koymadan reddediyoruz.
                await channel.BasicNackAsync(
                    ea.DeliveryTag,
                    false,
                    requeue: false,
                    cancellationToken: stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(
            queue: "cargo.shipment-events",
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation(
            "ShipmentCreatedConsumer is listening to cargo.shipment-events.");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
    // SQL Server unique index ihlalini tespit eder.
    private static bool IsDuplicateMessage(DbUpdateException exception)
    {
        for (Exception? current = exception;
             current is not null;
             current = current.InnerException)
        {
            if (current is SqlException sqlException
                && sqlException.Number is 2601 or 2627)
            {
                return true;
            }
        }

        return false;
    }
}



