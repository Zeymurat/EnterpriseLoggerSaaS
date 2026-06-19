namespace EnterpriseLogger.Application.Common.Interfaces;

public interface IApiKeyHasher
{
    string Hash(string apiKey);

    bool Verify(string apiKey, string apiKeyHash);
}
