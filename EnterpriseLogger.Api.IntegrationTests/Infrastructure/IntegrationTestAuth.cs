using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseLogger.Application.Common.Constants;

namespace EnterpriseLogger.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Kayıt artık API key döndürmez; testler login + rotate akışını kullanır.
/// </summary>
public static class IntegrationTestAuth
{
    public static async Task<string> RegisterLoginAndRotateApiKeyAsync(
        HttpClient client,
        string name,
        string? email = null,
        string password = "TestPass123",
        string phone = "05551234567")
    {
        email ??= $"{Guid.NewGuid():N}@test.com";

        var registerResponse = await client.PostAsJsonAsync("/api/tenants", new
        {
            name,
            ownerEmail = email,
            ownerPhone = phone,
            ownerPassword = password
        });
        registerResponse.EnsureSuccessStatusCode();

        var token = await LoginAsync(client, email, password);
        return await RotateApiKeyAsync(client, token);
    }

    public static async Task RegisterTenantAsync(
        HttpClient client,
        string name,
        string email,
        string password = "TestPass123",
        string phone = "05551234567")
    {
        var response = await client.PostAsJsonAsync("/api/tenants", new
        {
            name,
            ownerEmail = email,
            ownerPhone = phone,
            ownerPassword = password
        });
        response.EnsureSuccessStatusCode();
    }

    public static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        return doc!.RootElement.GetProperty("data").GetProperty("accessToken").GetString()!;
    }

    public static async Task<string> RotateApiKeyAsync(HttpClient client, string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/tenants/me/api-key/rotate");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        return doc!.RootElement.GetProperty("data").GetProperty("apiKey").GetString()!;
    }

    public static async Task CreateLogAsync(
        HttpClient client,
        string apiKey,
        string applicationName,
        string logLevel,
        string message,
        string? httpMethod = null,
        int? statusCode = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/logs");
        request.Headers.Add(TenantAuthConstants.ApiKeyHeaderName, apiKey);
        request.Content = JsonContent.Create(new
        {
            applicationName,
            logLevel,
            message,
            httpMethod,
            statusCode,
        });

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}
