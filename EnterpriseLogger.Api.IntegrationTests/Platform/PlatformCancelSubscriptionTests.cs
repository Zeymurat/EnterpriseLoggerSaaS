using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseLogger.Api.IntegrationTests.Infrastructure;
using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Domain.Enums;
using Xunit;

namespace EnterpriseLogger.Api.IntegrationTests.Platform;

public class PlatformCancelSubscriptionTests : IClassFixture<EnterpriseLoggerWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PlatformCancelSubscriptionTests(EnterpriseLoggerWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CancelSubscription_RecordsReasonAndDowngradesToFree()
    {
        const string email = "cancel-sub@test.com";
        await IntegrationTestAuth.RegisterTenantAsync(_client, "Cancel Sub Corp", email);
        var platformToken = await IntegrationTestAuth.PlatformLoginAsync(_client);
        var tenantId = await GetTenantIdAsync(platformToken, "Cancel Sub Corp");
        var proPackageId = await GetPackageIdAsync(platformToken, PackageCodes.Pro);

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
        }

        using var cancelRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/platform/tenants/{tenantId}/subscription/cancel");
        cancelRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        cancelRequest.Content = JsonContent.Create(new { reason = "Müşteri talebi ile iptal" });
        var cancelResponse = await _client.SendAsync(cancelRequest);
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);

        using var cancelDoc = await cancelResponse.Content.ReadFromJsonAsync<JsonDocument>();
        var subscription = cancelDoc!.RootElement.GetProperty("data").GetProperty("subscription");
        Assert.Equal(PackageCodes.Free, subscription.GetProperty("packageCode").GetString());
        Assert.Equal((int)SubscriptionStatus.Active, subscription.GetProperty("status").GetInt32());

        using var detailRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/platform/tenants/{tenantId}");
        detailRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        var detailResponse = await _client.SendAsync(detailRequest);
        using var detailDoc = await detailResponse.Content.ReadFromJsonAsync<JsonDocument>();

        var cancelled = detailDoc!.RootElement
            .GetProperty("data")
            .GetProperty("subscriptionHistory")
            .EnumerateArray()
            .First(item =>
                item.GetProperty("status").GetInt32() == (int)SubscriptionStatus.Cancelled
                && item.GetProperty("packageCode").GetString() == PackageCodes.Pro);

        Assert.Equal("Müşteri talebi ile iptal", cancelled.GetProperty("cancellationReason").GetString());
        Assert.False(string.IsNullOrWhiteSpace(cancelled.GetProperty("cancelledAt").GetString()));
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
