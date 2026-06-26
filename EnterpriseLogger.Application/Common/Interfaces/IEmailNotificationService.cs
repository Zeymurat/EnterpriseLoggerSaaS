namespace EnterpriseLogger.Application.Common.Interfaces;

public interface IEmailNotificationService
{
    bool IsEnabled { get; }

    Task SendPaymentDueAsync(
        int tenantId,
        string recipientEmail,
        string tenantName,
        string packageName,
        DateTime? paymentDueBy,
        CancellationToken cancellationToken = default);

    Task SendGraceReminderAsync(
        int tenantId,
        string recipientEmail,
        string tenantName,
        string packageName,
        DateTime graceEndsAt,
        CancellationToken cancellationToken = default);

    Task SendQuotaWarningAsync(
        int tenantId,
        string recipientEmail,
        string tenantName,
        string packageName,
        int monthlyLogCount,
        int monthlyLimit,
        CancellationToken cancellationToken = default);
}
