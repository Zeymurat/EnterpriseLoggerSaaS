namespace EnterpriseLogger.Application.Common.Constants;

public static class PlatformAuditActions
{
    public const string TenantStatusChanged = "tenant.status_changed";
    public const string TenantDeleted = "tenant.deleted";
    public const string TenantRootPasswordReset = "tenant.root_password_reset";
    public const string TenantImpersonated = "tenant.impersonated";
    public const string SubscriptionAssigned = "subscription.assigned";
    public const string SubscriptionCancelled = "subscription.cancelled";
    public const string SubscriptionRemoved = "subscription.removed";
    public const string SubscriptionPaymentLinked = "subscription.payment_linked";
    public const string PaymentRecorded = "payment.recorded";
    public const string PaymentConfirmed = "payment.confirmed";
    public const string PaymentRejected = "payment.rejected";
    public const string PaymentUpdated = "payment.updated";
    public const string PaymentDeleted = "payment.deleted";
    public const string PackageCreated = "package.created";
    public const string PackageUpdated = "package.updated";
    public const string PackageDeleted = "package.deleted";
    public const string SystemSubscriptionRenewal = "system.subscription_renewal";
    public const string SystemGraceExpired = "system.grace_expired";
    public const string SystemLogRetention = "system.log_retention";
}
