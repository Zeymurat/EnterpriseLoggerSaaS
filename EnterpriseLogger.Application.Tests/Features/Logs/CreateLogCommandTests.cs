using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Features.Logs.Commands;
using EnterpriseLogger.Application.Features.Logs.Dtos;
using FluentValidation;
using Moq;
using Xunit;

namespace EnterpriseLogger.Application.Tests.Features.Logs;

public class CreateLogCommandTests
{
    private readonly Mock<IApplicationDbContext> _mockContext = new();
    private readonly Mock<ICurrentTenantProvider> _mockTenantProvider = new();
    private readonly IValidator<CreateLogRequest> _validator = new CreateLogRequestValidator();
    private readonly CreateLogCommand _command;

    public CreateLogCommandTests()
    {
        _command = new CreateLogCommand(_mockContext.Object, _mockTenantProvider.Object, _validator);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTenantNotResolved_ShouldReturnFailureResult()
    {
        _mockTenantProvider.Setup(p => p.IsResolved).Returns(false);
        _mockTenantProvider.Setup(p => p.TenantId).Returns((int?)null);

        var request = new CreateLogRequest("BillingApi", "Error", "Payment failed");

        var result = await _command.ExecuteAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Contains("Tenant kimliği çözümlenemedi", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WhenMessageIsEmpty_ShouldReturnFailureResult()
    {
        _mockTenantProvider.Setup(p => p.IsResolved).Returns(true);
        _mockTenantProvider.Setup(p => p.TenantId).Returns(1);

        var request = new CreateLogRequest("BillingApi", "Error", "");

        var result = await _command.ExecuteAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Contains("Log mesajı boş olamaz.", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLogLevelIsInvalid_ShouldReturnFailureResult()
    {
        _mockTenantProvider.Setup(p => p.IsResolved).Returns(true);
        _mockTenantProvider.Setup(p => p.TenantId).Returns(1);

        var request = new CreateLogRequest("BillingApi", "Critical", "Something broke");

        var result = await _command.ExecuteAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Contains("Log seviyesi yalnızca Info, Warning veya Error olabilir.", result.ErrorMessage);
    }
}
