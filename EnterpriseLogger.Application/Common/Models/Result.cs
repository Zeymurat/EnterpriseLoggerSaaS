namespace EnterpriseLogger.Application.Common.Models;

public class Result<T>
{
    public T? Data { get; init; }
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }

    public static Result<T> Success(T data) => new() { Data = data, IsSuccess = true };
    public static Result<T> Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
}
