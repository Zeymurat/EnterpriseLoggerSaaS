using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseLogger.Api.IntegrationTests.Infrastructure;
using EnterpriseLogger.Application.Common.Constants;
using Xunit;

namespace EnterpriseLogger.Api.IntegrationTests.Auth;

public class RefreshSessionTests : IClassFixture<EnterpriseLoggerWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RefreshSessionTests(EnterpriseLoggerWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Refresh_WithValidJwt_ReturnsNewAccessTokenAndPreservesSessionClaim()
    {
        const string ownerEmail = "refresh-owner@test.com";
        const string ownerPassword = "TestPass123";

        await IntegrationTestAuth.RegisterTenantAsync(_client, "Refresh Corp", ownerEmail, ownerPassword);
        var loginToken = await IntegrationTestAuth.LoginAsync(_client, ownerEmail, ownerPassword);

        using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refreshRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginToken);

        var refreshResponse = await _client.SendAsync(refreshRequest);
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        using var refreshDoc = await refreshResponse.Content.ReadFromJsonAsync<JsonDocument>();
        var data = refreshDoc!.RootElement.GetProperty("data");
        var refreshedToken = data.GetProperty("accessToken").GetString();

        Assert.False(string.IsNullOrWhiteSpace(refreshedToken));
        Assert.True(data.GetProperty("expiresIn").GetInt32() > 0);
        Assert.Equal(ownerEmail, data.GetProperty("user").GetProperty("email").GetString());

        var handler = new JwtSecurityTokenHandler();
        var loginJwt = handler.ReadJwtToken(loginToken);
        var refreshedJwt = handler.ReadJwtToken(refreshedToken!);

        var loginSessionStart = loginJwt.Claims.First(c => c.Type == AuthClaimTypes.SessionStartedAt).Value;
        var refreshedSessionStart = refreshedJwt.Claims.First(c => c.Type == AuthClaimTypes.SessionStartedAt).Value;
        Assert.Equal(loginSessionStart, refreshedSessionStart);
        Assert.True(refreshedJwt.ValidTo >= loginJwt.ValidTo);
    }

    [Fact]
    public async Task Refresh_WithoutAuthorization_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync("/api/auth/refresh", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
