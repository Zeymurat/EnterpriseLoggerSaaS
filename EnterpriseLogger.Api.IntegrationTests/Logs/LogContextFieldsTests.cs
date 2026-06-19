using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseLogger.Api.IntegrationTests.Infrastructure;
using EnterpriseLogger.Application.Common.Constants;
using Xunit;

namespace EnterpriseLogger.Api.IntegrationTests.Logs;

public class LogContextFieldsTests : IClassFixture<EnterpriseLoggerWebApplicationFactory>
{
    private readonly HttpClient _client;

    public LogContextFieldsTests(EnterpriseLoggerWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateLog_WithRequestContext_PersistsAndReturnsFields()
    {
        var apiKey = await IntegrationTestAuth.RegisterLoginAndRotateApiKeyAsync(_client, "Log Context Corp");

        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/logs");
        createRequest.Headers.Add(TenantAuthConstants.ApiKeyHeaderName, apiKey);
        createRequest.Content = JsonContent.Create(new
        {
            applicationName = "CheckoutApi",
            logLevel = "Error",
            message = "Payment provider timeout after 30s",
            httpMethod = "POST",
            requestPath = "/api/checkout",
            statusCode = 504,
            correlationId = "corr-integration-001",
            actorIdentifier = "buyer@example.com",
            exceptionType = "TimeoutException"
        });

        var createResponse = await _client.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        using var createDoc = await createResponse.Content.ReadFromJsonAsync<JsonDocument>();
        var created = createDoc!.RootElement.GetProperty("data");

        Assert.Equal("POST", created.GetProperty("httpMethod").GetString());
        Assert.Equal("/api/checkout", created.GetProperty("requestPath").GetString());
        Assert.Equal(504, created.GetProperty("statusCode").GetInt32());
        Assert.Equal("corr-integration-001", created.GetProperty("correlationId").GetString());
        Assert.Equal("buyer@example.com", created.GetProperty("actorIdentifier").GetString());
        Assert.Equal("TimeoutException", created.GetProperty("exceptionType").GetString());

        using var listRequest = new HttpRequestMessage(HttpMethod.Get, "/api/logs");
        listRequest.Headers.Add(TenantAuthConstants.ApiKeyHeaderName, apiKey);

        var listResponse = await _client.SendAsync(listRequest);
        listResponse.EnsureSuccessStatusCode();

        using var listDoc = await listResponse.Content.ReadFromJsonAsync<JsonDocument>();
        var log = listDoc!.RootElement.GetProperty("data").EnumerateArray().Single();

        Assert.Equal("POST", log.GetProperty("httpMethod").GetString());
        Assert.Equal("/api/checkout", log.GetProperty("requestPath").GetString());
    }

    [Fact]
    public async Task CreateLog_WithoutOptionalContext_RemainsBackwardCompatible()
    {
        var apiKey = await IntegrationTestAuth.RegisterLoginAndRotateApiKeyAsync(_client, "Log Legacy Corp");

        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/logs");
        createRequest.Headers.Add(TenantAuthConstants.ApiKeyHeaderName, apiKey);
        createRequest.Content = JsonContent.Create(new
        {
            applicationName = "LegacyApp",
            logLevel = "Info",
            message = "Simple log without context"
        });

        var createResponse = await _client.SendAsync(createRequest);
        createResponse.EnsureSuccessStatusCode();

        using var doc = await createResponse.Content.ReadFromJsonAsync<JsonDocument>();
        var data = doc!.RootElement.GetProperty("data");

        Assert.Equal(JsonValueKind.Null, data.GetProperty("httpMethod").ValueKind);
        Assert.Equal(JsonValueKind.Null, data.GetProperty("requestPath").ValueKind);
    }
}
