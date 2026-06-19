using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseLogger.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace EnterpriseLogger.Api.IntegrationTests.Tenants;

public class TenantRegistrationTests : IClassFixture<EnterpriseLoggerWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TenantRegistrationTests(EnterpriseLoggerWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateTenant_WithOwner_ReturnsMetadataWithoutApiKey()
    {
        const string ownerEmail = "root@acme.test";
        const string ownerPhone = "05551234567";

        var response = await _client.PostAsJsonAsync("/api/tenants", new
        {
            name = "Acme Corp",
            ownerEmail,
            ownerPhone,
            ownerPassword = "TestPass123"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var data = doc!.RootElement.GetProperty("data");

        Assert.False(data.TryGetProperty("apiKey", out _));
        Assert.Equal(ownerEmail, data.GetProperty("ownerEmail").GetString());
        Assert.Equal("+905551234567", data.GetProperty("ownerPhone").GetString());
        Assert.True(data.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task RotateApiKey_AfterLogin_ReturnsKeyOnce()
    {
        const string email = "rotate-root@acme.test";
        const string password = "TestPass123";

        await IntegrationTestAuth.RegisterTenantAsync(_client, "Rotate Corp", email, password);
        var token = await IntegrationTestAuth.LoginAsync(_client, email, password);
        var apiKey = await IntegrationTestAuth.RotateApiKeyAsync(_client, token);

        Assert.StartsWith("EL_", apiKey);
        Assert.True(apiKey.Length >= 10);
    }

    [Fact]
    public async Task CreateTenant_SameEmailDifferentTenants_Succeeds()
    {
        const string sharedEmail = "consultant@shared.test";

        var first = await _client.PostAsJsonAsync("/api/tenants", new
        {
            name = "Tenant Alpha",
            ownerEmail = sharedEmail,
            ownerPhone = "05551111111",
            ownerPassword = "TestPass123"
        });

        var second = await _client.PostAsJsonAsync("/api/tenants", new
        {
            name = "Tenant Beta",
            ownerEmail = sharedEmail,
            ownerPhone = "05552222222",
            ownerPassword = "TestPass123"
        });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        using var firstDoc = await first.Content.ReadFromJsonAsync<JsonDocument>();
        using var secondDoc = await second.Content.ReadFromJsonAsync<JsonDocument>();

        var firstId = firstDoc!.RootElement.GetProperty("data").GetProperty("id").GetInt32();
        var secondId = secondDoc!.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        Assert.NotEqual(firstId, secondId);
    }

    [Fact]
    public async Task CreateTenant_WithWeakPassword_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/tenants", new
        {
            name = "Weak Pass Corp",
            ownerEmail = "weak@test.com",
            ownerPhone = "05551234567",
            ownerPassword = "short"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
