using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Platform.Tenants;

public static class TenantDeletePolicy
{
    public static async Task<(bool CanDelete, string? BlockedReason)> EvaluateAsync(
        IApplicationDbContext context,
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        if (await context.Payments.AnyAsync(p => p.TenantId == tenantId, cancellationToken))
            return (false, "Ödeme kaydı olan müşteri silinemez.");

        var subscriptions = await context.TenantSubscriptions
            .AsNoTracking()
            .Include(s => s.Package)
            .Where(s => s.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        if (subscriptions.Count == 0)
            return (true, null);

        if (subscriptions.Count > 1)
            return (false, "Abonelik geçmişi olan müşteri silinemez.");

        var only = subscriptions[0];

        if (only.Package.Code != PackageCodes.Free)
            return (false, "Tanımlı ücretli paketi olan müşteri silinemez.");

        if (only.Status is SubscriptionStatus.Cancelled or SubscriptionStatus.Superseded)
            return (false, "Abonelik geçmişi olan müşteri silinemez.");

        if (only.Status is SubscriptionStatus.PendingPayment or SubscriptionStatus.PastDue)
            return (false, "Aktif abonelik bekleyen müşteri silinemez.");

        if (only.PaymentId.HasValue)
            return (false, "Ödeme bağlantısı olan müşteri silinemez.");

        return (true, null);
    }
}
