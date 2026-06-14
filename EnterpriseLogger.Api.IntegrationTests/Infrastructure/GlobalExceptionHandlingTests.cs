using System.Net;
using System.Text.Json;
using EnterpriseLogger.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace EnterpriseLogger.Api.IntegrationTests.Infrastructure;

public class GlobalExceptionHandlingTests : IClassFixture<EnterpriseLoggerWebApplicationFactory>
{
    private readonly HttpClient _client;

    public GlobalExceptionHandlingTests(EnterpriseLoggerWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UnhandledException_ReturnsRfc7807ProblemDetails()
    {
        var response = await _client.GetAsync("/api/_debug/throw");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("problem+json", response.Content.Headers.ContentType?.MediaType ?? "");

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.Equal(500, root.GetProperty("status").GetInt32());
        Assert.Equal("Sunucu hatası", root.GetProperty("title").GetString());
        Assert.True(root.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task MissingApiKey_ReturnsProblemDetailsUnauthorized()
    {
        var response = await _client.GetAsync("/api/logs");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("problem+json", response.Content.Headers.ContentType?.MediaType ?? "");

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(401, doc.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Yetkisiz erişim", doc.RootElement.GetProperty("title").GetString());
    }
}
