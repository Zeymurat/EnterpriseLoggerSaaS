using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Features.Logs.Dtos;
using EnterpriseLogger.Application.Features.Logs.Queries;
using Moq;
using Xunit;

namespace EnterpriseLogger.Application.Tests.Features.Logs;

public class GetLogsQueryTests
{
    [Fact]
    public async Task ExecuteAsync_WhenTenantNotResolved_ShouldReturnFailureResult()
    {
        var mockContext = new Mock<IApplicationDbContext>();
        var mockTenantProvider = new Mock<ICurrentTenantProvider>();
        mockTenantProvider.Setup(p => p.IsResolved).Returns(false);
        mockTenantProvider.Setup(p => p.TenantId).Returns((int?)null);

        var query = new GetLogsQuery(mockContext.Object, mockTenantProvider.Object);

        var result = await query.ExecuteAsync(new GetLogsQueryParams());

        Assert.False(result.IsSuccess);
        Assert.Contains("Tenant kimliği çözümlenemedi", result.ErrorMessage);
    }
}
