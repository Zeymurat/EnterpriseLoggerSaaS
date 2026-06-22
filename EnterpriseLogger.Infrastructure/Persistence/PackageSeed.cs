using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Domain.Entities;

namespace EnterpriseLogger.Infrastructure.Persistence;

internal static class PackageSeed
{
    public static IReadOnlyList<Package> GetDefaultPackages() =>
    [
        new()
        {
            Id = 1,
            Code = PackageCodes.Free,
            Name = "Free",
            Description = "Başlangıç paketi — sınırlı log kotası",
            AllowedLogLevels = "INFO,WARNING,ERROR",
            IsMailEnabled = false,
            IsSmsEnabled = false,
            MonthlyRequestLimit = 10_000,
            MaxLogsPerMinute = 100,
            StorageRetentionDays = 30,
            PriceMonthly = 0,
            PriceQuarterly = 0,
            PriceSemiAnnual = 0,
            PriceAnnual = 0,
            IsDefault = true,
            IsAvailable = true,
            SortOrder = 1
        },
        new()
        {
            Id = 2,
            Code = PackageCodes.Basic,
            Name = "Basic",
            Description = "Küçük ekipler için aylık paket",
            AllowedLogLevels = "INFO,WARNING,ERROR",
            IsMailEnabled = true,
            IsSmsEnabled = false,
            MonthlyRequestLimit = 100_000,
            MaxLogsPerMinute = 500,
            StorageRetentionDays = 90,
            PriceMonthly = 499,
            PriceQuarterly = 1_399,
            PriceSemiAnnual = 2_699,
            PriceAnnual = 4_999,
            IsDefault = false,
            IsAvailable = true,
            SortOrder = 2
        },
        new()
        {
            Id = 3,
            Code = PackageCodes.Pro,
            Name = "Pro",
            Description = "Yüksek hacimli üretim ortamları",
            AllowedLogLevels = "INFO,WARNING,ERROR",
            IsMailEnabled = true,
            IsSmsEnabled = true,
            MonthlyRequestLimit = 1_000_000,
            MaxLogsPerMinute = 2_000,
            StorageRetentionDays = 365,
            PriceMonthly = 1_999,
            PriceQuarterly = 5_499,
            PriceSemiAnnual = 10_499,
            PriceAnnual = 19_999,
            IsDefault = false,
            IsAvailable = true,
            SortOrder = 3
        }
    ];
}
