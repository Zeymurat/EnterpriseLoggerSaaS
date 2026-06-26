using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Packages;
using EnterpriseLogger.Application.Common.Subscriptions;
using EnterpriseLogger.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Infrastructure.Notifications;

public class BillingNotificationProcessor
{
    private const string PaymentDueType = "payment_due";
    private const string GraceReminderType = "grace_reminder";

    private readonly IApplicationDbContext _context;
    private readonly IEmailNotificationService _emailService;

    public BillingNotificationProcessor(
        IApplicationDbContext context,
        IEmailNotificationService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        if (!_emailService.IsEnabled)
            return;

        var now = DateTime.UtcNow;
        await ProcessPaymentDueAsync(now, cancellationToken);
        await ProcessGraceRemindersAsync(now, cancellationToken);
        await ProcessQuotaWarningsAsync(now, cancellationToken);
    }

    private async Task ProcessPaymentDueAsync(DateTime now, CancellationToken cancellationToken)
    {
        var pending = await _context.TenantSubscriptions
            .AsNoTracking()
            .Include(s => s.Package)
            .Include(s => s.Tenant)
            .ThenInclude(t => t.Users)
            .Where(s => s.Status == SubscriptionStatus.PendingPayment
                && !s.IsPaid
                && s.EndDate > now
                && s.Package.IsMailEnabled)
            .ToListAsync(cancellationToken);

        foreach (var subscription in pending)
        {
            var referenceKey = $"sub-{subscription.Id}";
            if (await WasSentAsync(subscription.TenantId, PaymentDueType, referenceKey, cancellationToken))
                continue;

            var rootEmail = subscription.Tenant.Users
                .FirstOrDefault(u => u.Role == TenantUserRole.Root)?.Email;

            if (string.IsNullOrWhiteSpace(rootEmail))
                continue;

            await _emailService.SendPaymentDueAsync(
                subscription.TenantId,
                rootEmail,
                subscription.Tenant.Name,
                subscription.Package.Name,
                subscription.GracePeriodEndDate,
                cancellationToken);

            await MarkSentAsync(subscription.TenantId, PaymentDueType, referenceKey, cancellationToken);
        }
    }

    private async Task ProcessGraceRemindersAsync(DateTime now, CancellationToken cancellationToken)
    {
        var reminderWindowEnd = now.AddDays(3);

        var subscriptions = await _context.TenantSubscriptions
            .AsNoTracking()
            .Include(s => s.Package)
            .Include(s => s.Tenant)
            .ThenInclude(t => t.Users)
            .Where(s => s.Status == SubscriptionStatus.PendingPayment
                && !s.IsPaid
                && s.GracePeriodEndDate != null
                && s.GracePeriodEndDate > now
                && s.GracePeriodEndDate <= reminderWindowEnd
                && s.Package.IsMailEnabled)
            .ToListAsync(cancellationToken);

        foreach (var subscription in subscriptions)
        {
            var referenceKey = $"grace-{subscription.Id}-{subscription.GracePeriodEndDate:yyyyMMdd}";
            if (await WasSentAsync(subscription.TenantId, GraceReminderType, referenceKey, cancellationToken))
                continue;

            var rootEmail = subscription.Tenant.Users
                .FirstOrDefault(u => u.Role == TenantUserRole.Root)?.Email;

            if (string.IsNullOrWhiteSpace(rootEmail) || subscription.GracePeriodEndDate is null)
                continue;

            await _emailService.SendGraceReminderAsync(
                subscription.TenantId,
                rootEmail,
                subscription.Tenant.Name,
                subscription.Package.Name,
                subscription.GracePeriodEndDate.Value,
                cancellationToken);

            await MarkSentAsync(subscription.TenantId, GraceReminderType, referenceKey, cancellationToken);
        }
    }

    private async Task ProcessQuotaWarningsAsync(DateTime now, CancellationToken cancellationToken)
    {
        const string quotaType = "quota_warning";
        var monthKey = now.ToString("yyyy-MM");

        var activeSubscriptions = await _context.TenantSubscriptions
            .AsNoTracking()
            .Include(s => s.Package)
            .Include(s => s.Tenant)
            .ThenInclude(t => t.Users)
            .Where(s => SubscriptionHelper.ActiveStatuses.Contains(s.Status)
                && s.EndDate > now
                && s.Package.IsMailEnabled)
            .ToListAsync(cancellationToken);

        foreach (var subscription in activeSubscriptions)
        {
            var monthlyCount = await PackageQuotaHelper.GetMonthlyLogCountAsync(
                _context,
                subscription.TenantId,
                cancellationToken);

            var limit = subscription.Package.MonthlyRequestLimit;
            if (limit <= 0 || monthlyCount < limit * 0.9)
                continue;

            var referenceKey = $"{monthKey}-{subscription.TenantId}";
            if (await WasSentAsync(subscription.TenantId, quotaType, referenceKey, cancellationToken))
                continue;

            var rootEmail = subscription.Tenant.Users
                .FirstOrDefault(u => u.Role == TenantUserRole.Root)?.Email;

            if (string.IsNullOrWhiteSpace(rootEmail))
                continue;

            await _emailService.SendQuotaWarningAsync(
                subscription.TenantId,
                rootEmail,
                subscription.Tenant.Name,
                subscription.Package.Name,
                monthlyCount,
                limit,
                cancellationToken);

            await MarkSentAsync(subscription.TenantId, quotaType, referenceKey, cancellationToken);
        }
    }

    private async Task<bool> WasSentAsync(
        int tenantId,
        string notificationType,
        string referenceKey,
        CancellationToken cancellationToken)
    {
        return await _context.NotificationDispatchLogs
            .AsNoTracking()
            .AnyAsync(
                n => n.TenantId == tenantId
                    && n.NotificationType == notificationType
                    && n.ReferenceKey == referenceKey,
                cancellationToken);
    }

    private async Task MarkSentAsync(
        int tenantId,
        string notificationType,
        string referenceKey,
        CancellationToken cancellationToken)
    {
        _context.NotificationDispatchLogs.Add(new Domain.Entities.NotificationDispatchLog
        {
            TenantId = tenantId,
            NotificationType = notificationType,
            ReferenceKey = referenceKey,
            SentAt = DateTime.UtcNow,
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
