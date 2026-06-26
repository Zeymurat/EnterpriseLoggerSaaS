using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseLogger.Api.IntegrationTests.Infrastructure;
using EnterpriseLogger.Application.Common.Constants;
using Xunit;

namespace EnterpriseLogger.Api.IntegrationTests.Platform;

public class PlatformDeleteTenantTests : IClassFixture<EnterpriseLoggerWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PlatformDeleteTenantTests(EnterpriseLoggerWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DeleteTenant_AsPlatformAdmin_RemovesTenant()
    {
        const string email = "delete-tenant@test.com";
        const string tenantName = "Delete Me Corp";
        await IntegrationTestAuth.RegisterTenantAsync(_client, tenantName, email);

        var platformToken = await IntegrationTestAuth.PlatformLoginAsync(_client);
        var tenantId = await GetTenantIdAsync(platformToken, tenantName);

        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/platform/tenants/{tenantId}");
        deleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        var deleteResponse = await _client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        using var getRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/platform/tenants/{tenantId}");
        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        var getResponse = await _client.SendAsync(getRequest);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteTenant_WithActivePaidPackage_IsBlocked()
    {
        const string email = "delete-blocked@test.com";
        const string tenantName = "Blocked Delete Corp";
        await IntegrationTestAuth.RegisterTenantAsync(_client, tenantName, email);

        var platformToken = await IntegrationTestAuth.PlatformLoginAsync(_client);
        var tenantId = await GetTenantIdAsync(platformToken, tenantName);
        var basicPackageId = await GetPackageIdAsync(platformToken, PackageCodes.Basic);

        using var assignRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/platform/tenants/{tenantId}/subscription");
        assignRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        assignRequest.Content = JsonContent.Create(new
        {
            packageId = basicPackageId,
            billingCycle = (int)EnterpriseLogger.Domain.Enums.BillingCycle.Monthly,
            autoRenew = true,
            isPaid = true,
            gracePeriodEndDate = (DateTime?)null
        });
        var assignResponse = await _client.SendAsync(assignRequest);
        Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);

        using var detailRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/platform/tenants/{tenantId}");
        detailRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        var detailResponse = await _client.SendAsync(detailRequest);
        detailResponse.EnsureSuccessStatusCode();
        using var detailDoc = await detailResponse.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.False(detailDoc!.RootElement.GetProperty("data").GetProperty("canDelete").GetBoolean());

        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/platform/tenants/{tenantId}");
        deleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        var deleteResponse = await _client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.BadRequest, deleteResponse.StatusCode);
    }

    private async Task<int> GetPackageIdAsync(string platformToken, string code)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/platform/packages");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        return doc!.RootElement.GetProperty("data").GetProperty("packages").EnumerateArray()
            .First(p => p.GetProperty("code").GetString() == code)
            .GetProperty("id").GetInt32();
    }

    private async Task<int> GetTenantIdAsync(string platformToken, string tenantName)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/platform/tenants");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        return doc!.RootElement.GetProperty("data").GetProperty("tenants").EnumerateArray()
            .First(t => t.GetProperty("name").GetString() == tenantName)
            .GetProperty("id").GetInt32();
    }
}
