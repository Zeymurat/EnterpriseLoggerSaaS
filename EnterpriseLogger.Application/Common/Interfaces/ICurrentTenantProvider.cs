namespace EnterpriseLogger.Application.Common.Interfaces;

/// <summary>
/// Aktif HTTP isteğine ait tenant kimliğini taşır.
/// Middleware doğruladıktan sonra doldurulur; handler'lar buradan okur.
/// </summary>
public interface ICurrentTenantProvider
{
    int? TenantId { get; }
    bool IsResolved => TenantId.HasValue;
}
