namespace EnterpriseLogger.Application.Features.Logs.Dtos;

public record CreateLogRequest(
    string ApplicationName,
    string LogLevel,
    string Message,
    string? HttpMethod = null,
    string? RequestPath = null,
    int? StatusCode = null,
    string? CorrelationId = null,
    string? ActorIdentifier = null,
    string? ExceptionType = null);

public record LogResponseDto(
    long Id,
    int TenantId,
    string ApplicationName,
    string LogLevel,
    string Message,
    DateTime Timestamp,
    string? HttpMethod,
    string? RequestPath,
    int? StatusCode,
    string? CorrelationId,
    string? ActorIdentifier,
    string? ExceptionType);

public record GetLogsQueryParams(
    int Page = 1,
    int PageSize = 25,
    IReadOnlyList<string>? LogLevels = null,
    string? Search = null,
    DateTime? From = null,
    DateTime? To = null,
    IReadOnlyList<string>? ApplicationNames = null,
    IReadOnlyList<string>? HttpMethods = null,
    IReadOnlyList<int>? StatusCodes = null);

public record LogFilterOptionsDto(
    IReadOnlyList<string> ApplicationNames,
    IReadOnlyList<string> HttpMethods,
    IReadOnlyList<int> StatusCodes);

public record LogLevelSummaryDto(int Total, int Info, int Warning, int Error);

public record LogListResponseDto(
    IReadOnlyList<LogResponseDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    LogLevelSummaryDto Summary,
    LogFilterOptionsDto AvailableFilters,
    LogLevelSummaryDto? OverallSummary = null,
    bool IsDateFiltered = false);

public record LogExportResultDto(
    byte[] Content,
    string FileName,
    int ExportedCount,
    int TotalMatching,
    bool Truncated);
