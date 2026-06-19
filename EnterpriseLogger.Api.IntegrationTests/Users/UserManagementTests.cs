using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseLogger.Api.IntegrationTests.Infrastructure;
using EnterpriseLogger.Application.Common.Constants;
using Xunit;

namespace EnterpriseLogger.Api.IntegrationTests.Users;

public class UserManagementTests : IClassFixture<EnterpriseLoggerWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UserManagementTests(EnterpriseLoggerWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Root_InvitesUserWithLogsRead_UserCanGetLogs()
    {
        const string rootEmail = "root-invite@test.com";
        const string userEmail = "dev-read@test.com";
        const string password = "TestPass123";

        var rootToken = await RegisterAndLoginAsync("Invite Corp", rootEmail, password);

        var inviteResponse = await InviteUserAsync(rootToken, userEmail, "User", [PermissionCodes.LogsRead]);
        var tempPassword = inviteResponse.GetProperty("temporaryPassword").GetString()!;

        var userToken = await LoginAsync(userEmail, tempPassword);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/logs");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UserWithoutLogsRead_CannotGetLogs()
    {
        const string rootEmail = "root-no-read@test.com";
        const string userEmail = "dev-no-read@test.com";
        const string password = "TestPass123";

        var rootToken = await RegisterAndLoginAsync("No Read Corp", rootEmail, password);

        var inviteResponse = await InviteUserAsync(
            rootToken,
            userEmail,
            "User",
            [PermissionCodes.UsersRead]);
        var tempPassword = inviteResponse.GetProperty("temporaryPassword").GetString()!;

        var userToken = await LoginAsync(userEmail, tempPassword);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/logs");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CanInviteUser()
    {
        const string rootEmail = "root-for-admin@test.com";
        const string adminEmail = "admin-invite@test.com";
        const string userEmail = "invited-by-admin@test.com";
        const string password = "TestPass123";

        var rootToken = await RegisterAndLoginAsync("Admin Invite Corp", rootEmail, password);

        var adminInvite = await InviteUserAsync(rootToken, adminEmail, "Admin");
        var adminPassword = adminInvite.GetProperty("temporaryPassword").GetString()!;
        var adminToken = await LoginAsync(adminEmail, adminPassword);

        var userInvite = await InviteUserAsync(
            adminToken,
            userEmail,
            "User",
            [PermissionCodes.LogsRead]);

        Assert.False(string.IsNullOrWhiteSpace(userInvite.GetProperty("temporaryPassword").GetString()));
    }

    [Fact]
    public async Task Admin_CannotPromoteUserToAdmin()
    {
        const string rootEmail = "root-promote@test.com";
        const string adminEmail = "admin-promote@test.com";
        const string userEmail = "user-promote@test.com";
        const string password = "TestPass123";

        var rootToken = await RegisterAndLoginAsync("Promote Corp", rootEmail, password);

        var adminInvite = await InviteUserAsync(rootToken, adminEmail, "Admin");
        var adminToken = await LoginAsync(adminEmail, adminInvite.GetProperty("temporaryPassword").GetString()!);

        var userInvite = await InviteUserAsync(adminToken, userEmail, "User", [PermissionCodes.LogsRead]);
        var userId = userInvite.GetProperty("user").GetProperty("id").GetInt32();

        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/users/{userId}/role");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        request.Content = JsonContent.Create(new { role = "Admin" });

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Root_CanPromoteUserToAdmin()
    {
        const string rootEmail = "root-promote-ok@test.com";
        const string userEmail = "user-to-admin@test.com";
        const string password = "TestPass123";

        var rootToken = await RegisterAndLoginAsync("Promote Ok Corp", rootEmail, password);

        var userInvite = await InviteUserAsync(rootToken, userEmail, "User", [PermissionCodes.LogsRead]);
        var userId = userInvite.GetProperty("user").GetProperty("id").GetInt32();

        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/users/{userId}/role");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", rootToken);
        request.Content = JsonContent.Create(new { role = "Admin" });

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal("Admin", doc!.RootElement.GetProperty("data").GetProperty("role").GetString());
    }

    [Fact]
    public async Task Root_CannotBeDeactivated()
    {
        const string rootEmail = "root-nodeact@test.com";
        const string password = "TestPass123";

        var rootToken = await RegisterAndLoginAsync("Deact Corp", rootEmail, password);

        var usersResponse = await GetUsersAsync(rootToken);
        var rootId = usersResponse[0].GetProperty("id").GetInt32();

        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/users/{rootId}/deactivate");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", rootToken);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Invite_DuplicateActiveEmail_ReturnsConflict()
    {
        const string rootEmail = "root-dup-email@test.com";
        const string userEmail = "dup@test.com";
        const string password = "TestPass123";

        var rootToken = await RegisterAndLoginAsync("Dup Email Corp", rootEmail, password);
        await InviteUserAsync(rootToken, userEmail, "User", [PermissionCodes.LogsRead]);

        var response = await InviteUserRawAsync(rootToken, userEmail, "05551112233", "User", [PermissionCodes.LogsRead]);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Invite_DuplicatePhone_ReturnsConflict()
    {
        const string rootEmail = "root-dup-phone@test.com";
        const string password = "TestPass123";
        const string sharedPhone = "05553334455";

        var rootToken = await RegisterAndLoginAsync("Dup Phone Corp", rootEmail, password);
        await InviteUserRawAsync(rootToken, "first@test.com", sharedPhone, "User", [PermissionCodes.LogsRead]);

        var response = await InviteUserRawAsync(rootToken, "second@test.com", sharedPhone, "User", [PermissionCodes.LogsRead]);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Invite_ReactivatesInactiveUser()
    {
        const string rootEmail = "root-reinvite@test.com";
        const string userEmail = "reinvite@test.com";
        const string password = "TestPass123";

        var rootToken = await RegisterAndLoginAsync("Reinvite Corp", rootEmail, password);

        var firstInvite = await InviteUserAsync(rootToken, userEmail, "User", [PermissionCodes.LogsRead]);
        var userId = firstInvite.GetProperty("user").GetProperty("id").GetInt32();

        using (var deactivate = new HttpRequestMessage(HttpMethod.Patch, $"/api/users/{userId}/deactivate"))
        {
            deactivate.Headers.Authorization = new AuthenticationHeaderValue("Bearer", rootToken);
            var deactivateResponse = await _client.SendAsync(deactivate);
            deactivateResponse.EnsureSuccessStatusCode();
        }

        var reinvite = await InviteUserAsync(rootToken, userEmail, "User", [PermissionCodes.UsersRead]);
        Assert.False(string.IsNullOrWhiteSpace(reinvite.GetProperty("temporaryPassword").GetString()));
        Assert.True(reinvite.GetProperty("user").GetProperty("isActive").GetBoolean());

        var newPassword = reinvite.GetProperty("temporaryPassword").GetString()!;
        var userToken = await LoginAsync(userEmail, newPassword);

        using var logsRequest = new HttpRequestMessage(HttpMethod.Get, "/api/logs");
        logsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        var logsResponse = await _client.SendAsync(logsRequest);
        Assert.Equal(HttpStatusCode.Forbidden, logsResponse.StatusCode);
    }

    [Fact]
    public async Task DeactivatedUser_CannotLogin()
    {
        const string rootEmail = "root-deact-login@test.com";
        const string userEmail = "deact-user@test.com";
        const string password = "TestPass123";

        var rootToken = await RegisterAndLoginAsync("Deact Login Corp", rootEmail, password);
        var invite = await InviteUserAsync(rootToken, userEmail, "User", [PermissionCodes.LogsRead]);
        var tempPassword = invite.GetProperty("temporaryPassword").GetString()!;
        var userId = invite.GetProperty("user").GetProperty("id").GetInt32();

        using (var deactivate = new HttpRequestMessage(HttpMethod.Patch, $"/api/users/{userId}/deactivate"))
        {
            deactivate.Headers.Authorization = new AuthenticationHeaderValue("Bearer", rootToken);
            await _client.SendAsync(deactivate);
        }

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { email = userEmail, password = tempPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task UpdatePermissions_AfterReLogin_ReflectsNewAccess()
    {
        const string rootEmail = "root-perm-update@test.com";
        const string userEmail = "perm-update@test.com";
        const string password = "TestPass123";

        var rootToken = await RegisterAndLoginAsync("Perm Update Corp", rootEmail, password);
        var invite = await InviteUserAsync(rootToken, userEmail, "User", [PermissionCodes.LogsRead]);
        var userId = invite.GetProperty("user").GetProperty("id").GetInt32();
        var tempPassword = invite.GetProperty("temporaryPassword").GetString()!;

        using (var patch = new HttpRequestMessage(HttpMethod.Patch, $"/api/users/{userId}/permissions"))
        {
            patch.Headers.Authorization = new AuthenticationHeaderValue("Bearer", rootToken);
            patch.Content = JsonContent.Create(new { permissions = new[] { PermissionCodes.UsersRead } });
            var patchResponse = await _client.SendAsync(patch);
            patchResponse.EnsureSuccessStatusCode();
        }

        var userToken = await LoginAsync(userEmail, tempPassword);

        using var logsRequest = new HttpRequestMessage(HttpMethod.Get, "/api/logs");
        logsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        var logsResponse = await _client.SendAsync(logsRequest);
        Assert.Equal(HttpStatusCode.Forbidden, logsResponse.StatusCode);
    }

    [Fact]
    public async Task GetUsers_WithApiKey_ReturnsUnauthorized()
    {
        var apiKey = await IntegrationTestAuth.RegisterLoginAndRotateApiKeyAsync(_client, "ApiKey Users Corp");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/users");
        request.Headers.Add(TenantAuthConstants.ApiKeyHeaderName, apiKey);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<string> RegisterAndGetApiKeyAsync(string name) =>
        await IntegrationTestAuth.RegisterLoginAndRotateApiKeyAsync(_client, name);

    private async Task<HttpResponseMessage> InviteUserRawAsync(
        string bearerToken,
        string email,
        string phone,
        string role,
        IReadOnlyList<string>? permissions = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/users/invite");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        request.Content = JsonContent.Create(new { email, phone, role, permissions });
        return await _client.SendAsync(request);
    }

    private async Task<string> RegisterAndLoginAsync(string tenantName, string email, string password)
    {
        await RegisterTenantAsync(tenantName, email, password);
        return await LoginAsync(email, password);
    }

    private async Task RegisterTenantAsync(string name, string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/tenants", new
        {
            name,
            ownerEmail = email,
            ownerPhone = "05551234567",
            ownerPassword = password
        });
        response.EnsureSuccessStatusCode();
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        return doc!.RootElement.GetProperty("data").GetProperty("accessToken").GetString()!;
    }

    private async Task<JsonElement> InviteUserAsync(
        string bearerToken,
        string email,
        string role,
        IReadOnlyList<string>? permissions = null)
    {
        var uniquePhone = $"0555{Random.Shared.Next(1000000, 9999999)}";

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/users/invite");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        request.Content = JsonContent.Create(new
        {
            email,
            phone = uniquePhone,
            role,
            permissions
        });

        var response = await _client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Invite failed {(int)response.StatusCode}: {body}");
        }

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        return doc!.RootElement.GetProperty("data").Clone();
    }

    private async Task<List<JsonElement>> GetUsersAsync(string bearerToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        return doc!.RootElement.GetProperty("data").EnumerateArray().Select(e => e.Clone()).ToList();
    }
}
