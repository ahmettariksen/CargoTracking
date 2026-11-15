using Microsoft.Extensions.Hosting;
using Org.BouncyCastle.Crypto.Macs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Infrastructure.Settings;

// bu sınıf kuryelere mail gönderen ana mail gönderim merkezi gibi bişey
//Bu sınıf SMTP bağlantı bilgilerini taşıyacak.
//- Host: Gmail SMTP sunucusu.
//- Port: 587.
//- UserName: Gmail adresin.
//- Password: Gmail uygulama şifren.
//- FromEmail: Gönderici e-posta adresi.
//- FromName: E-postada görünecek uygulama adı.
public sealed class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}
