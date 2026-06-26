using EnterpriseLogger.Domain.Enums;

namespace EnterpriseLogger.Application.Common.Subscriptions;

public static class SubscriptionStatusLabels
{
    public static string ToTurkish(SubscriptionStatus? status) => status switch
    {
        SubscriptionStatus.Active => "Aktif",
        SubscriptionStatus.PendingPayment => "Ödeme bekliyor",
        SubscriptionStatus.PastDue => "Gecikmiş",
        SubscriptionStatus.Cancelled => "İptal",
        SubscriptionStatus.Superseded => "Kapatıldı",
        null => string.Empty,
        _ => status.ToString() ?? string.Empty,
    };
}
