using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseLogger.Api.IntegrationTests.Infrastructure;
using EnterpriseLogger.Application.Common.Constants;
using Xunit;

namespace EnterpriseLogger.Api.IntegrationTests.Logs;

public class LogPaginationTests : IClassFixture<EnterpriseLoggerWebApplicationFactory>
{
    private readonly HttpClient _client;

    public LogPaginationTests(EnterpriseLoggerWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetLogs_WithPagination_ReturnsPageAndSummary()
    {
        var apiKey = await IntegrationTestAuth.RegisterLoginAndRotateApiKeyAsync(_client, "Pagination Corp");

        for (var i = 1; i <= 25; i++)
        {
            await IntegrationTestAuth.CreateLogAsync(
                _client,
                apiKey,
                "PagerApp",
                "Info",
                $"Pagination log {i}");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/logs?page=2&pageSize=10");
        request.Headers.Add(TenantAuthConstants.ApiKeyHeaderName, apiKey);

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var data = doc!.RootElement.GetProperty("data");

        var items = data.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(10, items.Count);
        Assert.Equal(2, data.GetProperty("page").GetInt32());
        Assert.Equal(10, data.GetProperty("pageSize").GetInt32());
        Assert.Equal(25, data.GetProperty("totalCount").GetInt32());
        Assert.All(items, log => Assert.Equal("Info", log.GetProperty("logLevel").GetString()));

        var summary = data.GetProperty("summary");
        Assert.Equal(25, summary.GetProperty("total").GetInt32());
        Assert.Equal(25, summary.GetProperty("info").GetInt32());

        var filters = data.GetProperty("availableFilters");
        Assert.NotEmpty(filters.GetProperty("applicationNames").EnumerateArray());
    }

    [Fact]
    public async Task GetLogs_WithSearch_FiltersMessages()
    {
        var apiKey = await IntegrationTestAuth.RegisterLoginAndRotateApiKeyAsync(_client, "Search Corp");

        await IntegrationTestAuth.CreateLogAsync(_client, apiKey, "BillingApi", "Error", "Payment timeout");
        await IntegrationTestAuth.CreateLogAsync(_client, apiKey, "BillingApi", "Info", "Payment success");
        await IntegrationTestAuth.CreateLogAsync(_client, apiKey, "AuthApi", "Warning", "Login failed");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/logs?search=payment");
        request.Headers.Add(TenantAuthConstants.ApiKeyHeaderName, apiKey);

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var items = doc!.RootElement.GetProperty("data").GetProperty("items").EnumerateArray().ToList();

        Assert.Equal(2, items.Count);
        Assert.All(items, log =>
            Assert.Contains("Payment", log.GetProperty("message").GetString(), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetLogs_WithMultiFilters_ReturnsMatchingSubset()
    {
        var apiKey = await IntegrationTestAuth.RegisterLoginAndRotateApiKeyAsync(_client, "Multi Filter Corp");

        await IntegrationTestAuth.CreateLogAsync(_client, apiKey, "BillingApi", "Error", "Charge failed", httpMethod: "POST", statusCode: 402);
        await IntegrationTestAuth.CreateLogAsync(_client, apiKey, "BillingApi", "Info", "Charge ok", httpMethod: "POST", statusCode: 200);
        await IntegrationTestAuth.CreateLogAsync(_client, apiKey, "AuthApi", "Error", "Login failed", httpMethod: "GET", statusCode: 401);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/logs?applicationNames=BillingApi&httpMethods=POST&statusCodes=402&logLevels=Error");
        request.Headers.Add(TenantAuthConstants.ApiKeyHeaderName, apiKey);

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var items = doc!.RootElement.GetProperty("data").GetProperty("items").EnumerateArray().ToList();

        Assert.Single(items);
        Assert.Equal("Charge failed", items[0].GetProperty("message").GetString());
    }

    [Fact]
    public async Task GetLogs_WithDateFilter_ReturnsOverallAndFilteredSummaries()
    {
        var apiKey = await IntegrationTestAuth.RegisterLoginAndRotateApiKeyAsync(_client, "Date Summary Corp");

        for (var i = 1; i <= 5; i++)
            await IntegrationTestAuth.CreateLogAsync(_client, apiKey, "App", "Info", $"Log {i}");

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/logs?from={DateTime.UtcNow.AddDays(-1):O}&to={DateTime.UtcNow.AddDays(1):O}");
        request.Headers.Add(TenantAuthConstants.ApiKeyHeaderName, apiKey);

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var data = doc!.RootElement.GetProperty("data");

        Assert.True(data.GetProperty("isDateFiltered").GetBoolean());
        Assert.Equal(5, data.GetProperty("overallSummary").GetProperty("total").GetInt32());
        Assert.Equal(5, data.GetProperty("summary").GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task GetLogs_WithoutDateFilter_DoesNotReturnOverallSummary()
    {
        var apiKey = await IntegrationTestAuth.RegisterLoginAndRotateApiKeyAsync(_client, "No Date Filter Corp");
        await IntegrationTestAuth.CreateLogAsync(_client, apiKey, "App", "Info", "Log without date filter");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/logs");
        request.Headers.Add(TenantAuthConstants.ApiKeyHeaderName, apiKey);

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var data = doc!.RootElement.GetProperty("data");

        Assert.False(data.GetProperty("isDateFiltered").GetBoolean());
        Assert.Equal(JsonValueKind.Null, data.GetProperty("overallSummary").ValueKind);
    }

    [Fact]
    public async Task ExportLogs_WithFilters_ReturnsCsv()
    {
        var apiKey = await IntegrationTestAuth.RegisterLoginAndRotateApiKeyAsync(_client, "Export Corp");

        await IntegrationTestAuth.CreateLogAsync(_client, apiKey, "BillingApi", "Error", "Export me", httpMethod: "POST", statusCode: 500);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/logs/export?logLevels=Error&search=export");
        request.Headers.Add(TenantAuthConstants.ApiKeyHeaderName, apiKey);

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        Assert.Equal("text/csv; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        Assert.Equal("false", response.Headers.GetValues("X-Export-Truncated").Single());
        Assert.Equal("1", response.Headers.GetValues("X-Export-Count").Single());

        var csv = await response.Content.ReadAsStringAsync();
        Assert.Contains("Export me", csv);
        Assert.Contains("BillingApi", csv);
    }
}
