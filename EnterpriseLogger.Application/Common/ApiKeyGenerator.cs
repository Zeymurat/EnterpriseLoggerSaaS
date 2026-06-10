using System.Security.Cryptography;

namespace EnterpriseLogger.Application.Common;

/// <summary>
/// Kurumsal SaaS modeli: API anahtarını platform üretir, müşteri seçmez.
/// Örnek format: EL_a1b2c3d4e5f6... (kriptografik rastgele)
/// </summary>
public static class ApiKeyGenerator
{
    private const string Prefix = "EL_";

    public static string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(24);
        var secret = Convert.ToHexString(bytes).ToLowerInvariant();
        return $"{Prefix}{secret}";
    }
}
