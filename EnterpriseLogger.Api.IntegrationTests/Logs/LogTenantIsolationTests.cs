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
        var sahraKey = await CreateTenantAsync("Sahra Telekom");
        var acmeKey = await CreateTenantAsync("Acme Corp");

        await CreateLogAsync(sahraKey, "BillingApi", "Error", "Sahra log 1");
        await CreateLogAsync(sahraKey, "BillingApi", "Warning", "Sahra log 2");
        await CreateLogAsync(acmeKey, "InventoryApi", "Info", "Acme log 1");

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
    public async Task CreateTenant_ShouldReturnServerGeneratedApiKey()
    {
        var apiKey = await CreateTenantAsync("Zeymurat");

        Assert.StartsWith("EL_", apiKey);
        Assert.True(apiKey.Length >= 10);
    }

    private async Task<string> CreateTenantAsync(string name)
    {
        var ownerEmail = $"{Guid.NewGuid():N}@test.com";
        var response = await _client.PostAsJsonAsync("/api/tenants", new
        {
            name,
            ownerEmail,
            ownerPhone = "05551234567",
            ownerPassword = "TestPass123"
        });
        response.EnsureSuccessStatusCode();

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        return doc!.RootElement.GetProperty("data").GetProperty("apiKey").GetString()!;
    }

    private async Task CreateLogAsync(string apiKey, string applicationName, string logLevel, string message)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/logs");
        request.Headers.Add(TenantAuthConstants.ApiKeyHeaderName, apiKey);
        request.Content = JsonContent.Create(new { applicationName, logLevel, message });

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private async Task<List<JsonElement>> GetLogsAsync(string apiKey)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/logs");
        request.Headers.Add(TenantAuthConstants.ApiKeyHeaderName, apiKey);

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var data = doc!.RootElement.GetProperty("data");

        return data.EnumerateArray().Select(e => e.Clone()).ToList();
    }
}
