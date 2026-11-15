using CargoTracking.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CargoTracking.API.Controllers;

//[ApiController]
//[Route("api/[controller]")]
//public class EmailTestController(
//    IEmailService emailService):ControllerBase
//{
//    [HttpPost]
//    public async Task<IActionResult> SendTestEmail([FromQuery] string to, CancellationToken cancellationToken)
//    {
//        await emailService.SendEmailAsync(
//            to,
//            "CargoTracking SMTP Test",
//            "<h2>SMTP bağlantısı başarılı!</h2><p>E-posta gönderimi çalışıyor.</p>",
//            cancellationToken);

//        return Ok("Test e-postası gönderildi.");
//    }
//}
