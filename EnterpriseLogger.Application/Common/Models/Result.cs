namespace EnterpriseLogger.Application.Common.Models;

using EnterpriseLogger.Application.Common.Constants;

using EnterpriseLogger.Application.Features.Auth.Dtos;

public enum ResultErrorKind
{
    Validation = 400,
    Forbidden = 403,
    NotFound = 404,
    Conflict = 409,
    TooManyRequests = 429
}

public class Result<T>
{
    public T? Data { get; init; }
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public ResultErrorKind ErrorKind { get; init; } = ResultErrorKind.Validation;
    public string? ErrorCode { get; init; }
    public int? RetryAfterSeconds { get; init; }
    public IReadOnlyList<TenantLoginOptionDto>? TenantOptions { get; init; }

    public static Result<T> Success(T data) => new() { Data = data, IsSuccess = true };

    public static Result<T> Failure(string error, ResultErrorKind kind = ResultErrorKind.Validation) =>
        new() { IsSuccess = false, ErrorMessage = error, ErrorKind = kind };

    public static Result<T> AmbiguousTenant(string error, IReadOnlyList<TenantLoginOptionDto> tenantOptions) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = error,
            ErrorKind = ResultErrorKind.Validation,
            ErrorCode = AuthErrorCodes.AmbiguousTenantContext,
            TenantOptions = tenantOptions
        };

    public static Result<T> Forbidden(string error) => Failure(error, ResultErrorKind.Forbidden);

    public static Result<T> NotFound(string error) => Failure(error, ResultErrorKind.NotFound);

    public static Result<T> Conflict(string error) => Failure(error, ResultErrorKind.Conflict);

    public static Result<T> RateLimited(string error, int retryAfterSeconds) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = error,
            ErrorKind = ResultErrorKind.TooManyRequests,
            RetryAfterSeconds = Math.Max(1, retryAfterSeconds)
        };
}
