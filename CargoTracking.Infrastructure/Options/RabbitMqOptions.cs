using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Infrastructure.Options;

// Bu sınıf neden gerekli? appsettings.json içindeki ayarları tip güvenli bir C# nesnesine bağlayacağız. Böylece bağlantı bilgilerini her yerde elle okumak zorunda kalmayacağız
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
}
