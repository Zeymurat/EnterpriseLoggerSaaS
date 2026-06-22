namespace EnterpriseLogger.Domain.Enums;

public enum SubscriptionStatus
{
    Active = 0,
    PendingPayment = 1,
    PastDue = 2,
    Cancelled = 3,
    Superseded = 4
}
