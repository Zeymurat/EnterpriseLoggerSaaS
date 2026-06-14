using EnterpriseLogger.Application.Features.Auth.Commands;
using EnterpriseLogger.Application.Features.Auth.Dtos;
using FluentValidation;
using Xunit;

namespace EnterpriseLogger.Application.Tests.Features.Auth;

public class LoginRequestValidatorTests
{
    private readonly IValidator<LoginRequest> _validator = new LoginRequestValidator();

    [Fact]
    public void Validate_WhenEmailIsInvalid_ShouldFail()
    {
        var request = new LoginRequest("not-email", "password");

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Geçerli bir e-posta"));
    }

    [Fact]
    public void Validate_WhenPasswordIsEmpty_ShouldFail()
    {
        var request = new LoginRequest("user@test.com", "");

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Şifre boş olamaz"));
    }

    [Fact]
    public void Validate_WhenRequestIsValid_ShouldPass()
    {
        var request = new LoginRequest("user@test.com", "TestPass123", "Acme Corp");

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }
}
