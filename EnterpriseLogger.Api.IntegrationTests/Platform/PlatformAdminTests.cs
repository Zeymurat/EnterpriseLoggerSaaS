using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseLogger.Api.IntegrationTests.Infrastructure;
using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Infrastructure.Persistence;
using Xunit;

namespace EnterpriseLogger.Api.IntegrationTests.Platform;

public class PlatformLoginTests : IClassFixture<EnterpriseLoggerWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PlatformLoginTests(EnterpriseLoggerWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsJwtWithPlatformAdminClaim()
    {
        var response = await _client.PostAsJsonAsync("/api/platform/auth/login", new
        {
            email = PlatformAdminSeeder.TestEmail,
            password = PlatformAdminSeeder.TestPassword
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var data = doc!.RootElement.GetProperty("data");

        var accessToken = data.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(accessToken));
        Assert.True(data.GetProperty("expiresIn").GetInt32() > 0);
        Assert.Equal(PlatformAdminSeeder.TestEmail, data.GetProperty("admin").GetProperty("email").GetString());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        Assert.Contains(jwt.Claims, c => c.Type == AuthClaimTypes.IsPlatformAdmin && c.Value == "true");
        Assert.DoesNotContain(jwt.Claims, c => c.Type == AuthClaimTypes.TenantId);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/platform/auth/login", new
        {
            email = PlatformAdminSeeder.TestEmail,
            password = "WrongPassword999!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

public class PlatformTenantsTests : IClassFixture<EnterpriseLoggerWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PlatformTenantsTests(EnterpriseLoggerWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListTenants_AsPlatformAdmin_ReturnsAllTenantsWithCounts()
    {
        const string tenantName = "Platform List Corp";
        const string ownerEmail = "platform-list@test.com";
        const string ownerPassword = "TestPass123";

        await IntegrationTestAuth.RegisterTenantAsync(_client, tenantName, ownerEmail, ownerPassword);
        var apiKey = await IntegrationTestAuth.RegisterLoginAndRotateApiKeyAsync(
            _client,
            "Other Corp",
            "other-platform-list@test.com",
            ownerPassword);

        await IntegrationTestAuth.CreateLogAsync(_client, apiKey, "App", "Info", "test log");

        var platformToken = await IntegrationTestAuth.PlatformLoginAsync(_client);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/platform/tenants");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var tenants = doc!.RootElement
            .GetProperty("data")
            .GetProperty("tenants")
            .EnumerateArray()
            .ToList();

        Assert.True(tenants.Count >= 2);

        var listedTenant = tenants.FirstOrDefault(t =>
            t.GetProperty("name").GetString() == "Other Corp");
        Assert.NotEqual(default, listedTenant);
        Assert.Equal(1, listedTenant.GetProperty("logCount").GetInt64());
        Assert.True(listedTenant.GetProperty("userCount").GetInt32() >= 1);
    }

    [Fact]
    public async Task ListTenants_WithTenantJwt_ReturnsForbidden()
    {
        const string ownerEmail = "tenant-jwt-platform@test.com";
        const string ownerPassword = "TestPass123";

        await IntegrationTestAuth.RegisterTenantAsync(_client, "Tenant JWT Corp", ownerEmail, ownerPassword);
        var tenantToken = await IntegrationTestAuth.LoginAsync(_client, ownerEmail, ownerPassword);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/platform/tenants");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tenantToken);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
