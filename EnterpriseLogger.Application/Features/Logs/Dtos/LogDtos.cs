namespace EnterpriseLogger.Application.Features.Logs.Dtos;

public record CreateLogRequest(
    string ApplicationName,
    string LogLevel,
    string Message
);

public record LogResponseDto(
    long Id,
    int TenantId,
    string ApplicationName,
    string LogLevel,
    string Message,
    DateTime Timestamp
);
