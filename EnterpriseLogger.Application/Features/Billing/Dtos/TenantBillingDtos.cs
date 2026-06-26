using EnterpriseLogger.Domain.Enums;

namespace EnterpriseLogger.Application.Features.Billing.Dtos;

public record TenantBillingNoticeDto(
    bool ShowNotice,
    string? PackageName,
    DateTime? PaymentDueBy,
    DateTime? PeriodEnd,
    string? Message,
    BillingCycle? BillingCycle);
