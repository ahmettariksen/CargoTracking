using CargoTracking.Application.Events;
using CargoTracking.Application.Interfaces;
using CargoTracking.Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CargoTracking.Infrastructure.Messaging.Cunsomers;
public sealed class CourierAssignmentEmailConsumer : BackgroundService
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<CourierAssignmentEmailConsumer> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public CourierAssignmentEmailConsumer(IOptions<RabbitMqOptions> options, ILogger<CourierAssignmentEmailConsumer> logger, IServiceScopeFactory scopeFactory)
    {
        _options = options.Value;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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

        const string queueName = "cargo.courier-emails";

        const string deadLetterExchange = "cargo.dead-letter";

        const string deadLetterQueue = "cargo.courier-emails.dead";

        const string deadLetterRoutingKey = "courier-emails.dead";

        // Dead Letter Exchange oluşturulur.
        await channel.ExchangeDeclareAsync(
            exchange: deadLetterExchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        // Dead Letter Queue oluşturulur.
        await channel.QueueDeclareAsync(
            queue: deadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        // Dead Letter Queue, Exchange'e bağlanır.
        await channel.QueueBindAsync(
            queue: deadLetterQueue,
            exchange: deadLetterExchange,
            routingKey: deadLetterRoutingKey,
            cancellationToken: stoppingToken);

        // Ana kuyruğun Dead Letter ayarları.
        var queueArguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = deadLetterExchange,
            ["x-dead-letter-routing-key"] = deadLetterRoutingKey
        };

        // Ana kuyruk oluşturulur.
        await channel.QueueDeclareAsync(
           queue: queueName,
           durable: true,
           exclusive: false,
           autoDelete: false,
           arguments:queueArguments,
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
                var payload = Encoding.UTF8.GetString(
                    ea.Body.ToArray());

                var emailEvent =
                    JsonSerializer.Deserialize<CourierAssignmentEmailEvent>(
                        payload);

                if (emailEvent is null)
                {
                    throw new JsonException(
                        "CourierAssignmentEmailEvent payload is empty.");
                }

                await using var scope =
                    _scopeFactory.CreateAsyncScope();

                var emailService = scope.ServiceProvider
                    .GetRequiredService<IEmailService>();

                var address = WebUtility.HtmlEncode(
                    emailEvent.Address);

                var warehouseName = WebUtility.HtmlEncode(
                    emailEvent.WarehouseName);

                var warehouseAddress = WebUtility.HtmlEncode(
                    emailEvent.WarehouseAddress);

                var body = $"""
                    <h2>Kargo Alım Bilgilendirmesi</h2>
                    <p>Size yeni bir kargo alım görevi atanmıştır.</p>
                    <p><strong>Takip Numarası:</strong> {emailEvent.TrackingNumber}</p>
                    <p><strong>Adres:</strong> {address}</p>
                    <p><strong>Götürülecek Depo:</strong> {warehouseName}</p>
                    <p><strong>Depo Adresi:</strong> {warehouseAddress}</p>
                    """;

                await emailService.SendEmailAsync(
                    emailEvent.Email,
                    "Yeni Kargo Alım Görevi",
                    body,
                    stoppingToken);

                await channel.BasicAckAsync(
                    deliveryTag: ea.DeliveryTag,
                    multiple: false,
                    cancellationToken: stoppingToken);

                _logger.LogInformation(
                    "Courier assignment email sent. TrackingNumber: {TrackingNumber}",
                    emailEvent.TrackingNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send courier assignment email.");

                await channel.BasicNackAsync(
                    deliveryTag: ea.DeliveryTag,
                    multiple: false,
                    requeue: false,
                    cancellationToken: stoppingToken);
            }
        };
        await channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation(
            "CourierAssignmentEmailConsumer is listening to {QueueName}.",
            queueName);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
