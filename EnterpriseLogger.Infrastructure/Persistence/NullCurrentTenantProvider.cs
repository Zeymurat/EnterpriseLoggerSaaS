using EnterpriseLogger.Application.Common.Interfaces;

namespace EnterpriseLogger.Infrastructure.Persistence;

/// <summary>
/// Design-time ve tenant bağlamı olmayan senaryolar için TenantId çözülmemiş provider.
/// Global filter bu durumda okuma sorgularında satır döndürmez (güvenli varsayılan).
/// </summary>
internal sealed class NullCurrentTenantProvider : ICurrentTenantProvider
{
    public int? TenantId => null;
}
