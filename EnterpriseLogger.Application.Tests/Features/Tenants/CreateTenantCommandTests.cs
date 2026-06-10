using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Features.Tenants.Commands;
using EnterpriseLogger.Application.Features.Tenants.Dtos;
using FluentValidation;
using Moq;
using Xunit;

namespace EnterpriseLogger.Application.Tests.Features.Tenants;

public class CreateTenantCommandTests
{
    private readonly Mock<IApplicationDbContext> _mockContext = new();
    private readonly IValidator<CreateTenantRequest> _validator = new CreateTenantRequestValidator();
    private readonly CreateTenantCommand _command;

    public CreateTenantCommandTests()
    {
        _command = new CreateTenantCommand(_mockContext.Object, _validator);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNameIsEmpty_ShouldReturnFailureResult()
    {
        var invalidRequest = new CreateTenantRequest("");

        var result = await _command.ExecuteAsync(invalidRequest);

        Assert.False(result.IsSuccess);
        Assert.Contains("Şirket adı boş olamaz.", result.ErrorMessage);
    }
}
