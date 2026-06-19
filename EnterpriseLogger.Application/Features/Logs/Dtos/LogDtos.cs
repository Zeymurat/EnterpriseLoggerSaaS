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
