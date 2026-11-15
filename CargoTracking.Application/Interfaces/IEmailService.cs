using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Application.Interfaces;
public interface IEmailService
{
    //Bu arayüzün görevi e-posta gönderme işlemini soyutlamak. SMTP detayları Application katmanında bulunmayacak.
    Task SendEmailAsync(
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken = default);
}
