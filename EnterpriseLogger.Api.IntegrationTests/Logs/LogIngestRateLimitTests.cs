using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseLogger.Api.IntegrationTests.Infrastructure;
using EnterpriseLogger.Application.Common.Constants;
using Xunit;

namespace EnterpriseLogger.Api.IntegrationTests.Logs;

public class LogIngestRateLimitTests
{
    [Fact]
    public async Task PostLogs_ExceedingTenantLimit_Returns429WithProblemDetails()
    {
        await using var factory = new LowRateLimitWebApplicationFactory { RequestsPerWindow = 3 };
        using var client = factory.CreateClient();

        var apiKey = await IntegrationTestAuth.RegisterLoginAndRotateApiKeyAsync(client, "RateLimit Tenant");

        for (var i = 0; i < 3; i++)
        {
            var allowed = await PostLogAsync(client, apiKey, $"Allowed log {i}");
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        }

        var limited = await PostLogAsync(client, apiKey, "Should be rate limited");
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.True(limited.Headers.TryGetValues("Retry-After", out _));

        using var doc = await limited.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal("İstek limiti aşıldı", doc!.RootElement.GetProperty("title").GetString());
        Assert.Contains(
            "limiti aşıldı",
            doc.RootElement.GetProperty("detail").GetString(),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostLogs_RateLimit_IsScopedPerTenant()
    {
        await using var factory = new LowRateLimitWebApplicationFactory { RequestsPerWindow = 2 };
        using var client = factory.CreateClient();

        var tenantAKey = await IntegrationTestAuth.RegisterLoginAndRotateApiKeyAsync(client, "Tenant A Rate");
        var tenantBKey = await IntegrationTestAuth.RegisterLoginAndRotateApiKeyAsync(client, "Tenant B Rate");

        Assert.Equal(HttpStatusCode.OK, (await PostLogAsync(client, tenantAKey, "A1")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await PostLogAsync(client, tenantAKey, "A2")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await PostLogAsync(client, tenantAKey, "A3")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await PostLogAsync(client, tenantBKey, "B1")).StatusCode);
    }

    [Fact]
    public async Task GetLogs_IsNotRateLimited()
    {
        await using var factory = new LowRateLimitWebApplicationFactory { RequestsPerWindow = 1 };
        using var client = factory.CreateClient();

        var apiKey = await IntegrationTestAuth.RegisterLoginAndRotateApiKeyAsync(client, "Read Not Limited");

        Assert.Equal(HttpStatusCode.OK, (await PostLogAsync(client, apiKey, "Only ingest")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await PostLogAsync(client, apiKey, "Second ingest")).StatusCode);

        for (var i = 0; i < 5; i++)
        {
            using var readRequest = new HttpRequestMessage(HttpMethod.Get, "/api/logs");
            readRequest.Headers.Add(TenantAuthConstants.ApiKeyHeaderName, apiKey);
            var response = await client.SendAsync(readRequest);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    private static async Task<HttpResponseMessage> PostLogAsync(
        HttpClient client,
        string apiKey,
        string message)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/logs");
        request.Headers.Add(TenantAuthConstants.ApiKeyHeaderName, apiKey);
        request.Content = JsonContent.Create(new
        {
            applicationName = "RateLimitTest",
            logLevel = "Info",
            message
        });

        return await client.SendAsync(request);
    }
}
