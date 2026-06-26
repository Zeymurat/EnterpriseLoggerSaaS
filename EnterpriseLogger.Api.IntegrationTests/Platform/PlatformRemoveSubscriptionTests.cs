using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseLogger.Api.IntegrationTests.Infrastructure;
using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Domain.Enums;
using Xunit;

namespace EnterpriseLogger.Api.IntegrationTests.Platform;

public class PlatformRemoveSubscriptionTests : IClassFixture<EnterpriseLoggerWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PlatformRemoveSubscriptionTests(EnterpriseLoggerWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RemoveSubscription_DeletesRecordAndEnsuresFree()
    {
        const string email = "remove-sub@test.com";
        await IntegrationTestAuth.RegisterTenantAsync(_client, "Remove Sub Corp", email);
        var platformToken = await IntegrationTestAuth.PlatformLoginAsync(_client);
        var tenantId = await GetTenantIdAsync(platformToken, "Remove Sub Corp");
        var proPackageId = await GetPackageIdAsync(platformToken, PackageCodes.Pro);

        int proSubscriptionId;
        using (var assignRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/platform/tenants/{tenantId}/subscription"))
        {
            assignRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
            assignRequest.Content = JsonContent.Create(new
            {
                packageId = proPackageId,
                billingCycle = (int)BillingCycle.Monthly,
                autoRenew = true,
                isPaid = true
            });
            var assignResponse = await _client.SendAsync(assignRequest);
            Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);

            using var assignDoc = await assignResponse.Content.ReadFromJsonAsync<JsonDocument>();
            proSubscriptionId = assignDoc!.RootElement
                .GetProperty("data")
                .GetProperty("subscription")
                .GetProperty("id")
                .GetInt32();
        }

        using var deleteRequest = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/api/platform/tenants/{tenantId}/subscriptions/{proSubscriptionId}");
        deleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        var deleteResponse = await _client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        using var detailRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/platform/tenants/{tenantId}");
        detailRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        var detailResponse = await _client.SendAsync(detailRequest);
        using var detailDoc = await detailResponse.Content.ReadFromJsonAsync<JsonDocument>();

        var history = detailDoc!.RootElement
            .GetProperty("data")
            .GetProperty("subscriptionHistory")
            .EnumerateArray()
            .ToList();

        Assert.DoesNotContain(history, item => item.GetProperty("id").GetInt32() == proSubscriptionId);
        Assert.Equal(
            PackageCodes.Free,
            detailDoc.RootElement.GetProperty("data").GetProperty("currentSubscription")
                .GetProperty("packageCode").GetString());
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
}
