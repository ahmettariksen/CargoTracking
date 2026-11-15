using CargoTracking.Application.Interfaces;
using CargoTracking.Infrastructure.Settings;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace CargoTracking.Infrastructure.Services;

//Gönderici ve alıcı bilgilerini belirlemek.
//HTML formatında e-posta içeriği oluşturmak.
//Gmail SMTP sunucusuna bağlanmak.
//Kimlik doğrulaması yapmak.
//E-postayı göndermek.
public sealed class SmtpEmailService(IOptions<SmtpSettings> smtpOptions) : IEmailService
{
    private readonly SmtpSettings _settings = smtpOptions.Value;
    public async Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(
            _settings.FromName,
            _settings.FromEmail));

        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html")
        {
            Text = body
        };

        using var client = new SmtpClient();

        await client.ConnectAsync(
            _settings.Host,
            _settings.Port,
            SecureSocketOptions.StartTls,
            cancellationToken);

        await client.AuthenticateAsync(
           _settings.UserName,
           _settings.Password,
           cancellationToken);

        await client.SendAsync(message, cancellationToken);

        await client.DisconnectAsync(true, cancellationToken);
    }
}
