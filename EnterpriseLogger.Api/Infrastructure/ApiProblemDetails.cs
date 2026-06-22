using Microsoft.AspNetCore.Mvc;

namespace EnterpriseLogger.Api.Infrastructure;

/// <summary>
/// Middleware ve handler'ların aynı RFC 7807 formatını kullanması için yardımcı.
/// </summary>
public static class ApiProblemDetails
{
    public static IResult Unauthorized(string detail) =>
        Results.Problem(
            title: "Yetkisiz erişim",
            detail: detail,
            statusCode: StatusCodes.Status401Unauthorized,
            type: "https://tools.ietf.org/html/rfc9110#section-15.5.2");

    public static IResult Forbidden(string detail) =>
        Results.Problem(
            title: "Erişim engellendi",
            detail: detail,
            statusCode: StatusCodes.Status403Forbidden,
            type: "https://tools.ietf.org/html/rfc9110#section-15.5.4");

    public static IResult TooManyRequests(string detail) =>
        Results.Problem(
            title: "İstek limiti aşıldı",
            detail: detail,
            statusCode: StatusCodes.Status429TooManyRequests,
            type: "https://tools.ietf.org/html/rfc9110#section-15.5.1");

    public static async Task WriteAsync(HttpContext context, IResult problemResult) =>
        await problemResult.ExecuteAsync(context);
}
