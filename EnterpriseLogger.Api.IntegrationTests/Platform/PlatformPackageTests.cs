using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseLogger.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace EnterpriseLogger.Api.IntegrationTests.Platform;

public class PlatformPackageTests : IClassFixture<EnterpriseLoggerWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PlatformPackageTests(EnterpriseLoggerWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateUpdateAndDeletePackage_WorksForUnusedPackage()
    {
        var platformToken = await IntegrationTestAuth.PlatformLoginAsync(_client);

        int packageId;
        using (var request = new HttpRequestMessage(HttpMethod.Post, "/api/platform/packages"))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
            request.Content = JsonContent.Create(new
            {
                code = "enterprise-test",
                name = "Enterprise Test",
                description = "Test package",
                allowedLogLevels = "INFO,WARNING,ERROR",
                isMailEnabled = true,
                isSmsEnabled = false,
                monthlyRequestLimit = 50_000,
                maxLogsPerMinute = 250,
                storageRetentionDays = 60,
                priceMonthly = 750,
                priceQuarterly = 2_100,
                priceSemiAnnual = 4_000,
                priceAnnual = 7_500,
                isAvailable = true,
                sortOrder = 99,
            });

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
            packageId = doc!.RootElement.GetProperty("data").GetProperty("id").GetInt32();
        }

        using (var request = new HttpRequestMessage(HttpMethod.Put, $"/api/platform/packages/{packageId}"))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
            request.Content = JsonContent.Create(new
            {
                name = "Enterprise Test Updated",
                description = "Updated description",
                allowedLogLevels = "INFO,ERROR",
                isMailEnabled = false,
                isSmsEnabled = false,
                monthlyRequestLimit = 60_000,
                maxLogsPerMinute = 300,
                storageRetentionDays = 90,
                priceMonthly = 800,
                priceQuarterly = 2_200,
                priceSemiAnnual = 4_200,
                priceAnnual = 7_800,
                isAvailable = false,
                sortOrder = 100,
            });

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        using (var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/platform/packages/{packageId}"))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task ListPackages_IncludesActiveTenantCount()
    {
        const string ownerEmail = "package-count@test.com";
        const string ownerPassword = "TestPass123";

        await IntegrationTestAuth.RegisterTenantAsync(_client, "Package Count Corp", ownerEmail, ownerPassword);
        var platformToken = await IntegrationTestAuth.PlatformLoginAsync(_client);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/platform/packages");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var freePackage = doc!.RootElement
            .GetProperty("data")
            .GetProperty("packages")
            .EnumerateArray()
            .First(p => p.GetProperty("code").GetString() == "free");

        Assert.True(freePackage.GetProperty("activeTenantCount").GetInt32() >= 1);
    }

    [Fact]
    public async Task DeleteDefaultPackage_ReturnsBadRequest()
    {
        var platformToken = await IntegrationTestAuth.PlatformLoginAsync(_client);

        using var listRequest = new HttpRequestMessage(HttpMethod.Get, "/api/platform/packages");
        listRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        var listResponse = await _client.SendAsync(listRequest);
        listResponse.EnsureSuccessStatusCode();

        using var listDoc = await listResponse.Content.ReadFromJsonAsync<JsonDocument>();
        var defaultPackageId = listDoc!.RootElement
            .GetProperty("data")
            .GetProperty("packages")
            .EnumerateArray()
            .First(p => p.GetProperty("isDefault").GetBoolean())
            .GetProperty("id")
            .GetInt32();

        using var deleteRequest = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/api/platform/packages/{defaultPackageId}");
        deleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);

        var deleteResponse = await _client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.BadRequest, deleteResponse.StatusCode);
    }
}
