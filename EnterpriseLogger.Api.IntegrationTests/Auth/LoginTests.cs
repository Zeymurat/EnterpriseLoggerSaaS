using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseLogger.Api.IntegrationTests.Infrastructure;
using EnterpriseLogger.Application.Common.Constants;
using Xunit;

namespace EnterpriseLogger.Api.IntegrationTests.Auth;

public class LoginTests : IClassFixture<EnterpriseLoggerWebApplicationFactory>
{
    private readonly HttpClient _client;

    public LoginTests(EnterpriseLoggerWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_AfterTenantRegistration_ReturnsJwtWithClaims()
    {
        const string ownerEmail = "jwt-owner@test.com";
        const string ownerPassword = "TestPass123";
        const string tenantName = "Jwt Test Corp";

        await RegisterTenantAsync(tenantName, ownerEmail, ownerPassword);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = ownerEmail,
            password = ownerPassword
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var data = doc!.RootElement.GetProperty("data");

        var accessToken = data.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(accessToken));
        Assert.True(data.GetProperty("expiresIn").GetInt32() > 0);

        var user = data.GetProperty("user");
        Assert.Equal(ownerEmail, user.GetProperty("email").GetString());
        Assert.Equal("Root", user.GetProperty("role").GetString());
        Assert.Equal(tenantName, user.GetProperty("tenantName").GetString());
        var permissions = user.GetProperty("permissions")
            .EnumerateArray()
            .Select(p => p.GetString())
            .ToList();
        Assert.Contains(PermissionCodes.LogsRead, permissions);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(accessToken);

        Assert.Contains(jwt.Claims, c => c.Value == ownerEmail);
        Assert.Contains(jwt.Claims, c => c.Type == AuthClaimTypes.TenantId);
        Assert.Contains(jwt.Claims, c => c.Type == AuthClaimTypes.Permission && c.Value == PermissionCodes.LogsRead);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        await RegisterTenantAsync("Wrong Pass Corp", "wrongpass@test.com", "TestPass123");

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "wrongpass@test.com",
            password = "WrongPass999"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_SameEmailMultipleTenants_RequiresTenantName()
    {
        const string sharedEmail = "shared-login@test.com";
        const string password = "TestPass123";

        await RegisterTenantAsync("Alpha Corp", sharedEmail, password, "05551111111");
        await RegisterTenantAsync("Beta Corp", sharedEmail, password, "05552222222");

        var withoutTenant = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = sharedEmail,
            password
        });

        Assert.Equal(HttpStatusCode.Unauthorized, withoutTenant.StatusCode);

        using var failDoc = await withoutTenant.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Contains(
            "şirket adını",
            failDoc!.RootElement.GetProperty("errorMessage").GetString(),
            StringComparison.OrdinalIgnoreCase);

        var withTenant = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = sharedEmail,
            password,
            tenantName = "Beta Corp"
        });

        Assert.Equal(HttpStatusCode.OK, withTenant.StatusCode);

        using var successDoc = await withTenant.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal(
            "Beta Corp",
            successDoc!.RootElement.GetProperty("data").GetProperty("user").GetProperty("tenantName").GetString());
    }

    [Fact]
    public async Task Login_SameEmailTenantsDifferOnlyByCase_ResolvesWithExactTenantName()
    {
        const string sharedEmail = "case-tenant@test.com";
        const string password = "TestPass123";

        await RegisterTenantAsync("Acme Corp", sharedEmail, password, "05553331111");
        await RegisterTenantAsync("acme corp", sharedEmail, password, "05553332222");

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = sharedEmail,
            password,
            tenantName = "acme corp"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal(
            "acme corp",
            doc!.RootElement.GetProperty("data").GetProperty("user").GetProperty("tenantName").GetString());
    }

    private async Task RegisterTenantAsync(
        string name,
        string ownerEmail,
        string ownerPassword,
        string ownerPhone = "05551234567")
    {
        var response = await _client.PostAsJsonAsync("/api/tenants", new
        {
            name,
            ownerEmail,
            ownerPhone,
            ownerPassword
        });

        response.EnsureSuccessStatusCode();
    }
}
