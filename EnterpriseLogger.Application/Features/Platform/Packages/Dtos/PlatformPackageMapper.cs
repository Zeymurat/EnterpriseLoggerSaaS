namespace EnterpriseLogger.Application.Features.Platform.Packages.Dtos;

public static class PlatformPackageMapper
{
    public static PlatformPackageDto ToDto(Domain.Entities.Package package, int activeTenantCount = 0) =>
        new(
            package.Id,
            package.Code,
            package.Name,
            package.Description,
            package.AllowedLogLevels,
            package.IsMailEnabled,
            package.IsSmsEnabled,
            package.MonthlyRequestLimit,
            package.MaxLogsPerMinute,
            package.StorageRetentionDays,
            package.PriceMonthly,
            package.PriceQuarterly,
            package.PriceSemiAnnual,
            package.PriceAnnual,
            package.IsDefault,
            package.IsAvailable,
            package.SortOrder,
            activeTenantCount);
}
