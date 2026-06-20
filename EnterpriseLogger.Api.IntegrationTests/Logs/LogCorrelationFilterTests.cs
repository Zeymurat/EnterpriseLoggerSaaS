using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseLogger.Api.IntegrationTests.Infrastructure;
using EnterpriseLogger.Application.Common.Constants;
using Xunit;

namespace EnterpriseLogger.Api.IntegrationTests.Logs;

public class LogCorrelationFilterTests : IClassFixture<EnterpriseLoggerWebApplicationFactory>
{
    private readonly HttpClient _client;

    public LogCorrelationFilterTests(EnterpriseLoggerWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetLogs_WithCorrelationId_ReturnsOnlyMatchingTraceChain()
    {
        const string traceId = "trace-integration-abc-123";
        var apiKey = await IntegrationTestAuth.RegisterLoginAndRotateApiKeyAsync(_client, "Trace Corp");

        await IntegrationTestAuth.CreateLogAsync(_client, apiKey, "AuthService", "Info", "Login ok", correlationId: traceId);
        await IntegrationTestAuth.CreateLogAsync(_client, apiKey, "BillingService", "Warning", "Slow charge", correlationId: traceId);
        await IntegrationTestAuth.CreateLogAsync(_client, apiKey, "DbService", "Error", "Timeout", correlationId: traceId);
        await IntegrationTestAuth.CreateLogAsync(_client, apiKey, "OtherApp", "Info", "Unrelated log");

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/logs?correlationId={traceId}");
        request.Headers.Add(TenantAuthConstants.ApiKeyHeaderName, apiKey);

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var data = doc!.RootElement.GetProperty("data");

        var items = data.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(3, items.Count);
        Assert.Equal(3, data.GetProperty("totalCount").GetInt32());
        Assert.All(items, log => Assert.Equal(traceId, log.GetProperty("correlationId").GetString()));

        var apps = items.Select(log => log.GetProperty("applicationName").GetString()).OrderBy(x => x).ToList();
        Assert.Equal(["AuthService", "BillingService", "DbService"], apps);
    }

    [Fact]
    public async Task GetLogs_WithSharedCorrelationId_ExposesCorrelationLogCountOnListItems()
    {
        const string traceId = "trace-list-count-001";
        var apiKey = await IntegrationTestAuth.RegisterLoginAndRotateApiKeyAsync(_client, "Trace Count Corp");

        await IntegrationTestAuth.CreateLogAsync(_client, apiKey, "AuthService", "Info", "Step 1", correlationId: traceId);
        await IntegrationTestAuth.CreateLogAsync(_client, apiKey, "BillingService", "Warning", "Step 2", correlationId: traceId);
        await IntegrationTestAuth.CreateLogAsync(_client, apiKey, "OtherApp", "Info", "Unrelated log");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/logs?pageSize=10");
        request.Headers.Add(TenantAuthConstants.ApiKeyHeaderName, apiKey);

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var items = doc!.RootElement.GetProperty("data").GetProperty("items").EnumerateArray().ToList();

        var tracedItems = items
            .Where(item => item.GetProperty("correlationId").GetString() == traceId)
            .ToList();

        Assert.Equal(2, tracedItems.Count);
        Assert.All(tracedItems, item =>
            Assert.Equal(2, item.GetProperty("correlationLogCount").GetInt32()));
    }
}
