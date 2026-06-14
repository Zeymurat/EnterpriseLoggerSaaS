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
    private readonly Mock<IPasswordHasher> _mockPasswordHasher = new();
    private readonly IValidator<CreateTenantRequest> _validator = new CreateTenantRequestValidator();
    private readonly CreateTenantCommand _command;

    public CreateTenantCommandTests()
    {
        _mockPasswordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed-password");
        _command = new CreateTenantCommand(_mockContext.Object, _validator, _mockPasswordHasher.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNameIsEmpty_ShouldReturnFailureResult()
    {
        var invalidRequest = new CreateTenantRequest("", "owner@test.com", "05551234567", "TestPass123");

        var result = await _command.ExecuteAsync(invalidRequest);

        Assert.False(result.IsSuccess);
        Assert.Contains("Şirket adı boş olamaz.", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmailIsInvalid_ShouldReturnFailureResult()
    {
        var invalidRequest = new CreateTenantRequest("Acme Corp", "not-an-email", "05551234567", "TestPass123");

        var result = await _command.ExecuteAsync(invalidRequest);

        Assert.False(result.IsSuccess);
        Assert.Contains("Geçerli bir e-posta adresi giriniz.", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPhoneIsInvalid_ShouldReturnFailureResult()
    {
        var invalidRequest = new CreateTenantRequest("Acme Corp", "owner@test.com", "invalid", "TestPass123");

        var result = await _command.ExecuteAsync(invalidRequest);

        Assert.False(result.IsSuccess);
        Assert.Contains("Geçerli bir telefon numarası giriniz", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPasswordIsTooWeak_ShouldReturnFailureResult()
    {
        var invalidRequest = new CreateTenantRequest("Acme Corp", "owner@test.com", "05551234567", "weak");

        var result = await _command.ExecuteAsync(invalidRequest);

        Assert.False(result.IsSuccess);
        Assert.Contains("Şifre en az 8 karakter olmalıdır.", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPasswordMissingUppercase_ShouldReturnFailureResult()
    {
        var invalidRequest = new CreateTenantRequest("Acme Corp", "owner@test.com", "05551234567", "testpass123");

        var result = await _command.ExecuteAsync(invalidRequest);

        Assert.False(result.IsSuccess);
        Assert.Contains("Şifre en az bir büyük harf içermelidir.", result.ErrorMessage);
    }
}
