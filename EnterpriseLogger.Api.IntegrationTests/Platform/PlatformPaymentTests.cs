using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseLogger.Api.IntegrationTests.Infrastructure;
using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Settings;
using EnterpriseLogger.Domain.Enums;
using EnterpriseLogger.Infrastructure.Billing;
using EnterpriseLogger.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnterpriseLogger.Api.IntegrationTests.Platform;

public class PlatformPaymentTests : IClassFixture<EnterpriseLoggerWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly EnterpriseLoggerWebApplicationFactory _factory;

    public PlatformPaymentTests(EnterpriseLoggerWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RecordAndConfirmPayment_LinksToSubscription()
    {
        const string email = "payment-link@test.com";
        await IntegrationTestAuth.RegisterTenantAsync(_client, "Payment Link Corp", email);
        var platformToken = await IntegrationTestAuth.PlatformLoginAsync(_client);

        var tenantId = await GetTenantIdAsync(platformToken, "Payment Link Corp");
        var basicPackageId = await GetPackageIdAsync(platformToken, PackageCodes.Basic);

        var recordResponse = await PostPaymentAsync(platformToken, new
        {
            tenantId,
            packageId = basicPackageId,
            billingCycle = (int)BillingCycle.Annual,
            amount = 4999m,
            referenceNumber = "HVL-2026-001",
            periodStart = DateTime.UtcNow.Date
        });
        Assert.Equal(HttpStatusCode.OK, recordResponse.StatusCode);

        using var recordDoc = await recordResponse.Content.ReadFromJsonAsync<JsonDocument>();
        var paymentId = recordDoc!.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        using var confirmRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/platform/payments/{paymentId}/confirm");
        confirmRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        confirmRequest.Content = JsonContent.Create(new { notes = "Havale onaylandı" });
        var confirmResponse = await _client.SendAsync(confirmRequest);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        using var assignRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/platform/tenants/{tenantId}/subscription");
        assignRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        assignRequest.Content = JsonContent.Create(new
        {
            packageId = basicPackageId,
            billingCycle = (int)BillingCycle.Annual,
            autoRenew = true,
            isPaid = true,
            gracePeriodEndDate = (DateTime?)null,
            paymentId
        });

        var assignResponse = await _client.SendAsync(assignRequest);
        Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);

        using var assignDoc = await assignResponse.Content.ReadFromJsonAsync<JsonDocument>();
        var subscription = assignDoc!.RootElement.GetProperty("data").GetProperty("subscription");
        Assert.Equal((int)SubscriptionStatus.Active, subscription.GetProperty("status").GetInt32());
        Assert.Equal(PackageCodes.Basic, subscription.GetProperty("packageCode").GetString());
    }

    [Fact]
    public async Task RenewalProcessor_AutoRenewTrue_CreatesPendingPaymentRenewal()
    {
        const string email = "renewal@test.com";
        await IntegrationTestAuth.RegisterTenantAsync(_client, "Renewal Corp", email);
        var platformToken = await IntegrationTestAuth.PlatformLoginAsync(_client);
        var tenantId = await GetTenantIdAsync(platformToken, "Renewal Corp");
        var proPackageId = await GetPackageIdAsync(platformToken, PackageCodes.Pro);

        using var assignRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/platform/tenants/{tenantId}/subscription");
        assignRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        assignRequest.Content = JsonContent.Create(new
        {
            packageId = proPackageId,
            billingCycle = (int)BillingCycle.Monthly,
            autoRenew = true,
            isPaid = true,
            gracePeriodEndDate = (DateTime?)null,
            paymentId = (int?)null
        });
        (await _client.SendAsync(assignRequest)).EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var subscription = db.TenantSubscriptions.First(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active);
        subscription.EndDate = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();

        var processor = scope.ServiceProvider.GetRequiredService<SubscriptionRenewalProcessor>();
        await processor.ProcessAsync();

        var pending = db.TenantSubscriptions
            .Where(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.PendingPayment)
            .ToList();

        Assert.Single(pending);
        Assert.False(pending[0].IsPaid);
        Assert.NotNull(pending[0].GracePeriodEndDate);
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

    private async Task<HttpResponseMessage> PostPaymentAsync(string platformToken, object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/platform/payments");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        request.Content = JsonContent.Create(body);
        return await _client.SendAsync(request);
    }

    [Fact]
    public async Task UpdatePendingPayment_ChangesFields()
    {
        const string email = "payment-update@test.com";
        await IntegrationTestAuth.RegisterTenantAsync(_client, "Payment Update Corp", email);
        var platformToken = await IntegrationTestAuth.PlatformLoginAsync(_client);

        var tenantId = await GetTenantIdAsync(platformToken, "Payment Update Corp");
        var basicPackageId = await GetPackageIdAsync(platformToken, PackageCodes.Basic);
        var proPackageId = await GetPackageIdAsync(platformToken, PackageCodes.Pro);

        var recordResponse = await PostPaymentAsync(platformToken, new
        {
            tenantId,
            packageId = basicPackageId,
            billingCycle = (int)BillingCycle.Monthly,
            amount = 499m,
            referenceNumber = "HVL-EDIT-001",
            periodStart = DateTime.UtcNow.Date
        });
        Assert.Equal(HttpStatusCode.OK, recordResponse.StatusCode);

        using var recordDoc = await recordResponse.Content.ReadFromJsonAsync<JsonDocument>();
        var paymentId = recordDoc!.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        using var updateRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/platform/payments/{paymentId}");
        updateRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        updateRequest.Content = JsonContent.Create(new
        {
            packageId = proPackageId,
            billingCycle = (int)BillingCycle.Annual,
            amount = 9999m,
            referenceNumber = "HVL-EDIT-002",
            periodStart = DateTime.UtcNow.Date,
            notes = "Güncellendi"
        });
        var updateResponse = await _client.SendAsync(updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var updateDoc = await updateResponse.Content.ReadFromJsonAsync<JsonDocument>();
        var updated = updateDoc!.RootElement.GetProperty("data");
        Assert.Equal(proPackageId, updated.GetProperty("packageId").GetInt32());
        Assert.Equal("HVL-EDIT-002", updated.GetProperty("referenceNumber").GetString());
    }

    [Fact]
    public async Task DeleteRejectedPayment_RemovesRecord()
    {
        const string email = "payment-delete@test.com";
        await IntegrationTestAuth.RegisterTenantAsync(_client, "Payment Delete Corp", email);
        var platformToken = await IntegrationTestAuth.PlatformLoginAsync(_client);

        var tenantId = await GetTenantIdAsync(platformToken, "Payment Delete Corp");
        var basicPackageId = await GetPackageIdAsync(platformToken, PackageCodes.Basic);

        var recordResponse = await PostPaymentAsync(platformToken, new
        {
            tenantId,
            packageId = basicPackageId,
            billingCycle = (int)BillingCycle.Monthly,
            amount = 499m,
            referenceNumber = "HVL-DEL-001",
            periodStart = DateTime.UtcNow.Date
        });
        Assert.Equal(HttpStatusCode.OK, recordResponse.StatusCode);

        using var recordDoc = await recordResponse.Content.ReadFromJsonAsync<JsonDocument>();
        var paymentId = recordDoc!.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        using var rejectRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/platform/payments/{paymentId}/reject");
        rejectRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        rejectRequest.Content = JsonContent.Create(new { notes = "Yanlış dekont" });
        var rejectResponse = await _client.SendAsync(rejectRequest);
        Assert.Equal(HttpStatusCode.OK, rejectResponse.StatusCode);

        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/platform/payments/{paymentId}");
        deleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformToken);
        var deleteResponse = await _client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
    }
}
