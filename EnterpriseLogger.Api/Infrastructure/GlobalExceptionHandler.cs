using Microsoft.AspNetCore.Diagnostics;

namespace EnterpriseLogger.Api.Infrastructure;

/// <summary>
/// Yakalanmamış tüm exception'ları RFC 7807 Problem Details formatında döner.
/// Frontend her zaman application/problem+json bekleyebilir.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IHostEnvironment _environment;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IHostEnvironment environment, ILogger<GlobalExceptionHandler> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Yakalanmamış hata: {Path}", httpContext.Request.Path);

        var (status, title, detail) = MapException(exception);

        var extensions = new Dictionary<string, object?>
        {
            ["traceId"] = httpContext.TraceIdentifier
        };

        if (_environment.IsDevelopment())
            extensions["exceptionDetail"] = exception.ToString();

        var problem = Results.Problem(
            title: title,
            detail: _environment.IsDevelopment() ? exception.Message : detail,
            statusCode: status,
            type: status switch
            {
                StatusCodes.Status400BadRequest => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                StatusCodes.Status404NotFound => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                _ => "https://tools.ietf.org/html/rfc9110#section-15.6.1"
            },
            extensions: extensions);

        await problem.ExecuteAsync(httpContext);
        return true;
    }

    private static (int Status, string Title, string Detail) MapException(Exception exception) =>
        exception switch
        {
            ArgumentException => (
                StatusCodes.Status400BadRequest,
                "Geçersiz istek",
                "İstek geçersiz parametreler içeriyor."),
            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "Kayıt bulunamadı",
                "İstenen kaynak bulunamadı."),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Sunucu hatası",
                "İşleminiz gerçekleştirilirken sistemsel bir hata oluştu.")
        };
}
