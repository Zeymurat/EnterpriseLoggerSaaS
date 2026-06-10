using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EnterpriseLogger.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Test ortamında API'yi ayağa kaldırır (gerçek HTTP pipeline, InMemory veritabanı).
/// </summary>
public class EnterpriseLoggerWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}
