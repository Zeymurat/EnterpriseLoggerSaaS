using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseLogger.Api.IntegrationTests.Infrastructure;
using EnterpriseLogger.Application.Common.Constants;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace EnterpriseLogger.Api.IntegrationTests.Platform;

public class PlatformImpersonateTests : IClassFixture<EnterpriseLoggerWebApplicationFactory>
{
    private readonly EnterpriseLoggerWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PlatformImpersonateTests(EnterpriseLoggerWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Impersonate_AsPlatformAdmin_ReturnsTicketAndConsumableSession()
    {
        const string email = "impersonate-root@test.com";
        const string password = "TestPass123";
        const string tenantName = "Impersonate Corp";

        await IntegrationTestAuth.RegisterTenantAsync(_client, tenantName, email, password);
        var platformToken = await IntegrationTestAuth.PlatformLoginAsync(_client);

        var tenants = await GetTenantsAsync(platformToken);
        var tenantId = tenants.First(t => t.GetProperty("name").GetString() == tenantName)
            .GetProperty("id").GetInt32();

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/platform/tenants/{tenantId}/impersonate");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var data = doc!.RootElement.GetProperty("data");

        var ticket = data.GetProperty("ticket").GetString();
        Assert.False(string.IsNullOrWhiteSpace(ticket));

        using var consumeRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/auth/impersonate/{ticket}");
        var consumeResponse = await _client.SendAsync(consumeRequest);
        Assert.Equal(HttpStatusCode.OK, consumeResponse.StatusCode);

        using var consumeDoc = await consumeResponse.Content.ReadFromJsonAsync<JsonDocument>();
        var session = consumeDoc!.RootElement.GetProperty("data");

        var accessToken = session.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(accessToken));

        var user = session.GetProperty("user");
        Assert.Equal(email, user.GetProperty("email").GetString());
        Assert.Equal("Root", user.GetProperty("role").GetString());
        Assert.Equal(tenantName, user.GetProperty("tenantName").GetString());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        Assert.Contains(jwt.Claims, c => c.Type == AuthClaimTypes.TenantId);
        Assert.DoesNotContain(jwt.Claims, c => c.Type == AuthClaimTypes.IsPlatformAdmin);

        using var reuseRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/auth/impersonate/{ticket}");
        var reuseResponse = await _client.SendAsync(reuseRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);
    }

    [Fact]
    public async Task ImpersonateRedirect_ReturnsTenantPanelLocation()
    {
        const string email = "impersonate-redirect@test.com";
        await IntegrationTestAuth.RegisterTenantAsync(_client, "Redirect Corp", email);
        var platformToken = await IntegrationTestAuth.PlatformLoginAsync(_client);

        var tenants = await GetTenantsAsync(platformToken);
        var tenantId = tenants.First(t => t.GetProperty("name").GetString() == "Redirect Corp")
            .GetProperty("id").GetInt32();

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/platform/tenants/{tenantId}/impersonate");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var ticket = doc!.RootElement.GetProperty("data").GetProperty("ticket").GetString();

        using var redirectClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        using var redirectRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/platform/impersonate/redirect/{ticket}");
        var redirectResponse = await redirectClient.SendAsync(redirectRequest);

        Assert.Equal(HttpStatusCode.Redirect, redirectResponse.StatusCode);
        var location = redirectResponse.Headers.Location?.ToString();
        Assert.NotNull(location);
        Assert.Contains("/impersonate?ticket=", location, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Impersonate_WithTenantJwt_ReturnsForbidden()
    {
        const string email = "impersonate-forbidden@test.com";
        await IntegrationTestAuth.RegisterTenantAsync(_client, "Forbidden Impersonate", email);
        var tenantToken = await IntegrationTestAuth.LoginAsync(_client, email, "TestPass123");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/platform/tenants/1/impersonate");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tenantToken);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<List<JsonElement>> GetTenantsAsync(string platformToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/platform/tenants");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        return doc!.RootElement.GetProperty("data").GetProperty("tenants")
            .EnumerateArray()
            .Select(element => element.Clone())
            .ToList();
    }
}
