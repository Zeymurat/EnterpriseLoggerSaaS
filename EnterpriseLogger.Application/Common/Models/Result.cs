namespace EnterpriseLogger.Application.Common.Models;

public enum ResultErrorKind
{
    Validation = 400,
    Forbidden = 403,
    NotFound = 404,
    Conflict = 409
}

public class Result<T>
{
    public T? Data { get; init; }
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public ResultErrorKind ErrorKind { get; init; } = ResultErrorKind.Validation;

    public static Result<T> Success(T data) => new() { Data = data, IsSuccess = true };

    public static Result<T> Failure(string error, ResultErrorKind kind = ResultErrorKind.Validation) =>
        new() { IsSuccess = false, ErrorMessage = error, ErrorKind = kind };

    public static Result<T> Forbidden(string error) => Failure(error, ResultErrorKind.Forbidden);

    public static Result<T> NotFound(string error) => Failure(error, ResultErrorKind.NotFound);

    public static Result<T> Conflict(string error) => Failure(error, ResultErrorKind.Conflict);
}
