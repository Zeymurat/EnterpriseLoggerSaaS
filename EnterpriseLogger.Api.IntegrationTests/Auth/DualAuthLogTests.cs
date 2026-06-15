using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseLogger.Api.IntegrationTests.Infrastructure;
using EnterpriseLogger.Application.Common.Constants;
using Xunit;

namespace EnterpriseLogger.Api.IntegrationTests.Auth;

public class DualAuthLogTests : IClassFixture<EnterpriseLoggerWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DualAuthLogTests(EnterpriseLoggerWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetLogs_WithJwtRoot_ReturnsLogs()
    {
        const string tenantName = "Jwt Logs Corp";
        const string email = "jwt-logs@test.com";
        const string password = "TestPass123";

        var apiKey = await RegisterAndGetApiKeyAsync(tenantName, email, password);
        await CreateLogAsync(apiKey, "JwtApp", "Info", "JWT readable log");

        var token = await LoginAsync(email, password);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/logs");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var logs = doc!.RootElement.GetProperty("data").EnumerateArray().ToList();

        Assert.Single(logs);
        Assert.Equal("JWT readable log", logs[0].GetProperty("message").GetString());
    }

    [Fact]
    public async Task PostLogs_WithJwtRoot_ReturnsCreatedLog()
    {
        const string email = "jwt-post@test.com";
        const string password = "TestPass123";

        await RegisterTenantAsync("Jwt Post Corp", email, password);
        var token = await LoginAsync(email, password);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/logs");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new
        {
            applicationName = "PanelApp",
            logLevel = "Warning",
            message = "Created via JWT"
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetLogs_WithInvalidJwt_ReturnsUnauthorized()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/logs");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.token.here");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("problem+json", response.Content.Headers.ContentType?.MediaType ?? "");
    }

    [Fact]
    public async Task GetLogs_WithNoAuth_ReturnsUnauthorizedProblemDetails()
    {
        var response = await _client.GetAsync("/api/logs");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("problem+json", response.Content.Headers.ContentType?.MediaType ?? "");
    }

    [Fact]
    public async Task GetLogs_ApiKeyStillWorks_AfterDualAuth()
    {
        var apiKey = await RegisterAndGetApiKeyAsync("ApiKey Still Works");

        await CreateLogAsync(apiKey, "LegacyApp", "Info", "ApiKey log");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/logs");
        request.Headers.Add(TenantAuthConstants.ApiKeyHeaderName, apiKey);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var logs = doc!.RootElement.GetProperty("data").EnumerateArray().ToList();

        Assert.Single(logs);
    }

    private async Task<string> RegisterAndGetApiKeyAsync(
        string name,
        string? email = null,
        string password = "TestPass123")
    {
        var response = await _client.PostAsJsonAsync("/api/tenants", new
        {
            name,
            ownerEmail = email ?? $"{Guid.NewGuid():N}@test.com",
            ownerPhone = "05551234567",
            ownerPassword = password
        });
        response.EnsureSuccessStatusCode();

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        return doc!.RootElement.GetProperty("data").GetProperty("apiKey").GetString()!;
    }

    private async Task RegisterTenantAsync(string name, string email, string password)
    {
        await RegisterAndGetApiKeyAsync(name, email, password);
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        return doc!.RootElement.GetProperty("data").GetProperty("accessToken").GetString()!;
    }

    private async Task CreateLogAsync(string apiKey, string applicationName, string logLevel, string message)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/logs");
        request.Headers.Add(TenantAuthConstants.ApiKeyHeaderName, apiKey);
        request.Content = JsonContent.Create(new { applicationName, logLevel, message });

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}
