using System.Net;
using EnterpriseLogger.Api.Infrastructure;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace EnterpriseLogger.Api.IntegrationTests.Infrastructure;

public class ClientIpResolverTests
{
    [Fact]
    public void GetClientIp_IgnoresForwardedHeader_WhenTrustDisabled()
    {
        var previous = Environment.GetEnvironmentVariable("TRUST_FORWARDED_HEADERS");
        try
        {
            Environment.SetEnvironmentVariable("TRUST_FORWARDED_HEADERS", "false");

            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.10");
            context.Request.Headers["X-Forwarded-For"] = "198.51.100.99";

            Assert.Equal("203.0.113.10", ClientIpResolver.GetClientIp(context));
        }
        finally
        {
            Environment.SetEnvironmentVariable("TRUST_FORWARDED_HEADERS", previous);
        }
    }

    [Fact]
    public void GetClientIp_UsesForwardedHeader_WhenTrustEnabled()
    {
        var previous = Environment.GetEnvironmentVariable("TRUST_FORWARDED_HEADERS");
        try
        {
            Environment.SetEnvironmentVariable("TRUST_FORWARDED_HEADERS", "true");

            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.10");
            context.Request.Headers["X-Forwarded-For"] = "198.51.100.99, 203.0.113.50";

            Assert.Equal("198.51.100.99", ClientIpResolver.GetClientIp(context));
        }
        finally
        {
            Environment.SetEnvironmentVariable("TRUST_FORWARDED_HEADERS", previous);
        }
    }
}
