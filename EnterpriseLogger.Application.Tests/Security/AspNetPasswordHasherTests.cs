using EnterpriseLogger.Infrastructure.Security;
using Xunit;

namespace EnterpriseLogger.Application.Tests.Security;

public class AspNetPasswordHasherTests
{
    private readonly AspNetPasswordHasher _hasher = new();

    [Fact]
    public void HashAndVerify_ValidPassword_ReturnsTrue()
    {
        const string password = "SecurePass123";

        var hash = _hasher.Hash(password);

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.NotEqual(password, hash);
        Assert.True(_hasher.Verify(password, hash));
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("SecurePass123");

        Assert.False(_hasher.Verify("WrongPass123", hash));
    }
}
