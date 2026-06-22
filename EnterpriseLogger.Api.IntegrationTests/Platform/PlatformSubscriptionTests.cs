using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseLogger.Api.IntegrationTests.Infrastructure;
using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Domain.Enums;
using Xunit;

namespace EnterpriseLogger.Api.IntegrationTests.Platform;

public class PlatformSubscriptionTests : IClassFixture<EnterpriseLoggerWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PlatformSubscriptionTests(EnterpriseLoggerWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateTenant_AssignsFreePackageSubscription()
    {
        const string email = "free-sub@test.com";
        await IntegrationTestAuth.RegisterTenantAsync(_client, "Free Sub Corp", email);

        var platformToken = await IntegrationTestAuth.PlatformLoginAsync(_client);
        var tenants = await GetTenantsAsync(platformToken);
        var tenant = tenants.First(t => t.GetProperty("name").GetString() == "Free Sub Corp");

        Assert.Equal("Free", tenant.GetProperty("currentPackageName").GetString());

        var tenantId = tenant.GetProperty("id").GetInt32();
        var detail = await GetTenantDetailAsync(platformToken, tenantId);

        var current = detail.GetProperty("currentSubscription");
        Assert.Equal(PackageCodes.Free, current.GetProperty("packageCode").GetString());
        Assert.Equal((int)SubscriptionStatus.Active, current.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task AssignSubscription_ClosesPreviousAndCreatesHistory()
    {
        const string email = "sub-history@test.com";
        await IntegrationTestAuth.RegisterTenantAsync(_client, "Sub History Corp", email);

        var platformToken = await IntegrationTestAuth.PlatformLoginAsync(_client);
        var packages = await GetPackagesAsync(platformToken);
        var basicPackage = packages.First(p => p.GetProperty("code").GetString() == PackageCodes.Basic);

        var tenants = await GetTenantsAsync(platformToken);
        var tenantId = tenants.First(t => t.GetProperty("name").GetString() == "Sub History Corp")
            .GetProperty("id").GetInt32();

        await AssignSubscriptionAsync(
            platformToken,
            tenantId,
            basicPackage.GetProperty("id").GetInt32(),
            BillingCycle.Annual,
            isPaid: false);

        var detail = await GetTenantDetailAsync(platformToken, tenantId);
        var current = detail.GetProperty("currentSubscription");
        Assert.Equal(PackageCodes.Basic, current.GetProperty("packageCode").GetString());
        Assert.Equal((int)SubscriptionStatus.PendingPayment, current.GetProperty("status").GetInt32());
        Assert.Equal((int)BillingCycle.Annual, current.GetProperty("billingCycle").GetInt32());

        var history = detail.GetProperty("subscriptionHistory").EnumerateArray().ToList();
        Assert.Equal(2, history.Count);
        Assert.Contains(history, item =>
            item.GetProperty("packageCode").GetString() == PackageCodes.Free
            && item.GetProperty("status").GetInt32() == (int)SubscriptionStatus.Superseded);
    }

    [Fact]
    public async Task GetPackages_ReturnsSeededCatalog()
    {
        var platformToken = await IntegrationTestAuth.PlatformLoginAsync(_client);
        var packages = await GetPackagesAsync(platformToken);

        Assert.True(packages.Count >= 3);
        Assert.Contains(packages, p => p.GetProperty("code").GetString() == PackageCodes.Free);
        Assert.Contains(packages, p => p.GetProperty("code").GetString() == PackageCodes.Basic);
        Assert.Contains(packages, p => p.GetProperty("code").GetString() == PackageCodes.Pro);
    }

    private async Task<List<JsonElement>> GetTenantsAsync(string platformToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/platform/tenants");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        return doc!.RootElement.GetProperty("data").GetProperty("tenants")
            .EnumerateArray()
            .Select(element => element.Clone())
            .ToList();
    }

    private async Task<JsonElement> GetTenantDetailAsync(string platformToken, int tenantId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/platform/tenants/{tenantId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        return doc!.RootElement.GetProperty("data").Clone();
    }

    private async Task<List<JsonElement>> GetPackagesAsync(string platformToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/platform/packages");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        return doc!.RootElement.GetProperty("data").GetProperty("packages")
            .EnumerateArray()
            .Select(element => element.Clone())
            .ToList();
    }

    private async Task AssignSubscriptionAsync(
        string platformToken,
        int tenantId,
        int packageId,
        BillingCycle billingCycle,
        bool isPaid)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/platform/tenants/{tenantId}/subscription");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        request.Content = JsonContent.Create(new
        {
            packageId,
            billingCycle = (int)billingCycle,
            autoRenew = true,
            isPaid,
            gracePeriodEndDate = (DateTime?)null
        });

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
