namespace EnterpriseLogger.Application.Common;

/// <summary>
/// Telefon numaralarını E.164 formatına normalize eder (ör. +905551234567).
/// İleride SMS/OTP entegrasyonu için hazırlık.
/// </summary>
public static class PhoneNormalizer
{
    public static bool TryNormalize(string? input, out string normalized)
    {
        normalized = string.Empty;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        var digits = new string(input.Where(c => char.IsDigit(c) || c == '+').ToArray());

        if (digits.StartsWith('+'))
            digits = digits[1..];

        digits = new string(digits.Where(char.IsDigit).ToArray());

        if (digits.Length == 0)
            return false;

        // Türkiye: 05xx / 5xx / 90xxx → +90...
        if (digits.StartsWith("0") && digits.Length == 11)
            digits = "90" + digits[1..];
        else if (digits.StartsWith('5') && digits.Length == 10)
            digits = "90" + digits;
        else if (!digits.StartsWith("90") && digits.Length is >= 10 and <= 15)
        {
            // Diğer ülke kodları olduğu gibi bırakılır; başına + eklenir
        }

        if (digits.Length < 10 || digits.Length > 15)
            return false;

        normalized = "+" + digits;
        return true;
    }
}
