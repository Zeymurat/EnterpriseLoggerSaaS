using EnterpriseLogger.Application.Common;
using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Tenants.Dtos;
using EnterpriseLogger.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Tenants.Commands;

public class RotateTenantApiKeyCommand
{
    private const int MaxApiKeyGenerationAttempts = 5;

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserProvider _currentUser;
    private readonly IApiKeyHasher _apiKeyHasher;

    public RotateTenantApiKeyCommand(
        IApplicationDbContext context,
        ICurrentUserProvider currentUser,
        IApiKeyHasher apiKeyHasher)
    {
        _context = context;
        _currentUser = currentUser;
        _apiKeyHasher = apiKeyHasher;
    }

    public async Task<Result<RotateApiKeyResponseDto>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.TenantId is null)
            return Result<RotateApiKeyResponseDto>.Forbidden("Kimlik doğrulama gerekli.");

        if (_currentUser.Role != TenantUserRole.Root)
            return Result<RotateApiKeyResponseDto>.Forbidden("API anahtarı yalnızca Root kullanıcı tarafından yenilenebilir.");

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == _currentUser.TenantId, cancellationToken);

        if (tenant is null)
            return Result<RotateApiKeyResponseDto>.Failure("Tenant bulunamadı.");

        if (!tenant.IsActive)
            return Result<RotateApiKeyResponseDto>.Forbidden("Pasif tenant için API anahtarı üretilemez.");

        var apiKey = await GenerateUniqueApiKeyAsync(cancellationToken);
        if (apiKey is null)
            return Result<RotateApiKeyResponseDto>.Failure("API anahtarı üretilemedi. Lütfen tekrar deneyin.");

        tenant.ApiKeyHash = _apiKeyHasher.Hash(apiKey);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Result<RotateApiKeyResponseDto>.Failure("API anahtarı kaydedilemedi. Lütfen tekrar deneyin.");
        }

        return Result<RotateApiKeyResponseDto>.Success(new RotateApiKeyResponseDto(apiKey));
    }

    private async Task<string?> GenerateUniqueApiKeyAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxApiKeyGenerationAttempts; attempt++)
        {
            var candidate = ApiKeyGenerator.Generate();
            var candidateHash = _apiKeyHasher.Hash(candidate);
            var exists = await _context.Tenants
                .AnyAsync(t => t.ApiKeyHash == candidateHash, cancellationToken);

            if (!exists)
                return candidate;
        }

        return null;
    }
}
