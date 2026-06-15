using EnterpriseLogger.Application.Common;
using Xunit;

namespace EnterpriseLogger.Application.Tests.Common;

public class TemporaryPasswordGeneratorTests
{
    [Fact]
    public void Generate_ReturnsPasswordMeetingPolicy()
    {
        var password = TemporaryPasswordGenerator.Generate();

        Assert.True(password.Length >= 8);
        Assert.Contains(password, c => char.IsUpper(c));
        Assert.Contains(password, c => char.IsLower(c));
        Assert.Contains(password, c => char.IsDigit(c));
    }

    [Fact]
    public void Generate_ProducesUniqueValues()
    {
        var first = TemporaryPasswordGenerator.Generate();
        var second = TemporaryPasswordGenerator.Generate();

        Assert.NotEqual(first, second);
    }
}
