using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseLogger.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace EnterpriseLogger.Api.IntegrationTests.Auth;

public class LoginLockoutTests
{
    [Fact]
    public async Task Login_AfterRepeatedFailures_LocksEmailAndReturns429()
    {
        await using var factory = new LowLoginProtectionWebApplicationFactory
        {
            MaxFailedAttemptsPerEmail = 3,
            MaxBackoffSeconds = 0
        };
        using var client = factory.CreateClient();

        const string email = "lockout@test.local";
        await IntegrationTestAuth.RegisterTenantAsync(client, "Lockout Corp", email);

        Assert.Equal(HttpStatusCode.Unauthorized, (await PostLoginAsync(client, email, "WrongPassword123")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await PostLoginAsync(client, email, "WrongPassword123")).StatusCode);

        var locked = await PostLoginAsync(client, email, "WrongPassword123");
        Assert.Equal(HttpStatusCode.TooManyRequests, locked.StatusCode);
        Assert.True(locked.Headers.TryGetValues("Retry-After", out _));

        using var doc = await locked.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Contains(
            "başarısız giriş",
            doc!.RootElement.GetProperty("errorMessage").GetString(),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_AfterSuccessfulLogin_ClearsFailureCounters()
    {
        await using var factory = new LowLoginProtectionWebApplicationFactory
        {
            MaxFailedAttemptsPerEmail = 3,
            MaxBackoffSeconds = 0
        };
        using var client = factory.CreateClient();

        const string email = "recover@test.local";
        const string password = "TestPass123";
        await IntegrationTestAuth.RegisterTenantAsync(client, "Recover Corp", email, password);

        for (var i = 0; i < 2; i++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await PostLoginAsync(client, email, "WrongPassword123")).StatusCode);
        }

        var success = await PostLoginAsync(client, email, password);
        Assert.Equal(HttpStatusCode.OK, success.StatusCode);

        for (var i = 0; i < 2; i++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await PostLoginAsync(client, email, "WrongPassword123")).StatusCode);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await PostLoginAsync(client, email, "WrongPassword123")).StatusCode);
    }

    [Fact]
    public async Task Login_LockedEmail_StillReturns429EvenWithCorrectPassword()
    {
        await using var factory = new LowLoginProtectionWebApplicationFactory
        {
            MaxFailedAttemptsPerEmail = 2,
            MaxBackoffSeconds = 0
        };
        using var client = factory.CreateClient();

        const string email = "locked-correct@test.local";
        const string password = "TestPass123";
        await IntegrationTestAuth.RegisterTenantAsync(client, "Locked Correct Corp", email, password);

        Assert.Equal(HttpStatusCode.Unauthorized, (await PostLoginAsync(client, email, "WrongPassword123")).StatusCode);

        var lockedWithWrongPassword = await PostLoginAsync(client, email, "WrongPassword123");
        Assert.Equal(HttpStatusCode.TooManyRequests, lockedWithWrongPassword.StatusCode);

        var lockedWithCorrectPassword = await PostLoginAsync(client, email, password);
        Assert.Equal(HttpStatusCode.TooManyRequests, lockedWithCorrectPassword.StatusCode);
    }

    private static Task<HttpResponseMessage> PostLoginAsync(HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/api/auth/login", new { email, password });
}
