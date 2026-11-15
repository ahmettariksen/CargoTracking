using CargoTracking.Application.Features.Shipments.Events;
using CargoTracking.Infrastructure.Options;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Security.Policy;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CargoTracking.Infrastructure.Messaging;
//Exchange: cargo.events mesajları yönlendirir.
//Queue: cargo.shipment-events mesajları bekletir.
//Routing key: ShipmentCreatedEvent gibi mesaj türlerini ayırt eder.
//Persistent: Mesajın kalıcı olarak işaretlenmesini sağlar.Bunun tek başına teslim garantisi vermediğini unutma; publisher confirms ve uygun broker yapılandırması da gerekir.
//mandatory: true: Mesaj uygun bir kuyruğa yönlendirilemezse sessizce kaybolması yerine publisher'a geri dönmesini sağlar; bunun için return davranışını da doğru ele almalıyız.
public sealed class RabbitMqMessagePublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    private IConnection? _connection;


    public RabbitMqMessagePublisher(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
    }

    public async ValueTask DisposeAsync()
    {
        await _connectionLock.WaitAsync();

        try
        {
            if (_connection is not null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }
        }
        finally
        {
            _connectionLock.Release();
            _connectionLock.Dispose();
        }
    }
    private async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        //RabbitMQ bağlantısı ilk mesaj gönderilirken açılır ve sonraki gönderimlerde yeniden kullanılır.
        //Her gönderimde ayrı bir kanal oluşturulur ve işlem bitince kapatılır. Böylece aynı kanal üzerinde eşzamanlı yayın yapmaktan kaçınırız.
        //Bağlantı kapalıysa yeni bir bağlantı açılması denenir.
        //Uygulama kapanırken publisher'ın bağlantısı da kapatılır.

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is null || !_connection.IsOpen)
            {
                if (_connection is not null)
                {
                    await _connection.DisposeAsync();
                    _connection = null;
                }

                var factory = new ConnectionFactory
                {
                    HostName = _options.HostName,
                    Port = _options.Port,
                    UserName = _options.UserName,
                    Password = _options.Password,

                    // Mülakat sorusu=> Soru: RabbitMQ bağlantısı koptuğunda neden hem Automatic Recovery hem de Outbox Retry kullanıyorsun?
                    // Cevap => "RabbitMQ bağlantısı kurulduktan sonra koparsa Automatic Recovery özelliği yeniden bağlantı kurmayı dener. Ancak bağlantı kurulamadığı veya mesaj gönderimi başarısız olduğu durumlarda Outbox Retry mekanizmasını kullanıyorum. Bu mekanizmada başarısız mesajlar belirlediğim bekleme süreleriyle yeniden deneniyor. Beş deneme de başarısız olursa mesaj Dead Letter olarak işaretleniyor. Böylece bağlantı yönetimiyle mesaj gönderim yönetimini birbirinden ayırıyorum."

                    // Bağlantı koptuğunda otomatik yeniden bağlanmayı etkinleştirir.
                    AutomaticRecoveryEnabled = true,

                    // Yeniden bağlantı denemeleri arasındaki süre.
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(5),

                    // Exchange, Queue ve Binding yapılarını kurtarır.
                    TopologyRecoveryEnabled = true
                };

                _connection =
                    await factory.CreateConnectionAsync(cancellationToken);
            }

            return _connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }
    public async Task PublishAsync(Guid messageId, string messageType, string payload, CancellationToken cancellationToken)
    {
        var connection = await GetConnectionAsync(cancellationToken);


        // Şu an publisher mesajı RabbitMQ'ya gönderme çağrısı yapıyor, ancak broker'ın mesajı kabul ettiğini ayrıca doğrulamıyoruz. Outbox kaydının ProcessedAt alanı da publish metodu hata vermeden döndüğünde dolduruluyor.
        //Publisher Confirm ile RabbitMQ'nun mesajı kabul edip etmediğini doğrulayacağız.
        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);

        //Kanal açma
        await using var channel = await connection.CreateChannelAsync(channelOptions,
            cancellationToken: cancellationToken);

        //Eğer rabbitmq sunucusunda bir problem olursa kuyruk ve kuyruktaki mesajlar silinecektir bu durumun önüne geçmek için Message Durability yapılandırması kullanılır bunu için;
        // 1-önce aşağıdaki instance de durable değeri true yapılır aynı şekilde cunsomerda da

        //Queue Exchange tipi belirledik ve ayarlarını Oluşturma
        await channel.ExchangeDeclareAsync(
            exchange: "cargo.events",
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        var queueName = messageType == "CourierAssignmentEmailEvent"
     ? "cargo.courier-emails"
     : "cargo.shipment-events";

        //Queue Oluşturma
        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: queueName,
            exchange: "cargo.events",
            routingKey: messageType,
            cancellationToken: cancellationToken);

        //Queue' ye Mesaj Gönderme
        //RabbitMQ kuyruğa atacağım mesajları byte türünden kabul etmektedir.
        var body = Encoding.UTF8.GetBytes(payload);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            Type = messageType,
            MessageId = messageId.ToString()
        };

        //2- publish edilecek mesajın kalıcılığı ise basicproperties üzerinden yapılır bu da 
        await channel.BasicPublishAsync(
            exchange: "cargo.events",
            routingKey: messageType,
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }
}
