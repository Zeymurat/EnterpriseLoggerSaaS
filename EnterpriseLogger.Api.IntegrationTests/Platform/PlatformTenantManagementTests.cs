using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseLogger.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace EnterpriseLogger.Api.IntegrationTests.Platform;

public class PlatformTenantManagementTests : IClassFixture<EnterpriseLoggerWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PlatformTenantManagementTests(EnterpriseLoggerWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SetTenantInactive_BlocksTenantLogin()
    {
        const string tenantName = "Inactive Tenant Corp";
        const string ownerEmail = "inactive-tenant@test.com";
        const string ownerPassword = "TestPass123";

        await IntegrationTestAuth.RegisterTenantAsync(_client, tenantName, ownerEmail, ownerPassword);
        var tenantId = await FindTenantIdAsync(tenantName);
        var platformToken = await IntegrationTestAuth.PlatformLoginAsync(_client);

        using (var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/platform/tenants/{tenantId}/status"))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
            request.Content = JsonContent.Create(new { isActive = false });
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = ownerEmail,
            password = ownerPassword,
        });
        Assert.Equal(HttpStatusCode.Forbidden, loginResponse.StatusCode);
    }

    [Fact]
    public async Task SetTenantInactive_BlocksBillingWithExistingToken()
    {
        const string tenantName = "Inactive Billing Corp";
        const string ownerEmail = "inactive-billing@test.com";
        const string ownerPassword = "TestPass123";

        await IntegrationTestAuth.RegisterTenantAsync(_client, tenantName, ownerEmail, ownerPassword);
        var tenantId = await FindTenantIdAsync(tenantName);
        var token = await IntegrationTestAuth.LoginAsync(_client, ownerEmail, ownerPassword);

        using (var billing = new HttpRequestMessage(HttpMethod.Get, "/api/billing/overview"))
        {
            billing.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var before = await _client.SendAsync(billing);
            before.EnsureSuccessStatusCode();
        }

        var platformToken = await IntegrationTestAuth.PlatformLoginAsync(_client);
        using (var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/platform/tenants/{tenantId}/status"))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
            request.Content = JsonContent.Create(new { isActive = false });
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        using (var billing = new HttpRequestMessage(HttpMethod.Get, "/api/billing/overview"))
        {
            billing.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var after = await _client.SendAsync(billing);
            Assert.Equal(HttpStatusCode.Forbidden, after.StatusCode);
        }
    }

    [Fact]
    public async Task ResetRootPassword_AllowsLoginWithNewPassword()
    {
        const string tenantName = "Reset Password Corp";
        const string ownerEmail = "reset-password@test.com";
        const string ownerPassword = "TestPass123";

        await IntegrationTestAuth.RegisterTenantAsync(_client, tenantName, ownerEmail, ownerPassword);
        var tenantId = await FindTenantIdAsync(tenantName);
        var platformToken = await IntegrationTestAuth.PlatformLoginAsync(_client);

        string temporaryPassword;
        using (var request = new HttpRequestMessage(
                   HttpMethod.Post,
                   $"/api/platform/tenants/{tenantId}/root-password/reset"))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
            temporaryPassword = doc!.RootElement
                .GetProperty("data")
                .GetProperty("temporaryPassword")
                .GetString()!;
            Assert.False(string.IsNullOrWhiteSpace(temporaryPassword));
        }

        var oldLogin = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = ownerEmail,
            password = ownerPassword,
        });
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);

        var newLogin = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = ownerEmail,
            password = temporaryPassword,
        });
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
    }

    [Fact]
    public async Task ListTenants_WithFilters_ReturnsMatchingTenant()
    {
        const string tenantName = "Filter Match Corp";
        const string ownerEmail = "filter-match@test.com";
        const string ownerPassword = "TestPass123";
        const string ownerPhone = "05559876543";

        await IntegrationTestAuth.RegisterTenantAsync(
            _client,
            tenantName,
            ownerEmail,
            ownerPassword,
            ownerPhone);
        var platformToken = await IntegrationTestAuth.PlatformLoginAsync(_client);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/platform/tenants?name=Filter%20Match&rootEmail=filter-match&rootPhone=9876543&isActive=true");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var tenants = doc!.RootElement
            .GetProperty("data")
            .GetProperty("tenants")
            .EnumerateArray()
            .ToList();

        Assert.Contains(tenants, t => t.GetProperty("name").GetString() == tenantName);
        var match = tenants.First(t => t.GetProperty("name").GetString() == tenantName);
        Assert.Equal(ownerEmail, match.GetProperty("rootEmail").GetString());
        Assert.Contains("9876543", match.GetProperty("rootPhone").GetString());
    }

    private async Task<int> FindTenantIdAsync(string tenantName)
    {
        var platformToken = await IntegrationTestAuth.PlatformLoginAsync(_client);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/platform/tenants");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        return doc!.RootElement
            .GetProperty("data")
            .GetProperty("tenants")
            .EnumerateArray()
            .First(t => t.GetProperty("name").GetString() == tenantName)
            .GetProperty("id")
            .GetInt32();
    }
}
