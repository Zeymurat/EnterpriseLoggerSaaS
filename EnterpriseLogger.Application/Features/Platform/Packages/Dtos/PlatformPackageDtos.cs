using EnterpriseLogger.Domain.Enums;

namespace EnterpriseLogger.Application.Features.Platform.Packages.Dtos;

public record PlatformPackageDto(
    int Id,
    string Code,
    string Name,
    string Description,
    string AllowedLogLevels,
    bool IsMailEnabled,
    bool IsSmsEnabled,
    int MonthlyRequestLimit,
    int MaxLogsPerMinute,
    int StorageRetentionDays,
    decimal PriceMonthly,
    decimal PriceQuarterly,
    decimal PriceSemiAnnual,
    decimal PriceAnnual,
    bool IsDefault,
    bool IsAvailable,
    int SortOrder,
    int ActiveTenantCount);

public record PlatformPackageListResponse(IReadOnlyList<PlatformPackageDto> Packages);

public record CreatePlatformPackageRequest(
    string Code,
    string Name,
    string Description,
    string AllowedLogLevels,
    bool IsMailEnabled,
    bool IsSmsEnabled,
    int MonthlyRequestLimit,
    int MaxLogsPerMinute,
    int StorageRetentionDays,
    decimal PriceMonthly,
    decimal PriceQuarterly,
    decimal PriceSemiAnnual,
    decimal PriceAnnual,
    bool IsAvailable,
    int SortOrder);

public record UpdatePlatformPackageRequest(
    string Name,
    string Description,
    string AllowedLogLevels,
    bool IsMailEnabled,
    bool IsSmsEnabled,
    int MonthlyRequestLimit,
    int MaxLogsPerMinute,
    int StorageRetentionDays,
    decimal PriceMonthly,
    decimal PriceQuarterly,
    decimal PriceSemiAnnual,
    decimal PriceAnnual,
    bool IsAvailable,
    int SortOrder);
