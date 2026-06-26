using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseLogger.Api.IntegrationTests.Infrastructure;
using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Domain.Enums;
using Xunit;

namespace EnterpriseLogger.Api.IntegrationTests.Platform;

public class PlatformLinkSubscriptionPaymentTests : IClassFixture<EnterpriseLoggerWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PlatformLinkSubscriptionPaymentTests(EnterpriseLoggerWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task LinkPayment_ToPendingSubscription_ActivatesSubscription()
    {
        const string email = "link-pending@test.com";
        await IntegrationTestAuth.RegisterTenantAsync(_client, "Link Pending Corp", email);
        var platformToken = await IntegrationTestAuth.PlatformLoginAsync(_client);
        var tenantId = await GetTenantIdAsync(platformToken, "Link Pending Corp");
        var proPackageId = await GetPackageIdAsync(platformToken, PackageCodes.Pro);

        using (var assignRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/platform/tenants/{tenantId}/subscription"))
        {
            assignRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
            assignRequest.Content = JsonContent.Create(new
            {
                packageId = proPackageId,
                billingCycle = (int)BillingCycle.Monthly,
                autoRenew = true,
                isPaid = false,
                gracePeriodEndDate = DateTime.UtcNow.AddDays(10)
            });
            var assignResponse = await _client.SendAsync(assignRequest);
            Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);
        }

        using (var recordRequest = new HttpRequestMessage(HttpMethod.Post, "/api/platform/payments"))
        {
            recordRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
            recordRequest.Content = JsonContent.Create(new
            {
                tenantId,
                packageId = proPackageId,
                billingCycle = (int)BillingCycle.Monthly,
                amount = 999m,
                referenceNumber = "HVL-LINK-001",
                periodStart = DateTime.UtcNow.Date
            });
            var recordResponse = await _client.SendAsync(recordRequest);
            Assert.Equal(HttpStatusCode.OK, recordResponse.StatusCode);

            using var recordDoc = await recordResponse.Content.ReadFromJsonAsync<JsonDocument>();
            var paymentId = recordDoc!.RootElement.GetProperty("data").GetProperty("id").GetInt32();

            using var linkRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/platform/tenants/{tenantId}/subscription/link-payment");
            linkRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
            linkRequest.Content = JsonContent.Create(new { paymentId });
            var linkResponse = await _client.SendAsync(linkRequest);
            Assert.Equal(HttpStatusCode.OK, linkResponse.StatusCode);

            using var linkDoc = await linkResponse.Content.ReadFromJsonAsync<JsonDocument>();
            var subscription = linkDoc!.RootElement.GetProperty("data").GetProperty("subscription");
            Assert.Equal((int)SubscriptionStatus.Active, subscription.GetProperty("status").GetInt32());
            Assert.True(subscription.GetProperty("isPaid").GetBoolean());
        }
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
