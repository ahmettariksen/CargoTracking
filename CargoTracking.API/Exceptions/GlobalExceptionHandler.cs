using CargoTracking.Application.sda.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CargoTracking.API.Exceptions;
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        //ArgumentException: Geçersiz parametreler için 400 döndürüyor.
        //InvalidOperationException: İş kuralı ihlalleri için 400 döndürüyor.
        //Diğer beklenmeyen hatalar: 500 döndürüyor ve ayrıntıları yalnızca sunucu loglarına yazıyor.
        //ProblemDetails: API hatalarını standart bir JSON formatında sunuyor
        var (statusCode, title, detail) = exception switch
        {
            FluentValidation.ValidationException validationException =>
            (
            StatusCodes.Status400BadRequest,
            "Doğrulama Hatası", string.Join(
                " ", validationException.Errors.Select(error => error.ErrorMessage))
            ),
            ForbiddenAccessException =>
            (StatusCodes.Status403Forbidden,
            "Erişim engellendi",
            exception.Message),

            UnauthorizedAccessException =>
            (StatusCodes.Status401Unauthorized,
            "Yetkisiz erişim",
             exception.Message),

            ArgumentException =>
                (StatusCodes.Status400BadRequest,
                 "Geçersiz istek",
                 exception.Message),

            InvalidOperationException =>
                (StatusCodes.Status400BadRequest,
                 "İşlem gerçekleştirilemedi",
                 exception.Message),

            // concurrency hata mesajı 409 dur.
            DbUpdateConcurrencyException =>
            (StatusCodes.Status409Conflict,
            "Concurrency Conflict",
            "Kayıt başka bir işlem tarafından değiştirildi. Lütfen tekrar deneyin."),

            _ =>
                (StatusCodes.Status500InternalServerError,
                 "Sunucu hatası",
                 "Beklenmeyen bir hata oluştu.")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "Beklenmeyen bir hata oluştu.");
        }
        else
        {
            _logger.LogWarning(
                exception,
                "İstek işlenemedi: {Message}",
                exception.Message);
        }

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail
            },
            cancellationToken);

        return true;
    }
}
