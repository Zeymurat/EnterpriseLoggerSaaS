using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseLogger.Api.IntegrationTests.Infrastructure;
using EnterpriseLogger.Application.Common.Constants;
using Xunit;

namespace EnterpriseLogger.Api.IntegrationTests.Logs;

public class LogTenantIsolationTests : IClassFixture<EnterpriseLoggerWebApplicationFactory>
{
    private readonly HttpClient _client;

    public LogTenantIsolationTests(EnterpriseLoggerWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetLogs_ReturnsOnlyLogsForAuthenticatedTenant()
    {
        var sahraKey = await CreateTenantWithApiKeyAsync("Sahra Telekom");
        var acmeKey = await CreateTenantWithApiKeyAsync("Acme Corp");

        await IntegrationTestAuth.CreateLogAsync(_client, sahraKey, "BillingApi", "Error", "Sahra log 1");
        await IntegrationTestAuth.CreateLogAsync(_client, sahraKey, "BillingApi", "Warning", "Sahra log 2");
        await IntegrationTestAuth.CreateLogAsync(_client, acmeKey, "InventoryApi", "Info", "Acme log 1");

        var sahraLogs = await GetLogsAsync(sahraKey);
        Assert.Equal(2, sahraLogs.Count);
        Assert.All(sahraLogs, log => Assert.Contains("Sahra", log.GetProperty("message").GetString()));

        var acmeLogs = await GetLogsAsync(acmeKey);
        Assert.Single(acmeLogs);
        Assert.Equal("Acme log 1", acmeLogs[0].GetProperty("message").GetString());

        var unauthenticated = await _client.GetAsync("/api/logs");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);
    }

    [Fact]
    public async Task RotateApiKey_ShouldReturnServerGeneratedApiKey()
    {
        var apiKey = await CreateTenantWithApiKeyAsync("Zeymurat");

        Assert.StartsWith("EL_", apiKey);
        Assert.True(apiKey.Length >= 10);
    }

    private async Task<string> CreateTenantWithApiKeyAsync(string name)
    {
        return await IntegrationTestAuth.RegisterLoginAndRotateApiKeyAsync(_client, name);
    }

    private async Task<List<JsonElement>> GetLogsAsync(string apiKey)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/logs");
        request.Headers.Add(TenantAuthConstants.ApiKeyHeaderName, apiKey);

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var data = doc!.RootElement.GetProperty("data");

        return data.GetProperty("items").EnumerateArray().Select(e => e.Clone()).ToList();
    }
}
