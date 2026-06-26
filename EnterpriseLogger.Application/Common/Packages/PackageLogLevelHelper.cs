namespace EnterpriseLogger.Application.Common.Packages;

public static class PackageLogLevelHelper
{
    public static readonly string[] SupportedLevels = ["INFO", "WARNING", "ERROR"];

    public static IReadOnlySet<string> Parse(string allowedLogLevels) =>
        allowedLogLevels
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(level => level.ToUpperInvariant())
            .Where(level => SupportedLevels.Contains(level, StringComparer.Ordinal))
            .ToHashSet(StringComparer.Ordinal);

    public static string Serialize(IEnumerable<string> levels)
    {
        var normalized = levels
            .Select(level => level.Trim().ToUpperInvariant())
            .Where(level => SupportedLevels.Contains(level, StringComparer.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(level => Array.IndexOf(SupportedLevels, level))
            .ToArray();

        return string.Join(',', normalized);
    }

    public static bool IsAllowed(string allowedLogLevels, string logLevel)
    {
        var allowed = Parse(allowedLogLevels);
        if (allowed.Count == 0)
            return false;

        return allowed.Contains(logLevel.Trim().ToUpperInvariant());
    }

    public static bool IsValidSelection(string allowedLogLevels) => Parse(allowedLogLevels).Count > 0;
}
