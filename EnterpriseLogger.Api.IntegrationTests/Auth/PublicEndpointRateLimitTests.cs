using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseLogger.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace EnterpriseLogger.Api.IntegrationTests.Auth;

public class PublicEndpointRateLimitTests
{
    [Fact]
    public async Task PostLogin_ExceedingIpLimit_Returns429()
    {
        await using var factory = new LowPublicRateLimitWebApplicationFactory { AuthLoginRequestsPerWindow = 3 };
        using var client = factory.CreateClient();

        for (var i = 0; i < 3; i++)
        {
            var allowed = await PostLoginAsync(client, $"user{i}@test.local");
            Assert.Equal(HttpStatusCode.Unauthorized, allowed.StatusCode);
        }

        var limited = await PostLoginAsync(client, "user3@test.local");
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.True(limited.Headers.TryGetValues("Retry-After", out _));

        using var doc = await limited.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal("Çok fazla giriş denemesi. Lütfen kısa süre sonra tekrar deneyin.", doc!.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task PostTenants_ExceedingIpLimit_Returns429()
    {
        await using var factory = new LowPublicRateLimitWebApplicationFactory { TenantRegisterRequestsPerWindow = 2 };
        using var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await PostTenantAsync(client, 1)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await PostTenantAsync(client, 2)).StatusCode);

        var limited = await PostTenantAsync(client, 3);
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.True(limited.Headers.TryGetValues("Retry-After", out _));
    }

    [Fact]
    public async Task PostTenants_RateLimit_DoesNotApplyToApiKeyRotate()
    {
        await using var factory = new LowPublicRateLimitWebApplicationFactory { TenantRegisterRequestsPerWindow = 1 };
        using var client = factory.CreateClient();

        var email = $"rotate-{Guid.NewGuid():N}@test.local";
        var registerResponse = await client.PostAsJsonAsync("/api/tenants", new
        {
            name = $"Rotate Tenant {Guid.NewGuid():N}",
            ownerEmail = email,
            ownerPhone = "05551234567",
            ownerPassword = "TestPass123"
        });
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var token = await IntegrationTestAuth.LoginAsync(client, email, "TestPass123");
        using var rotateRequest = new HttpRequestMessage(HttpMethod.Post, "/api/tenants/me/api-key/rotate");
        rotateRequest.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var rotateResponse = await client.SendAsync(rotateRequest);
        Assert.Equal(HttpStatusCode.OK, rotateResponse.StatusCode);
    }

    private static Task<HttpResponseMessage> PostLoginAsync(HttpClient client, string email) =>
        client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "WrongPassword123"
        });

    private static Task<HttpResponseMessage> PostTenantAsync(HttpClient client, int suffix) =>
        client.PostAsJsonAsync("/api/tenants", new
        {
            name = $"RateLimit Tenant {suffix}-{Guid.NewGuid():N}",
            ownerEmail = $"tenant-{suffix}-{Guid.NewGuid():N}@test.local",
            ownerPhone = "05551234567",
            ownerPassword = "TestPass123"
        });
}
