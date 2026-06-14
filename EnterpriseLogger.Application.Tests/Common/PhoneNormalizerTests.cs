using EnterpriseLogger.Application.Common;
using Xunit;

namespace EnterpriseLogger.Application.Tests.Common;

public class PhoneNormalizerTests
{
    [Theory]
    [InlineData("+905551234567", "+905551234567")]
    [InlineData("05551234567", "+905551234567")]
    [InlineData("5551234567", "+905551234567")]
    [InlineData("90 555 123 45 67", "+905551234567")]
    public void TryNormalize_ValidTurkishNumbers_ReturnsE164(string input, string expected)
    {
        var success = PhoneNormalizer.TryNormalize(input, out var normalized);

        Assert.True(success);
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("123")]
    public void TryNormalize_InvalidInput_ReturnsFalse(string input)
    {
        Assert.False(PhoneNormalizer.TryNormalize(input, out _));
    }
}
