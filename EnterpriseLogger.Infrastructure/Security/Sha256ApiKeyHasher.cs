using System.Security.Cryptography;
using System.Text;
using EnterpriseLogger.Application.Common.Interfaces;

namespace EnterpriseLogger.Infrastructure.Security;

public class Sha256ApiKeyHasher : IApiKeyHasher
{
    public string Hash(string apiKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public bool Verify(string apiKey, string apiKeyHash)
    {
        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiKeyHash))
            return false;

        var computed = Hash(apiKey);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(apiKeyHash));
    }
}
