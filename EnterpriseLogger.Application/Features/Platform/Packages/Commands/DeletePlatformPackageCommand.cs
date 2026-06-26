using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Platform.Packages.Commands;

public class DeletePlatformPackageCommand
{
    private readonly IApplicationDbContext _context;
    private readonly IPlatformAuditService _auditService;

    public DeletePlatformPackageCommand(
        IApplicationDbContext context,
        IPlatformAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<Result<bool>> ExecuteAsync(int packageId, CancellationToken cancellationToken = default)
    {
        var package = await _context.Packages
            .FirstOrDefaultAsync(p => p.Id == packageId, cancellationToken);

        if (package is null)
            return Result<bool>.NotFound("Paket bulunamadı.");

        if (package.IsDefault)
            return Result<bool>.Failure("Varsayılan paket silinemez.");

        var hasSubscriptions = await _context.TenantSubscriptions
            .AnyAsync(s => s.PackageId == packageId, cancellationToken);

        if (hasSubscriptions)
        {
            return Result<bool>.Failure(
                "Bu pakete bağlı abonelik kayıtları var. Paket silinemez; satışı kapatabilirsiniz.");
        }

        var hasPayments = await _context.Payments
            .AnyAsync(p => p.PackageId == packageId, cancellationToken);

        if (hasPayments)
            return Result<bool>.Failure("Bu pakete bağlı ödeme kayıtları var. Paket silinemez.");

        _context.Packages.Remove(package);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            PlatformAuditActions.PackageDeleted,
            "package",
            packageId,
            null,
            $"code={package.Code}",
            cancellationToken);

        return Result<bool>.Success(true);
    }
}
